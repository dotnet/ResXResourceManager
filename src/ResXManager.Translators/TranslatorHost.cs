﻿namespace ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using ResXManager.Infrastructure;
    using ResXManager.Translators.Properties;

    using Throttle;
    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    [Export, Shared]
    public sealed class TranslatorHost : IDisposable
    {
        private readonly ITranslator[] _translators;
        private readonly TaskFactory _mainThread = new(TaskScheduler.FromCurrentSynchronizationContext());

        private ITranslationSession? _activeSession;

        [ImportingConstructor]
        public TranslatorHost([ImportMany] ITranslator[] translators)
        {
            _translators = translators;

            var settings = Settings.Default;

            if (settings.upgradeNeeded)
            {
                settings.Upgrade();
                settings.Save();
                settings.Reload();
                settings.upgradeNeeded = false;
                settings.Save();
            }

            var configuration = settings.Configuration;

            LoadConfiguration(translators, configuration);

            RegisterChangeEvents(translators);
        }

        public event EventHandler<EventArgs>? SessionStateChanged;

        public IEnumerable<ITranslator> Translators => _translators;

        public ITranslationSession? ActiveSession => _activeSession;

        public void StartSession(CultureInfo? sourceLanguage, CultureInfo neutralResourcesLanguage, ICollection<ITranslationItem> items)
        {
            Task.Run(() =>
            {
                var session = new TranslationSession(_mainThread, sourceLanguage, neutralResourcesLanguage, items);
                Interlocked.Exchange(ref _activeSession, session)?.Dispose();

                SessionStateChanged?.Invoke(this, EventArgs.Empty);

                try
                {
                    var translatorTasks = Translators
                        .Where(t => t.IsEnabled)
                        .Select(t => t.Translate(session))
                        .ToArray();

                    Task.WaitAll(translatorTasks);
                }
                finally
                {
                    session.Dispose();
                    SessionStateChanged?.Invoke(this, EventArgs.Empty);
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
                var json = JsonConvert.SerializeObject(translator) ?? string.Empty;
                values[translator.Id] = json;
            }

            settings.Configuration = JsonConvert.SerializeObject(values);
        }

        private static void LoadConfiguration(ITranslator[] translators, string? configuration)
        {
            if (configuration.IsNullOrEmpty())
                return;

            try
            {
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(configuration ?? string.Empty);
                if (values == null)
                    return;

                foreach (var translator in translators)
                {
                    if (!values.TryGetValue(translator.Id, out var setting))
                        continue;
                    if (setting.IsNullOrEmpty())
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

        private void RegisterChangeEvents(ITranslator[] translators)
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
