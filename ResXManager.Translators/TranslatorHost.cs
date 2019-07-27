namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Threading;

    using JetBrains.Annotations;

    using Throttle;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Translators.Properties;

    using TomsToolbox.Wpf;

    [Export]
    public class TranslatorHost
    {
        private readonly ITranslator[] _translators;

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

        public void Translate([NotNull] ITranslationSession translationSession)
        {
            var translatorCounter = 0;

            foreach (var translator in Translators)
            {
                var local = translator;
                if (!local.IsEnabled)
                    continue;

                Interlocked.Increment(ref translatorCounter);

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        local.Translate(translationSession);
                    }
                    finally
                    {
                        // ReSharper disable once AccessToModifiedClosure
                        if (Interlocked.Decrement(ref translatorCounter) == 0)
                        {
                            translationSession.IsComplete = true;
                        }
                    }
                });
            }

            if (translatorCounter == 0)
            {
                translationSession.IsComplete = true;
            }
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
    }
}
