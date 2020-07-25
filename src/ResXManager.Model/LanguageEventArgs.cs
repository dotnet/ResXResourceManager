namespace ResXManager.Model
{
    using System;

    public class LanguageEventArgs : EventArgs
    {
        public LanguageEventArgs(ResourceLanguage language)
        {
            Language = language;
        }

        public ResourceLanguage Language { get; }
    }
}
