namespace ResXManager.Model
{
    using System;

    using JetBrains.Annotations;

    public class LanguageEventArgs : EventArgs
    {
        public LanguageEventArgs([NotNull] ResourceLanguage language)
        {
            Language = language;
        }

        [NotNull]
        public ResourceLanguage Language { get; }
    }
}
