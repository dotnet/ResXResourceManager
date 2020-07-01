namespace ResXManager.View
{
    using System.Composition;

    using ResXManager.Infrastructure;
    using ResXManager.View.Properties;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [Export, Shared]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu, RegionId.ResourceTableItemContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Delete)]
    [Text(IconUriKey, "pack://application:,,,/ResXManager.View;component/Assets/delete.png")]
    [Text(GroupNameKey, "Edit")]
    public class DeleteCommand : CommandSourceFactory
    {
    }

    [Export, Shared]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Cut)]
    [Text(IconUriKey, "pack://application:,,,/ResXManager.View;component/Assets/cut.png")]
    [Text(GroupNameKey, "Edit")]
    public class CutCommand : CommandSourceFactory
    {
    }

    [Export, Shared]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu, RegionId.ResourceTableItemContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Copy)]
    [Text(IconUriKey, "pack://application:,,,/ResXManager.View;component/Assets/copy.png")]
    [Text(GroupNameKey, "Edit")]
    public class CopyCommand : CommandSourceFactory
    {
    }

    [Export, Shared]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu, RegionId.ProjectListContextMenu, RegionId.ResourceTableItemContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Paste)]
    [Text(IconUriKey, "pack://application:,,,/ResXManager.View;component/Assets/paste.png")]
    [Text(GroupNameKey, "Edit")]
    public class PasteCommand : CommandSourceFactory
    {
    }

    [Export, Shared]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Invariant)]
    [Text(IsCheckableKey, "True")]
    [Text(GroupNameKey, "Validation")]
    public class IsInvariantCommand : CommandSourceFactory
    {
    }

    [Export, Shared]
    [VisualCompositionExport(RegionId.ResourceTableItemContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Invariant)]
    [Text(IsCheckableKey, "True")]
    [Text(GroupNameKey, "Validation")]
    public class IsItemInvariantCommand : CommandSourceFactory
    {
    }

    [Export, Shared]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu)]
    [LocalizedDisplayName(StringResourceKey.Configuration_Rules)]
    [Text(SubRegionIdKey, Region)]
    [Text(GroupNameKey, "Validation")]
    public class ConsistencyChecksCommand : CommandSourceFactory
    {
        public const string Region = "ConsistencyChecks";
    }

    [Export, Shared]
    [VisualCompositionExport(ConsistencyChecksCommand.Region, Sequence = 1)]
    [Model.Properties.LocalizedDisplayName(Model.Properties.StringResourceKey.ResourceTableEntryRuleStringFormat_Name)]
    [Text(IsCheckableKey, "True")]
    public class ToggleConsistencyCheckStringFormatCommand : CommandSourceFactory
    {
    }

    [Export, Shared]
    [VisualCompositionExport(ConsistencyChecksCommand.Region, Sequence = 2)]
    [Model.Properties.LocalizedDisplayName(Model.Properties.StringResourceKey.ResourceTableEntryRuleWhiteSpaceLead_Name)]
    [Text(IsCheckableKey, "True")]
    public class ToggleConsistencyCheckWhiteSpaceLeadCommand : CommandSourceFactory
    {
    }
    [Export, Shared]
    [VisualCompositionExport(ConsistencyChecksCommand.Region, Sequence = 3)]
    [Model.Properties.LocalizedDisplayName(Model.Properties.StringResourceKey.ResourceTableEntryRuleWhiteSpaceTail_Name)]
    [Text(IsCheckableKey, "True")]
    public class ToggleConsistencyCheckWhiteSpaceTailCommand : CommandSourceFactory
    {
    }

    [Export, Shared]
    [VisualCompositionExport(ConsistencyChecksCommand.Region, Sequence = 4)]
    [Model.Properties.LocalizedDisplayName(Model.Properties.StringResourceKey.ResourceTableEntryRulePunctuationLead_Name)]
    [Text(IsCheckableKey, "True")]
    public class ToggleConsistencyCheckPunctuationLeadCommand : CommandSourceFactory
    {
    }
    [Export, Shared]
    [VisualCompositionExport(ConsistencyChecksCommand.Region, Sequence = 5)]
    [Model.Properties.LocalizedDisplayName(Model.Properties.StringResourceKey.ResourceTableEntryRulePunctuationTail_Name)]
    [Text(IsCheckableKey, "True")]
    public class ToggleConsistencyCheckPunctuationTailCommand : CommandSourceFactory
    {
    }

    [Export, Shared]
    [VisualCompositionExport(RegionId.ResourceTableContextMenu, RegionId.ResourceTableItemContextMenu)]
    [LocalizedDisplayName(StringResourceKey.CellSelection)]
    [Text(IsCheckableKey, "True")]
    [Text(GroupNameKey, "Options")]
    public class ToggleCellSelectionCommand : CommandSourceFactory
    {
    }
}
