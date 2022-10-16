namespace ResXManager.View.Properties
{
    public sealed partial class Settings
    {
        static Settings()
        {
            Default.PropertyChanged += (sender, _) => (sender as Settings)?.Save();
        }
    }
}
