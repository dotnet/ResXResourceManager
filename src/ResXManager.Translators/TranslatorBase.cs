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

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;

    [DataContract]
    public abstract class TranslatorBase : ITranslator
    {
        private static readonly Regex _removeKeyboardShortcutIndicatorsRegex = new(@"[&_](?=[\w\d])", RegexOptions.Compiled);

        protected static readonly IWebProxy WebProxy = TryGetDefaultProxy();

        protected TranslatorBase(string id, string displayName, Uri? uri, IList<ICredentialItem>? credentials)
        {
            Id = id;
            DisplayName = displayName;
            Uri = uri;
            Credentials = credentials ?? Array.Empty<ICredentialItem>();
        }

        public string Id { get; }

        public string DisplayName { get; }

        public Uri? Uri { get; }

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

                await Translate(translationSession).ConfigureAwait(false);
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

        protected static string RemoveKeyboardShortcutIndicators(string value)
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

#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
    }
}