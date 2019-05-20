namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Desktop;

    [DataContract]
    public abstract class TranslatorBase : ObservableObject, ITranslator
    {
        [NotNull]
        private static readonly Regex _removeKeyboardShortcutIndicatorsRegex = new Regex(@"[&_](?=[\w\d])", RegexOptions.Compiled);

        [NotNull]
        protected static readonly IWebProxy WebProxy = new WebProxy();

        // ReSharper disable once NotNullMemberIsNotInitialized
        static TranslatorBase()
        {
            try
            {
                WebProxy = WebRequest.DefaultWebProxy ?? new WebProxy();
                WebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            }
            catch
            {
                // just use default...
            }
        }

        protected TranslatorBase([NotNull] string id, [NotNull] string displayName, [CanBeNull] Uri uri, [CanBeNull][ItemNotNull] IList<ICredentialItem> credentials)
        {
            Id = id;
            DisplayName = displayName;
            Uri = uri;
            Credentials = credentials ?? Array.Empty<ICredentialItem>();
        }

        public string Id { get; }

        public string DisplayName { get; }

        [CanBeNull]
        public Uri Uri { get; }

        [DataMember]
        public bool IsEnabled { get; set; } = true;

        [DataMember]
        public bool SaveCredentials { get; set; }

        public IList<ICredentialItem> Credentials { get; }

        public abstract void Translate(ITranslationSession translationSession);

        [NotNull]
        protected static string RemoveKeyboardShortcutIndicators([NotNull] string value)
        {
            return _removeKeyboardShortcutIndicatorsRegex.Replace(value, string.Empty);
        }
    }
}