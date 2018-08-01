namespace tomenglertde.ResXManager.VSIX.Properties
{
    using System.Collections.Generic;

    using JetBrains.Annotations;

    using Newtonsoft.Json;

    using TomsToolbox.Core;
    using TomsToolbox.ObservableCollections;

    public sealed partial class Settings
    {
        [NotNull]
        private readonly ObservableIndexer<string, int> _moveToResourcePreferedReplacementPatternIndex = new ObservableIndexer<string, int>(_ => 0);
        [NotNull]
        private readonly ObservableIndexer<string, int> _moveToResourcePreferedKeyPatternIndex = new ObservableIndexer<string, int>(_ => 0);

        static Settings()
        {
            Default.PropertyChanged += (sender, _) => ((Settings)sender).Save();
        }

        public Settings()
        {
            try
            {
                var values = JsonConvert.DeserializeObject<KeyValuePair<string, int>[]>(MoveToResourcePreferedReplacementPatterns);
                values?.ForEach(value => _moveToResourcePreferedReplacementPatternIndex[value.Key] = value.Value);
            }
            catch
            {
                // invalid source, go with default...
            }

            try
            {
                var values = JsonConvert.DeserializeObject<KeyValuePair<string, int>[]>(MoveToResourcePreferedKeyPatterns);
                values?.ForEach(value => _moveToResourcePreferedKeyPatternIndex[value.Key] = value.Value);
            }
            catch
            {
                // invalid source, go with default...
            }

            _moveToResourcePreferedReplacementPatternIndex.CollectionChanged += (_, __) => MoveToResource_PreferedReplacementPatternIndex_Changed();
            _moveToResourcePreferedReplacementPatternIndex.PropertyChanged += (_, __) => MoveToResource_PreferedReplacementPatternIndex_Changed();

            _moveToResourcePreferedKeyPatternIndex.CollectionChanged += (_, __) => MoveToResource_PreferedKeyPatternIndex_Changed();
            _moveToResourcePreferedKeyPatternIndex.PropertyChanged += (_, __) => MoveToResource_PreferedKeyPatternIndex_Changed();
        }

        [NotNull]
        public ObservableIndexer<string, int> MoveToResourcePreferedReplacementPatternIndex => _moveToResourcePreferedReplacementPatternIndex;

        [NotNull]
        public ObservableIndexer<string, int> MoveToResourcePreferedKeyPatternIndex => _moveToResourcePreferedKeyPatternIndex;

        private void MoveToResource_PreferedReplacementPatternIndex_Changed()
        {
            MoveToResourcePreferedReplacementPatterns = JsonConvert.SerializeObject(_moveToResourcePreferedReplacementPatternIndex);
        }

        private void MoveToResource_PreferedKeyPatternIndex_Changed()
        {
            MoveToResourcePreferedKeyPatterns = JsonConvert.SerializeObject(_moveToResourcePreferedKeyPatternIndex);
        }
    }
}
