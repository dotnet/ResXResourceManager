namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics.Contracts;
    using System.Threading;

    using Newtonsoft.Json;

    using tomenglertde.ResXManager.Translators.Properties;

    public static class TranslatorHost
    {
        public static readonly ITranslator[] Translators = 
        {
            new BingTranslator(),
            new GoogleTranslator(), 
            new GoogleWebTranslator(), 
        };

        static TranslatorHost()
        {
            var settings = Settings.Default;
            var configuration = settings.Configuration;

            if (string.IsNullOrEmpty(configuration))
                return;

            try
            {
                var values = JsonConvert.DeserializeObject<StringDictionary>(configuration);

                foreach (var translator in Translators)
                {
                    Contract.Assume(translator != null);

                    var setting = values[translator.Id];
                    if (string.IsNullOrEmpty(setting))
                        continue;

                    try
                    {
                        JsonConvert.PopulateObject(setting, translator);
                    }
                    catch // Newtonsoft.Jason has not documented any exceptions...
                    {
                    }
                }
            }
            catch // Newtonsoft.Jason has not documented any exceptions...
            {
            }
        }

        public static void Translate(Session session)
        {
            var translatorCounter = 0;

            foreach (var translator in Translators)
            {
                Contract.Assume(translator != null);

                var local = translator;
                if (!local.IsEnabled)
                    continue;

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Interlocked.Increment(ref translatorCounter);
                    try
                    {
                        local.Translate(session);
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref translatorCounter) == 0)
                        {
                            session.IsComplete = true;
                        }
                    }
                });
            }
        }

        public static void SaveConfiguration()
        {
            var settings = Settings.Default;

            var values = new StringDictionary();

            foreach (var translator in Translators)
            {
                var json = JsonConvert.SerializeObject(translator);

                values[translator.Id] = json;
            }

            settings.Configuration = JsonConvert.SerializeObject(values);
        }
    }
}
