﻿namespace ResXManager.VSIX.Properties;

using System.Collections.Generic;

using ResXManager.Infrastructure;

using TomsToolbox.Essentials;
using TomsToolbox.ObservableCollections;

public sealed partial class Settings
{
    static Settings()
    {
        Default.PropertyChanged += (sender, _) => ((Settings)sender).Save();
    }

    public Settings()
    {
        try
        {
            var values = JsonConvert.DeserializeObject<KeyValuePair<string, int>[]>(MoveToResourcePreferedReplacementPatterns);
            values?.ForEach(value => MoveToResourcePreferedReplacementPatternIndex[value.Key] = value.Value);
        }
        catch
        {
            // invalid source, go with default...
        }

        try
        {
            var values = JsonConvert.DeserializeObject<KeyValuePair<string, int>[]>(MoveToResourcePreferedKeyPatterns);
            values?.ForEach(value => MoveToResourcePreferedKeyPatternIndex[value.Key] = value.Value);
        }
        catch
        {
            // invalid source, go with default...
        }

        MoveToResourcePreferedReplacementPatternIndex.CollectionChanged += (_, __) => MoveToResource_PreferedReplacementPatternIndex_Changed();
        MoveToResourcePreferedReplacementPatternIndex.PropertyChanged += (_, __) => MoveToResource_PreferedReplacementPatternIndex_Changed();

        MoveToResourcePreferedKeyPatternIndex.CollectionChanged += (_, __) => MoveToResource_PreferedKeyPatternIndex_Changed();
        MoveToResourcePreferedKeyPatternIndex.PropertyChanged += (_, __) => MoveToResource_PreferedKeyPatternIndex_Changed();
    }

    public ObservableIndexer<string, int> MoveToResourcePreferedReplacementPatternIndex { get; } = new(_ => 0);

    public ObservableIndexer<string, int> MoveToResourcePreferedKeyPatternIndex { get; } = new(_ => 0);

    private void MoveToResource_PreferedReplacementPatternIndex_Changed()
    {
        MoveToResourcePreferedReplacementPatterns = JsonConvert.SerializeObject(MoveToResourcePreferedReplacementPatternIndex);
    }

    private void MoveToResource_PreferedKeyPatternIndex_Changed()
    {
        MoveToResourcePreferedKeyPatterns = JsonConvert.SerializeObject(MoveToResourcePreferedKeyPatternIndex);
    }
}
