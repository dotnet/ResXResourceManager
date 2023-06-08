namespace ResXManager.View.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Composition;
    using System.Linq;
    using System.Windows.Input;
    using System.Windows.Threading;

    using PropertyChanged;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.Translators;
    using ResXManager.View.Properties;

    using TomsToolbox.Essentials;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [VisualCompositionExport(RegionId.Content, Sequence = 2)]
    [Shared]
    internal sealed partial class TranslationsViewModel : INotifyPropertyChanged
    {
        public TranslatorHost TranslatorHost { get; }

        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private readonly ResourceManager _resourceManager;
        private readonly ResourceViewModel _resourceViewModel;

        [ImportingConstructor]
        public TranslationsViewModel(TranslatorHost translatorHost, ResourceManager resourceManager, ResourceViewModel resourceViewModel, IConfiguration configuration)
        {
            TranslatorHost = translatorHost;
            _resourceManager = resourceManager;
            _resourceViewModel = resourceViewModel;

            Configuration = configuration;

            _resourceManager.Loaded += ResourceManager_Loaded;

            SourceCulture = _resourceManager.Cultures.FirstOrDefault();

            var selectedTargetCultures = new ObservableCollection<CultureKey>();
            selectedTargetCultures.CollectionChanged += SelectedTargetCultures_CollectionChanged;
            SelectedTargetCultures = selectedTargetCultures;

            TranslatorHost.SessionStateChanged += (_, _) => _dispatcher.BeginInvoke(() =>
            {
                OnPropertyChanged(nameof(TranslatorHost));
                CommandManager.InvalidateRequerySuggested();
            });
        }

        public ObservableCollection<CultureKey> Cultures => _resourceManager.Cultures;

        public IConfiguration Configuration { get; }

        [OnChangedMethod(nameof(OnSourceCultureChanged))]
        public CultureKey? SourceCulture { get; set; }
        private void OnSourceCultureChanged()
        {
            AllTargetCultures = _resourceManager
                .Cultures
                .ObservableWhere(key => key != SourceCulture);
        }

        [OnChangedMethod(nameof(OnAllTargetCulturesChanged))]
        public ICollection<CultureKey> AllTargetCultures { get; private set; } = Array.Empty<CultureKey>();
        private void OnAllTargetCulturesChanged()
        {
            var selectedCultures = AllTargetCultures.Except(UnselectedTargetCultures).OrderBy(c => c).ToArray();

            _dispatcher.BeginInvoke(() =>
            {
                try
                {
                    SelectedTargetCultures.SynchronizeWith(selectedCultures);
                }
                catch (InvalidOperationException)
                {
                    // collection is already changing...
                }
            });
        }

        public ICollection<CultureKey> SelectedTargetCultures { get; }

        public ICollection<ITranslationItem> Items { get; private set; } = Array.Empty<ITranslationItem>();

        public ICollection<ITranslationItem> SelectedItems { get; } = new ObservableCollection<ITranslationItem>();

        public ICommand InitCommand => new DelegateCommand(() => !HasTranslationResults, UpdateTargetList);

        public ICommand StartCommand => new DelegateCommand(() => SourceCulture != null && Items.Any() && !HasTranslationResults, StartSession);

        public ICommand DiscardCommand => new DelegateCommand(() => IsSessionComplete && HasTranslationResults, UpdateTargetList);

        public ICommand ApplyAllCommand => new DelegateCommand(() => Items.Any(item => item.Results.Any()), () => Apply(Items));

        public ICommand ApplySelectedCommand => new DelegateCommand(() => SelectedItems.Any(item => item.Results.Any()), () => Apply(SelectedItems));

        public ICommand StopCommand => new DelegateCommand(() => IsSessionRunning, Stop);

        private void ResourceManager_Loaded(object? sender, EventArgs e)
        {
            if ((SourceCulture == null) || !_resourceManager.Cultures.Contains(SourceCulture))
                SourceCulture = _resourceManager.Cultures.FirstOrDefault();

            Items = Array.Empty<TranslationItem>();
        }

        private void Stop()
        {
            TranslatorHost.ActiveSession?.Cancel();
        }

        private void Apply(IEnumerable<ITranslationItem> items)
        {
            var prefix = Configuration.EffectiveTranslationPrefix;

            var valuePrefix = Configuration.PrefixFieldType.HasFlag(PrefixFieldType.Value) ? prefix : null;
            var commentPrefix = Configuration.PrefixFieldType.HasFlag(PrefixFieldType.Comment) ? prefix : null;

            foreach (var item in items.Where(item => !string.IsNullOrEmpty(item.Translation)).ToArray())
            {
                if (!item.Apply(valuePrefix, commentPrefix))
                    break;

                Items.Remove(item);
            }
        }

        private bool IsSessionComplete => TranslatorHost.ActiveSession?.IsComplete == true;

        private bool IsSessionRunning => TranslatorHost.ActiveSession?.IsActive == true;

        private bool HasTranslationResults => Items.Any(item => item.Results.Any(r => r.Translator != null));

        private static IEnumerable<CultureKey> UnselectedTargetCultures
        {
            get => (Settings.Default.TranslationUnselectedTargetCultures ?? string.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.ToCultureKey())
                    .ExceptNullItems();
            set => Settings.Default.TranslationUnselectedTargetCultures = string.Join(",", value.Select(c => c.ToString(".")));
        }

        private void UpdateTargetList()
        {
            SelectedItems.Clear();

            var sourceCulture = SourceCulture;
            if (sourceCulture == null)
            {
                Items = Array.Empty<TranslationItem>();
                return;
            }

            var itemsToTranslate = GetItemsToTranslate(_resourceViewModel.ResourceTableEntries, sourceCulture, SelectedTargetCultures, Configuration.EffectiveTranslationPrefix);

            Items = new ObservableCollection<ITranslationItem>(itemsToTranslate);
            CommandManager.InvalidateRequerySuggested();
        }

        private void StartSession()
        {
            var sourceCulture = SourceCulture;
            if (sourceCulture == null)
                return;

            var itemsToTranslate = Items.ToList();

            TranslatorHost.StartSession(sourceCulture.Culture, Configuration.NeutralResourcesLanguage, itemsToTranslate);
        }

        private ICollection<ITranslationItem> GetItemsToTranslate(IEnumerable<ResourceTableEntry> resourceTableEntries, CultureKey? sourceCulture, ICollection<CultureKey> targetCultures, string? translationPrefix)
        {
            // #1: all entries that are not invariant and have a valid value in the source culture
            var allEntriesWithSourceValue = resourceTableEntries
                .Where(entry => !entry.IsInvariant)
                .Select(entry => (Entry: entry, Source: entry.Values.GetValue(sourceCulture)))
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

            bool HasTranslation(string? value)
            {
                return !string.IsNullOrWhiteSpace(value) &&
                       !string.Equals(value, translationPrefix, StringComparison.Ordinal);
            }

            // #3: all entries with no target
            // ! item.Source is checked in #1
            var itemsToTranslate = allEntries.AsParallel()
                .Where(item => !HasTranslation(item.Target) && !item.Entry.IsItemInvariant.GetValue(item.TargetCulture))
                .Select(item => new TranslationItem(item.Entry, item.Source!, item.TargetCulture))
                .ToArray();

            if (Configuration.AutoApplyExistingTranslations)
            {
                // #4: apply existing translations
                // ! item.Source is checked in #1
                foreach (var targetCulture in targetCultures)
                {
                    var itemsWithTranslations = allEntries.AsParallel()
                        .Where(item => item.TargetCulture == targetCulture)
                        .Where(item => HasTranslation(item.Target))
                        .GroupBy(item => item.Source!)
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
            }

            return itemsToTranslate;
        }

        private void SelectedTargetCultures_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateTargetList();
            _dispatcher.BeginInvoke(() => UnselectedTargetCultures = AllTargetCultures.Concat(UnselectedTargetCultures).Distinct().Except(SelectedTargetCultures));
        }

        public override string ToString()
        {
            return Resources.ShellTabHeader_Translate;
        }
    }
}
