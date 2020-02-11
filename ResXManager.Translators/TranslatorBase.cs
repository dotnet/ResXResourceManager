namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    [DataContract]
    public abstract class TranslatorBase : ITranslator
    {
        [NotNull]
        private static readonly Regex _removeKeyboardShortcutIndicatorsRegex = new Regex(@"[&_](?=[\w\d])", RegexOptions.Compiled);

        [NotNull]
        protected static readonly IWebProxy WebProxy = TryGetDefaultProxy();

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

        public virtual bool SupportsHtml => false;

        [DataMember]
        public bool AutoDetectHtml { get; set; }

        public virtual bool HasCharacterRateLimit => false;

        [DataMember]
        public int MaxCharactersPerMinute { get; set; }

        public abstract void Translate(ITranslationSession translationSession);

        [NotNull]
        protected static string RemoveKeyboardShortcutIndicators([NotNull] string value)
        {
            return _removeKeyboardShortcutIndicatorsRegex.Replace(value, string.Empty);
        }

        private static IWebProxy TryGetDefaultProxy()
        {
            try
            {
                var webProxy = WebRequest.DefaultWebProxy ?? new WebProxy();
                webProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                return webProxy;
            }
            catch
            {
                return new WebProxy();
            }
        }

        [UsedImplicitly]
        public event PropertyChangedEventHandler PropertyChanged;
    }
}