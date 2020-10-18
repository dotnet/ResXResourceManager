namespace ResXManager.VSIX.Visuals
{
    using System.Composition;

    using ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition.AttributedModel;

    [VisualCompositionExport(RegionId.Content, Sequence = 100)]
    [Shared]
    public class ColorViewModel
    {
        public override string ToString()
        {
            return "Colors";
        }
    }
}
