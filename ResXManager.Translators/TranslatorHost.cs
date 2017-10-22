namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Threading;

    using JetBrains.Annotations;

    using Newtonsoft.Json;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Translators.Properties;

    using Throttle;

    using TomsToolbox.Desktop;

    [Export]
    public class TranslatorHost
    {
        [NotNull]
        [ItemNotNull]
        private readonly ITranslator[] _translators;

        [ImportingConstructor]
        public TranslatorHost([ImportMany][NotNull][ItemNotNull] ITranslator[] translators)
        {
            Contract.Requires(translators != null);

            _translators = translators;

            var settings = Settings.Default;
            // ReSharper disable once PossibleNullReferenceException
            var configuration = settings.Configuration;

            LoadConfiguration(translators, configuration);

            RegisterChangeEvents(translators);
        }

        [NotNull]
        [ItemNotNull]
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

        [Throttled(typeof(Throttle), 1000)]
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

        private static void LoadConfiguration([NotNull][ItemNotNull] ITranslator[] translators, [CanBeNull] string configuration)
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
            Contract.Requires(translators != null);

            foreach (var translator in translators)
            {
                Contract.Assume(translator != null);

                translator.PropertyChanged += (_, __) => SaveConfiguration();

                foreach (var credential in translator.Credentials)
                {
                    Contract.Assume(credential != null);

                    credential.PropertyChanged += (_, __) => SaveConfiguration();
                }
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_translators != null);
        }
    }
}
