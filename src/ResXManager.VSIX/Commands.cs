namespace ResXManager.VSIX
{
    using System.Composition;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [Export]
    [VisualCompositionExport(RegionId.ProjectListContextMenu)]
    [LocalizedDisplayName(StringResourceKey.CodeGenerator_CommandGroup)]
    [Text(SubRegionIdKey, @"CodeGen")]
    public class CodeGeneratorGroupCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(@"CodeGen")]
    [LocalizedDisplayName(StringResourceKey.CodeGenerator_CommandInternal)]
    [Text(IsCheckableKey, @"True")]
    public class ResXFileCodeGeneratorCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(@"CodeGen")]
    [LocalizedDisplayName(StringResourceKey.CodeGenerator_CommandPublic)]
    [Text(IsCheckableKey, @"True")]
    public class PublicResXFileCodeGeneratorCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(@"CodeGen")]
    [LocalizedDisplayName(StringResourceKey.CodeGenerator_CommandTextTemplate)]
    [Text(IsCheckableKey, @"True")]
    public class TextTemplateCodeGeneratorCommand : CommandSourceFactory
    {
    }
}
