namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.Translators;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    using Settings = Model.Properties.Settings;

    [VisualCompositionExport(RegionId.Content, Sequence = 2)]
    internal class TranslationsViewModel : ObservableObject
    {
        [NotNull]
        private readonly TranslatorHost _translatorHost;
        [NotNull]
        private readonly ResourceManager _resourceManager;
        [NotNull]
        private readonly ResourceViewModel _resourceViewModel;

        [ImportingConstructor]
        public TranslationsViewModel([NotNull] TranslatorHost translatorHost, [NotNull] ResourceManager resourceManager, [NotNull] ResourceViewModel resourceViewModel, [NotNull] Configuration configuration)
        {
            _translatorHost = translatorHost;
            _resourceManager = resourceManager;
            _resourceViewModel = resourceViewModel;

            Configuration = configuration;

            _resourceManager.Loaded += ResourceManager_Loaded;

            SourceCulture = _resourceManager.Cultures.FirstOrDefault();

            var selectedTargetCultures = new ObservableCollection<CultureKey>();
            selectedTargetCultures.CollectionChanged += SelectedTargetCultures_CollectionChanged;
            SelectedTargetCultures = selectedTargetCultures;
        }

        [NotNull, ItemNotNull]
        public ObservableCollection<CultureKey> Cultures => _resourceManager.Cultures;

        [NotNull]
        public Configuration Configuration { get; }

        [NotNull, ItemNotNull]
        public IEnumerable<ITranslator> Translators => _translatorHost.Translators;

        [CanBeNull]
        public CultureKey SourceCulture { get; set; }

        [UsedImplicitly] // PropertyChanged.Fody
        private void OnSourceCultureChanged()
        {
            AllTargetCultures = _resourceManager
                .Cultures
                .ObservableWhere(key => key != SourceCulture);
        }

        [NotNull, ItemNotNull]
        public ICollection<CultureKey> AllTargetCultures { get; private set; } = new CultureKey[0];

        [UsedImplicitly] // PropertyChanged.Fody
        private void OnAllTargetCulturesChanged()
        {
            var selectedCultures = AllTargetCultures.Except(UnselectedTargetCultures).OrderBy(c => c).ToArray();

            Dispatcher.BeginInvoke(() => { SelectedTargetCultures.SynchronizeWith(selectedCultures); });
        }

        [NotNull, ItemNotNull]
        public ICollection<CultureKey> SelectedTargetCultures { get; }

        [NotNull, ItemNotNull]
        public ICollection<ITranslationItem> Items { get; private set; } = new ITranslationItem[0];

        [NotNull, ItemNotNull]
        public ICollection<ITranslationItem> SelectedItems { get; } = new ObservableCollection<ITranslationItem>();

        [CanBeNull]
        public ITranslationSession TranslationSession { get; set; }

        [NotNull]
        public ICommand StartCommand => new DelegateCommand(() => TranslationSession == null, UpdateTargetList);

        [NotNull]
        public ICommand RestartCommand => new DelegateCommand(() => SourceCulture != null, UpdateTargetList);

        [NotNull]
        public ICommand ApplyAllCommand => new DelegateCommand(() => IsSessionComplete && Items.Any(), () => Apply(Items));

        [NotNull]
        public ICommand ApplySelectedCommand => new DelegateCommand(() => IsSessionComplete && SelectedItems.Any(), () => Apply(SelectedItems));

        [NotNull]
        public ICommand StopCommand => new DelegateCommand(() => IsSessionRunning, Stop);

        private void ResourceManager_Loaded([NotNull] object sender, [NotNull] EventArgs e)
        {
            if ((SourceCulture == null) || !_resourceManager.Cultures.Contains(SourceCulture))
                SourceCulture = _resourceManager.Cultures.FirstOrDefault();

            Items = new TranslationItem[0];
        }

        private void Stop()
        {
            TranslationSession?.Cancel();
        }

        private void Apply([NotNull, ItemNotNull] IEnumerable<ITranslationItem> items)
        {
            var prefix = Configuration.EffectiveTranslationPrefix;

            foreach (var item in items.Where(item => !string.IsNullOrEmpty(item.Translation)).ToArray())
            {
                if (!item.Apply(prefix))
                    break;

                Items.Remove(item);
            }
        }

        private bool IsSessionComplete => TranslationSession != null && TranslationSession.IsComplete;

        private bool IsSessionRunning => TranslationSession != null && !TranslationSession.IsComplete && !TranslationSession.IsCanceled;

        [NotNull]
        [ItemNotNull]
        private static IEnumerable<CultureKey> UnselectedTargetCultures
        {
            get
            {
                return (Settings.Default.TranslationUnselectedTargetCultures ?? string.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.ToCultureKey())
                    .Where(c => c != null);
            }
            set
            {
                Settings.Default.TranslationUnselectedTargetCultures = string.Join(",", value.Select(c => c.ToString(".")));
            }
        }

        private void UpdateTargetList()
        {
            TranslationSession?.Cancel();

            SelectedItems.Clear();

            var sourceCulture = SourceCulture;

            if (sourceCulture == null)
            {
                Items = new TranslationItem[0];
                return;
            }

            var itemsToTranslate = GetItemsToTranslate(_resourceViewModel.ResourceTableEntries, sourceCulture, SelectedTargetCultures, Configuration.EffectiveTranslationPrefix);

            Items = new ObservableCollection<ITranslationItem>(itemsToTranslate);

            TranslationSession = new TranslationSession(sourceCulture.Culture, Configuration.NeutralResourcesLanguage, itemsToTranslate);

            _translatorHost.Translate(TranslationSession);
        }

        [NotNull, ItemNotNull]
        private static ICollection<ITranslationItem> GetItemsToTranslate([NotNull, ItemNotNull] IEnumerable<ResourceTableEntry> resourceTableEntries, [CanBeNull] CultureKey sourceCulture, [NotNull, ItemNotNull] ICollection<CultureKey> targetCultures, [CanBeNull] string translationPrefix)
        {
            // #1: all entries that are not invariant and have a valid value in the source culture
            var allEntriesWithSourceValue = resourceTableEntries
                .Where(entry => !entry.IsInvariant)
                .Select(entry => new
                {
                    Entry = entry,
                    Source = entry.Values.GetValue(sourceCulture),
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Source))
                .ToArray();

            // #2: all entries with target culture and target text
            var allEntries = targetCultures.SelectMany(targetCulture =>
                    allEntriesWithSourceValue
                        .Select(entry => new
                        {
                            entry.Entry,
                            entry.Source,
                            Target = entry.Entry.Values.GetValue(targetCulture),
                            TargetCulture = targetCulture
                        }))
                .ToArray();

            bool HasTranslation(string value) => !string.IsNullOrWhiteSpace(value) && !string.Equals(value, translationPrefix, StringComparison.Ordinal);

            // #3: all entries with no target
            var itemsToTranslate = allEntries.AsParallel()
                .Where(item => !HasTranslation(item.Target) && !item.Entry.IsItemInvariant.GetValue(item.TargetCulture))
                .Select(item => new TranslationItem(item.Entry, item.Source, item.TargetCulture))
                .ToArray();

            // #4: apply existing translations
            foreach (var targetCulture in targetCultures)
            {
                var itemsWithTranslations = allEntries.AsParallel()
                    .Where(item => item.TargetCulture == targetCulture)
                    .Where(item => HasTranslation(item.Target))
                    .GroupBy(item => item.Source)
                    .ToDictionary(item => item.Key);

                itemsToTranslate.AsParallel()
                    .Where(item => item.TargetCulture == targetCulture)
                    .ForAll(item =>
                    {
                        if (!itemsWithTranslations.TryGetValue(item.Source, out var translations))
                            return;

                        foreach (var translation in translations.GroupBy(t => t.Target))
                        {
                            item.Results.Add(new TranslationMatch(null, translation.Key, translation.Count()));
                        }
                    });
            }

            return itemsToTranslate;
        }

        private void SelectedTargetCultures_CollectionChanged([NotNull] object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(() => UnselectedTargetCultures = AllTargetCultures.Concat(UnselectedTargetCultures).Distinct().Except(SelectedTargetCultures));
        }

        public override string ToString() => Resources.ShellTabHeader_Translate;
    }
}
