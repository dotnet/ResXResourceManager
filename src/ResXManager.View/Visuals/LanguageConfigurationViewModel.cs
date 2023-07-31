namespace ResXManager.View.Visuals
{
    using System.ComponentModel;
    using System.Composition;

    using ResXManager.Infrastructure;
    using ResXManager.View.Properties;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [VisualCompositionExport(RegionId.Content, Sequence = 4)]
    [Shared]
    internal sealed partial class LanguageConfigurationViewModel : INotifyPropertyChanged
    {
        public override string ToString()
        {
            return Resources.ShellTabHeader_Languages;
        }
    }
}
