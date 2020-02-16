namespace ResXManager.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Windows.Threading;

    using JetBrains.Annotations;

    public interface ITranslationSession : IDisposable
    {
        bool IsActive { get; }

        bool IsCanceled { get; }

        bool IsComplete { get; }

        CancellationToken CancellationToken { get; }

        void Cancel();

        [ItemNotNull]
        ICollection<ITranslationItem> Items { get; }

        [ItemNotNull]
        IList<string> Messages { get; }

        CultureInfo NeutralResourcesLanguage { get; }

        int Progress { get; set; }

        CultureInfo SourceLanguage { get; }

        Dispatcher Dispatcher { get; }

        void AddMessage([NotNull] string text);
    }

    public static class SessionExtensionMethods
    {
        public static bool IsRunning(this ITranslationSession session)
        {
            return session.IsActive && !session.IsCanceled;
        }
    }
}