namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
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

    using Settings = tomenglertde.ResXManager.Model.Properties.Settings;

    [VisualCompositionExport(RegionId.Content, Sequence = 2)]
    internal class TranslationsViewModel : ObservableObject
    {
        [NotNull]
        private readonly TranslatorHost _translatorHost;
        [NotNull]
        private readonly ResourceManager _resourceManager;
        [NotNull]
        private readonly ResourceViewModel _resourceViewModel;
        [NotNull]
        private readonly ObservableCollection<ITranslationItem> _selectedItems = new ObservableCollection<ITranslationItem>();
        [NotNull]
        private readonly ObservableCollection<CultureKey> _selectedTargetCultures = new ObservableCollection<CultureKey>();


        [ImportingConstructor]
        public TranslationsViewModel([NotNull] TranslatorHost translatorHost, [NotNull] ResourceManager resourceManager, [NotNull] ResourceViewModel resourceViewModel, [NotNull] Configuration configuration)
        {
            Contract.Requires(translatorHost != null);
            Contract.Requires(resourceManager != null);
            Contract.Requires(resourceViewModel != null);
            Contract.Requires(configuration != null);

            _translatorHost = translatorHost;
            _resourceManager = resourceManager;
            _resourceViewModel = resourceViewModel;

            Configuration = configuration;

            _resourceManager.Loaded += ResourceManager_Loaded;

            SourceCulture = _resourceManager.Cultures.FirstOrDefault();

            _selectedTargetCultures.CollectionChanged += SelectedTargetCultures_CollectionChanged;
        }

        [NotNull]
        public ObservableCollection<CultureKey> Cultures => _resourceManager.Cultures;

        [NotNull]
        public Configuration Configuration { get; }

        [NotNull]
        public IEnumerable<ITranslator> Translators => _translatorHost.Translators;

        public CultureKey SourceCulture { get; set; }

        [UsedImplicitly] // PropertyChanged.Fody
        private void OnSourceCultureChanged()
        {
            AllTargetCultures = _resourceManager
                .Cultures
                .ObservableWhere(key => key != SourceCulture);
        }

        [NotNull]
        public ICollection<CultureKey> AllTargetCultures { get; private set; } = new CultureKey[0];

        [UsedImplicitly] // PropertyChanged.Fody
        private void OnAllTargetCulturesChanged()
        {
            _selectedTargetCultures.SynchronizeWith(AllTargetCultures.Except(UnselectedTargetCultures).ToArray());
        }

        [NotNull]
        public ICollection<CultureKey> SelectedTargetCultures => _selectedTargetCultures;

        [NotNull]
        public ICollection<ITranslationItem> Items { get; private set; } = new ITranslationItem[0];

        [NotNull]
        public ICollection<ITranslationItem> SelectedItems => _selectedItems;

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

        private void ResourceManager_Loaded(object sender, EventArgs e)
        {
            if ((SourceCulture == null) || !_resourceManager.Cultures.Contains(SourceCulture))
                SourceCulture = _resourceManager.Cultures.FirstOrDefault();

            Items = new TranslationItem[0];
        }

        private void Stop()
        {
            TranslationSession?.Cancel();
        }

        private void Apply([NotNull] IEnumerable<ITranslationItem> items)
        {
            Contract.Requires(items != null);

            var prefix = Configuration.EffectiveTranslationPrefix;

            foreach (var item in items.Where(item => !string.IsNullOrEmpty(item.Translation)).ToArray())
            {
                Contract.Assume(item != null);

                if (!item.Apply(prefix))
                    break;

                Items.Remove(item);
            }
        }

        private bool IsSessionComplete => TranslationSession != null && TranslationSession.IsComplete;

        private bool IsSessionRunning => TranslationSession != null && !TranslationSession.IsComplete && !TranslationSession.IsCanceled;

        [NotNull]
        private static IEnumerable<CultureKey> UnselectedTargetCultures
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<CultureKey>>() != null);

                return (Settings.Default.TranslationUnselectedTargetCultures ?? string.Empty).Split(',').Select(c => c.ToCultureKey()).Where(c => c != null);
            }
            set
            {
                Contract.Requires(value != null);

                Settings.Default.TranslationUnselectedTargetCultures = string.Join(",", value);
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

            var itemsToTranslate = GetItemsToTranslate(_resourceViewModel.ResourceTableEntries, sourceCulture, _selectedTargetCultures, Configuration.EffectiveTranslationPrefix);

            Items = new ObservableCollection<ITranslationItem>(itemsToTranslate);

            TranslationSession = new TranslationSession(sourceCulture.Culture, Configuration.NeutralResourcesLanguage, itemsToTranslate);

            _translatorHost.Translate(TranslationSession);
        }

        [NotNull, ItemNotNull]
        private static ICollection<ITranslationItem> GetItemsToTranslate([NotNull, ItemNotNull] IEnumerable<ResourceTableEntry> resourceTableEntries, CultureKey sourceCulture, [NotNull, ItemNotNull] ObservableCollection<CultureKey> targetCultures, string translationPrefix)
        {
            Contract.Requires(resourceTableEntries != null);
            Contract.Requires(targetCultures != null);
            Contract.Ensures(Contract.Result<IEnumerable<ITranslationItem>>() != null);

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
                .Where(item => !HasTranslation(item.Target))
                .Select(item => new TranslationItem(item.Entry, item.Source, item.TargetCulture))
                .ToArray();

            Contract.Assume(itemsToTranslate != null);

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
                        if (itemsWithTranslations.TryGetValue(item.Source, out var translations))
                        {
                            foreach (var translation in translations.GroupBy(t => t.Target))
                            {
                                item.Results.Add(new TranslationMatch(null, translation.Key, translation.Count()));
                            }
                        }
                    });
            }

            return itemsToTranslate;
        }

        private void SelectedTargetCultures_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UnselectedTargetCultures = AllTargetCultures.Concat(UnselectedTargetCultures).Distinct().Except(_selectedTargetCultures);
        }

        public override string ToString() => Resources.ShellTabHeader_Translate;

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_translatorHost != null);
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_resourceViewModel != null);
            Contract.Invariant(Configuration != null);
            Contract.Invariant(_selectedItems != null);
            Contract.Invariant(_selectedTargetCultures != null);
            Contract.Invariant(Items != null);
            Contract.Invariant(AllTargetCultures != null);
        }
    }
}
