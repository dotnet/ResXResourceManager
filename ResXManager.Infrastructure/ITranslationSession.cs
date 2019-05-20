namespace tomenglertde.ResXManager.Infrastructure
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    public interface ITranslationSession
    {
        bool IsActive { get; }

        bool IsCanceled { get; }

        bool IsComplete { get; set; }

        [NotNull]
        [ItemNotNull]
        ICollection<ITranslationItem> Items { get; }

        [NotNull]
        [ItemNotNull]
        IList<string> Messages { get; }

        [NotNull]
        CultureInfo NeutralResourcesLanguage { get; }

        int Progress { get; set; }

        [NotNull]
        CultureInfo SourceLanguage { get; }

        [NotNull]
        Dispatcher Dispatcher { get; }

        void AddMessage([NotNull] string text);

        void Cancel();
    }
}