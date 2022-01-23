namespace ResXManager.Translators;

using System.ComponentModel;

using ResXManager.Infrastructure;

public class CredentialItem : ICredentialItem
{
    public CredentialItem(string key, string description, bool isPassword = true)
    {
        Key = key;
        Description = description;
        IsPassword = isPassword;
    }

    public string Key { get; }

    public string Description { get; }

    public string? Value { get; set; }

    public bool IsPassword { get; }

#pragma warning disable CS0067
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
}