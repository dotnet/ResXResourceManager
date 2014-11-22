namespace tomenglertde.ResXManager.Model.Properties
{
    using System;
    using System.Text.RegularExpressions;

    public sealed partial class Settings
    {
        static Settings()
        {
            Default.PropertyChanged += (sender, _) => ((Settings)sender).Save();
            Default.SettingChanging += Default_SettingChanging;
        }

        static void Default_SettingChanging(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            if (e.SettingName.StartsWith("DetectCodeReferences_"))
            {
                new Regex(e.NewValue as string ?? string.Empty);
            }
        }
    }
}
