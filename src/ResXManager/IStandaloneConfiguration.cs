namespace ResXManager
{
    using ResXManager.Properties;

    public enum ColorTheme
    {
        [LocalizedDisplayName(StringResourceKey.ColorTheme_System)]
        System,

        [LocalizedDisplayName(StringResourceKey.ColorTheme_Light)]
        Light,

        [LocalizedDisplayName(StringResourceKey.ColorTheme_Dark)]
        Dark
    }

    public interface IStandaloneConfiguration
    {
        ColorTheme ColorTheme { get; set; }
    }
}