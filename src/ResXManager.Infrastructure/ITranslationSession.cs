namespace ResXManager.Infrastructure;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

public interface ITranslationSession : IDisposable
{
    bool IsActive { get; }

    bool IsCanceled { get; }

    bool IsComplete { get; }

    CancellationToken CancellationToken { get; }

    void Cancel();

    ICollection<ITranslationItem> Items { get; }

    IList<string> Messages { get; }

    CultureInfo NeutralResourcesLanguage { get; }

    int Progress { get; set; }

    CultureInfo SourceLanguage { get; }

    TaskFactory MainThread { get; }

    void AddMessage(string text);
}