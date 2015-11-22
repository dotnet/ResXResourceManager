namespace tomenglertde.ResXManager.VSIX
{
    using System.ComponentModel;
    using System.ComponentModel.Composition;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;

    [Export]
    [VisualCompositionExport(RegionId.ProjectListContextMenu)]
    [DisplayName("Code generator")]
    [Text(SubRegionIdKey, "CodeGen")]
    public class CodeGeneratorGroupCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport("CodeGen")]
    [DisplayName("Internal")]
    [Text(IsCheckableKey, "True")]
    public class ResXFileCodeGeneratorCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport("CodeGen")]
    [DisplayName("Public")]
    [Text(IsCheckableKey, "True")]
    public class PublicResXFileCodeGeneratorCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport("CodeGen")]
    [DisplayName("Text Template")]
    [Text(IsCheckableKey, "True")]
    public class TextTemplateCodeGeneratorCommand : CommandSourceFactory
    {
    }
}
