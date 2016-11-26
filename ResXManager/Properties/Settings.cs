namespace tomenglertde.ResXManager.Properties
{
    public sealed partial class Settings
    {
        static Settings()
        {
            // ReSharper disable once PossibleNullReferenceException
            Default.PropertyChanged += (sender, _) => ((Settings)sender).Save();
        }
    }
}
