namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Threading;
    using System.Windows.Threading;

    public static class TranslatorHost
    {
        public static readonly ITranslator[] Translators = 
        {
            new BingTranslator(),
            new GoogleTranslator(), 
        };

        public static void Translate(Dispatcher dispatcher, CultureInfo sourceCulture, CultureInfo targetCulture, IList<ITranslationItem> items)
        {
            foreach (var translator in Translators)
            {
                Contract.Assume(translator != null);

                ThreadPool.QueueUserWorkItem(_ => translator.Translate(dispatcher, sourceCulture, targetCulture, items));
            }
        }
    }
}
