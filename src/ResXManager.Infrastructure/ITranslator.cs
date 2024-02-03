namespace ResXManager.Infrastructure;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

public interface ITranslator : INotifyPropertyChanged
{
    string Id { get; }

    string DisplayName { get; }

    Uri? Uri { get; }

    bool IsEnabled { get; set; }

    bool IsActive { get; }

    bool SaveCredentials { get; set; }

    double Ranking { get; set; }

    Task Translate(ITranslationSession translationSession);

    IList<ICredentialItem> Credentials { get; }
}