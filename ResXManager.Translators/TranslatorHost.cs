namespace ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using Throttle;

    using ResXManager.Infrastructure;
    using ResXManager.Translators.Properties;

    using TomsToolbox.Wpf;

    [Export]
    public class TranslatorHost : IDisposable
    {
        private readonly ITranslator[] _translators;

        [CanBeNull]
        private ITranslationSession _activeSession;

        [ImportingConstructor]
        public TranslatorHost([ImportMany] ITranslator[] translators)
        {
            _translators = translators;

            var settings = Settings.Default;
            var configuration = settings.Configuration;

            LoadConfiguration(translators, configuration);

            RegisterChangeEvents(translators);
        }

        public IEnumerable<ITranslator> Translators => _translators;

        [CanBeNull]
        public ITranslationSession ActiveSession => _activeSession;

        public void StartSession([CanBeNull] CultureInfo sourceLanguage, [NotNull] CultureInfo neutralResourcesLanguage, [NotNull][ItemNotNull] ICollection<ITranslationItem> items)
        {
            var session = new TranslationSession(sourceLanguage, neutralResourcesLanguage, items);
            Interlocked.Exchange(ref _activeSession, session)?.Dispose();

            Task.Run(() =>
            {
                try
                {
                    var translatorTasks = Translators
                        .Where(t => t.IsEnabled)
                        .Select(t => Task.Run(() => { t.Translate(session); }))
                        .ToArray();

                    Task.WaitAll(translatorTasks);
                }
                finally
                {
                    session.Dispose();
                }
            });
        }

        [Throttled(typeof(Throttle), 1000)]
        private void SaveConfiguration()
        {
            var settings = Settings.Default;

            var values = new Dictionary<string, string>();

            foreach (var translator in _translators)
            {
                var json = JsonConvert.SerializeObject(translator);
                values[translator.Id] = json;
            }

            settings.Configuration = JsonConvert.SerializeObject(values);
        }

        private static void LoadConfiguration([NotNull][ItemNotNull] ITranslator[] translators, [CanBeNull] string configuration)
        {
            if (string.IsNullOrEmpty(configuration))
                return;

            try
            {
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(configuration);
                if (values == null)
                    return;

                foreach (var translator in translators)
                {
                    if (!values.TryGetValue(translator.Id, out var setting))
                        continue;
                    if (string.IsNullOrEmpty(setting))
                        continue;

                    try
                    {
                        JsonConvert.PopulateObject(setting, translator);
                    }
                    catch
                    {
                        // Newtonsoft.Json has not documented any exceptions...
                    }
                }
            }
            catch
            {
                // Newtonsoft.Json has not documented any exceptions...           
            }
        }

        private void RegisterChangeEvents([NotNull][ItemNotNull] ITranslator[] translators)
        {
            foreach (var translator in translators)
            {
                translator.PropertyChanged += (_, __) => SaveConfiguration();

                foreach (var credential in translator.Credentials)
                {
                    credential.PropertyChanged += (_, __) => SaveConfiguration();
                }
            }
        }

        public void Dispose()
        {
            ActiveSession?.Dispose();
        }
    }
}
