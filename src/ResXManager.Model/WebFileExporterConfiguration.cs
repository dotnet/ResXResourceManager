namespace ResXManager.Model
{
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.CompilerServices;

    using Newtonsoft.Json;

    using TomsToolbox.Essentials;

    public sealed class WebFileExporterConfiguration : INotifyPropertyChanged
    {
        private const string WebExportConfigFileName = "resx-manager.webexport.config";

        [JsonProperty("typeScriptFileDir")]
        public string? TypeScriptFileDir { get; set; }

        [JsonProperty("jsonFileDir")]
        public string? JsonFileDir { get; set; }

        [JsonProperty("exportNeutralJson")]
        public bool ExportNeutralJson { get; set; }

        [JsonProperty("include")]
        public string? Include { get; set; }

        public static bool Load(string? solutionFolder, [NotNullWhen(true)] out WebFileExporterConfiguration? config)
        {
            config = null;
            if (solutionFolder.IsNullOrEmpty())
                return false;

            var configFilePath = Path.Combine(solutionFolder, WebExportConfigFileName);
            if (!File.Exists(configFilePath))
                return false;

            config = JsonConvert.DeserializeObject<WebFileExporterConfiguration>(File.ReadAllText(configFilePath));

            return config != null;
        }

        public void Save(string solutionFolder)
        {
            var configFilePath = Path.Combine(solutionFolder, WebExportConfigFileName);

            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

#pragma warning disable IDE0051 // Remove unused private members
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}