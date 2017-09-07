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
    [Text(GroupNameKey, "Edit")]
    public class DeleteCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Cut)]
    [Text(IconUriKey, "pack://application:,,,/ResXManager.View;component/Assets/cut.png")]
    [Text(GroupNameKey, "Edit")]
    public class CutCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu, RegionId.ResourceTableItemContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Copy)]
    [Text(IconUriKey, "pack://application:,,,/ResXManager.View;component/Assets/copy.png")]
    [Text(GroupNameKey, "Edit")]
    public class CopyCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu, RegionId.ProjectListContextMenu, RegionId.ResourceTableItemContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Paste)]
    [Text(IconUriKey, "pack://application:,,,/ResXManager.View;component/Assets/paste.png")]
    [Text(GroupNameKey, "Edit")]
    public class PasteCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Invariant)]
    [Text(IsCheckableKey, "True")]
    [Text(GroupNameKey, "Edit")]
    public class IsInvariantCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(RegionId.ResourceTableItemContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Invariant)]
    [Text(IsCheckableKey, "True")]
    [Text(GroupNameKey, "Edit")]
    public class IsItemInvariantCommand : CommandSourceFactory
    {
    }

    [Export]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu, RegionId.ResourceTableItemContextMenu)]
    [LocalizedDisplayName(StringResourceKey.CellSelection)]
    [Text(IsCheckableKey, "True")]
    [Text(GroupNameKey, "Options")]
    public class ToggleCellSelectionCommand : CommandSourceFactory
    {
    }
}
