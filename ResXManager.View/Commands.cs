namespace tomenglertde.ResXManager.View
{
    using System.ComponentModel.Composition;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf.Composition;

    [Export]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Delete)]
    [Text(IconUriKey, "pack://application:,,,/ResXManager.View;component/Assets/delete.png")]
    public class DeleteCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Cut)]
    [Text(IconUriKey, "pack://application:,,,/ResXManager.View;component/Assets/cut.png")]
    public class CutCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Copy)]
    [Text(IconUriKey, "pack://application:,,,/ResXManager.View;component/Assets/copy.png")]
    public class CopyCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu, RegionId.ProjectListContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Paste)]
    [Text(IconUriKey, "pack://application:,,,/ResXManager.View;component/Assets/paste.png")]
    public class PasteCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Invariant)]
    [Text(IsCheckableKey, "True")]
    public class IsInvariantCommand : CommandSourceFactory
    {
    }
}
