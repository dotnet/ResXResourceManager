namespace ResXManager.Properties;

using System;
using System.Collections.Specialized;
using System.Linq;

public sealed partial class Settings
{
    private const int MaxRecentFolders = 10;

    static Settings()
    {
        Default.PropertyChanged += (sender, _) => ((Settings)sender).Save();
    }

    public Settings()
    {
        RecentStartupFolders ??= new();

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(StartupFolder))
            {
                RecentStartupFolders = AddStartupFolder(RecentStartupFolders, StartupFolder);
            }
        };
    }

    internal static StringCollection AddStartupFolder(StringCollection originalItems, string? folder)
    {
        if (folder is null || (originalItems.Count > 0 && string.Equals(originalItems[0], folder, StringComparison.OrdinalIgnoreCase)))
            return originalItems;

        var items = new StringCollection();

        items.AddRange(originalItems.Cast<string>().Where(item => !string.Equals(item, folder, StringComparison.OrdinalIgnoreCase)).ToArray());

        items.Insert(0, folder);

        while (items.Count > MaxRecentFolders)
        {
            items.RemoveAt(items.Count - 1);
        }

        return items;
    }
}
