namespace ResXManager.Translators.Properties;

internal sealed partial class Settings
{
    static Settings()
    {
        Default.PropertyChanged += (sender, _) => ((Settings)sender).Save();
    }
}
