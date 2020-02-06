namespace tomenglertde.ResXManager.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    using JetBrains.Annotations;

    public interface ITranslator : INotifyPropertyChanged
    {
        [NotNull]
        string Id { get; }

        [NotNull]
        string DisplayName { get; }

        [CanBeNull]
        Uri Uri { get; }

        bool IsEnabled { get; set; }

        bool SaveCredentials { get; set; }

        void Translate([NotNull] ITranslationSession translationSession);

        [NotNull, ItemNotNull]
        IList<ICredentialItem> Credentials { get; }

        bool MustIdentifyHtml { get; }

        bool AutoDetectHtml { get; }
    }
}
