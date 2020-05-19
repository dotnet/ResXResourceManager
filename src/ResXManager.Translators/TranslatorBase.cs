namespace ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;

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

        public bool IsActive { get; protected set; }

        [DataMember]
        public bool SaveCredentials { get; set; }

        [DataMember]
        public double Ranking { get; set; } = 1.0;

        public IList<ICredentialItem> Credentials { get; }

        async Task ITranslator.Translate(ITranslationSession translationSession)
        {
            try
            {
                IsActive = true;

                await Translate(translationSession);
            }
            catch (Exception ex)
            {
                translationSession.AddMessage(DisplayName + ": " + string.Join(" => ", ex.ExceptionChain().Select(item => item.Message)));
            }
            finally
            {
                IsActive = false;
            }
        }

        protected abstract Task Translate(ITranslationSession translationSession);

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