namespace ResXManager.Model
{
    using System;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using ResXManager.Infrastructure;

    using JsonConvert = Newtonsoft.Json.JsonConvert;

    [Export(typeof(IService))]
    class WebFilesExporter : IService
    {
        private readonly ResourceManager _resourceManager;
        private readonly ITracer _tracer;

        [ImportingConstructor]
        public WebFilesExporter(ResourceManager resourceManager, ITracer tracer)
        {
            _resourceManager = resourceManager;
            _tracer = tracer;
            _resourceManager.ProjectFileSaved += (_, __) => Export();
            _resourceManager.Loaded += (_, __) => Export();
        }

        public void Start()
        {
        }

        private void Export()
        {
            try
            {
                var solutionFolder = _resourceManager.SolutionFolder;
                if (solutionFolder == null)
                    return;

                var configFilePath = Path.Combine(solutionFolder, "resx-manager.webexport.config");
                if (!File.Exists(configFilePath))
                    return;

                var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configFilePath));

                var typeScriptFileDir = config.TypeScriptFileDir;
                var jsonFileDir = config.JsonFileDir;

                if (string.IsNullOrEmpty(typeScriptFileDir) || string.IsNullOrEmpty(jsonFileDir))
                    return;

                typeScriptFileDir = Directory.CreateDirectory(Path.Combine(solutionFolder, typeScriptFileDir)).FullName;
                jsonFileDir = Directory.CreateDirectory(Path.Combine(solutionFolder, jsonFileDir)).FullName;

                var neutralLanguages = _resourceManager.ResourceEntities
                    .Select(entity => entity.Languages.FirstOrDefault())
                    .Where(lang => lang != null);

                var typescript = new StringBuilder();

                foreach (var language in neutralLanguages)
                {
                    var entityName = language.Container.BaseName;

                    typescript.AppendLine($@"export class {entityName} {{");

                    foreach (var node in language.GetNodes())
                    {
                        var value = JsonConvert.SerializeObject(node.Text ?? string.Empty);
                        typescript.AppendLine($@"  {node.Key} = {value};");
                    }

                    typescript.AppendLine(@"}");
                    typescript.AppendLine();
                }

                var typeScriptFilePath = Path.Combine(typeScriptFileDir, "resources.ts");
                File.WriteAllText(typeScriptFilePath, typescript.ToString());

                var specificLanguages = _resourceManager.ResourceEntities
                    .SelectMany(entity => entity.Languages.Skip(1))
                    .GroupBy(lang => lang.CultureKey);

                foreach (var languages in specificLanguages)
                {
                    var cultureKey = languages.Key;
                    var json = new JObject();

                    foreach (var language in languages)
                    {
                        var entityName = language.Container.BaseName;
                        var node = new JObject();
                        foreach (var resourceNode in language.GetNodes())
                        {
                            node.Add(resourceNode.Key, JToken.FromObject(resourceNode.Text));
                        }

                        json.Add(entityName, node);
                    }

                    var jsonFilePath = Path.Combine(jsonFileDir, $"resources{cultureKey}.json");
                    File.WriteAllText(jsonFilePath, json.ToString());
                }
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex.ToString());
            }
        }

        private class Configuration
        {
            [JsonProperty("typeScriptFileDir")]
            public string TypeScriptFileDir { get; set; }
            [JsonProperty("jsonFileDir")]
            public string JsonFileDir { get; set; }
        }
    }
}
