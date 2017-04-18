namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Threading;

    using JetBrains.Annotations;

    using Newtonsoft.Json;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Translators.Properties;

    using TomsToolbox.Desktop;

    [Export]
    public class TranslatorHost
    {
        [NotNull]
        private readonly Throttle _changeThrottle;
        [NotNull]
        private readonly ITranslator[] _translators;

        [ImportingConstructor]
        public TranslatorHost([ImportMany][NotNull][ItemNotNull] ITranslator[] translators)
        {
            Contract.Requires(translators != null);

            _changeThrottle = new Throttle(TimeSpan.FromSeconds(1), SaveConfiguration);
            _translators = translators;

            var settings = Settings.Default;
            // ReSharper disable once PossibleNullReferenceException
            var configuration = settings.Configuration;

            LoadConfiguration(translators, configuration);

            RegisterChangeEvents(translators, _changeThrottle);
        }

        [NotNull]
        public IEnumerable<ITranslator> Translators
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<ITranslator>>() != null);

                return _translators;
            }
        }

        public void Translate([NotNull] ITranslationSession translationSession)
        {
            Contract.Requires(translationSession != null);

            var translatorCounter = 0;

            foreach (var translator in Translators)
            {
                Contract.Assume(translator != null);

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

        private void SaveConfiguration()
        {
            var settings = Settings.Default;

            var values = new Dictionary<string, string>();

            foreach (var translator in _translators)
            {
                Contract.Assume(translator != null);

                var json = JsonConvert.SerializeObject(translator);
                values[translator.Id] = json;
            }

            settings.Configuration = JsonConvert.SerializeObject(values);
        }

        private static void LoadConfiguration([NotNull] ITranslator[] translators, [CanBeNull] string configuration)
        {
            Contract.Requires(translators != null);

            if (string.IsNullOrEmpty(configuration))
                return;

            try
            {
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(configuration);
                Contract.Assume(values != null);

                foreach (var translator in translators)
                {
                    Contract.Assume(translator != null);

                    string setting;

                    if (!values.TryGetValue(translator.Id, out setting))
                        continue;
                    if (string.IsNullOrEmpty(setting))
                        continue;

                    try
                    {
                        JsonConvert.PopulateObject(setting, translator);
                    }
                    catch
                    {
                        // Newtonsoft.Jason has not documented any exceptions...
                    }
                }
            }
            catch
            {
                // Newtonsoft.Jason has not documented any exceptions...           
            }
        }

        private static void RegisterChangeEvents([NotNull] ITranslator[] translators, [NotNull] Throttle changeThrottle)
        {
            Contract.Requires(translators != null);
            Contract.Requires(changeThrottle != null);

            foreach (var translator in translators)
            {
                Contract.Assume(translator != null);

                translator.PropertyChanged += (_, __) => changeThrottle.Tick();
                foreach (var credential in translator.Credentials)
                {
                    Contract.Assume(credential != null);

                    credential.PropertyChanged += (_, __) => changeThrottle.Tick();
                }
            }
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_changeThrottle != null);
            Contract.Invariant(_translators != null);
        }
    }
}
