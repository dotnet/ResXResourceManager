namespace ResXManager.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;

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

        bool IsActive { get; }

        bool SaveCredentials { get; set; }

        double Ranking { get; set; }

        Task Translate([NotNull] ITranslationSession translationSession);

        [NotNull, ItemNotNull]
        IList<ICredentialItem> Credentials { get; }
    }
}
