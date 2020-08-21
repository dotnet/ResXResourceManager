namespace ResXManager.VSIX.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.Windows.Input;
    using System.Windows.Threading;

    using ResXManager.Model;
    using ResXManager.View.Visuals;

    using Throttle;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    [Export]
    public sealed class VsixShellViewModel : ObservableObject
    {
        private readonly ResourceViewModel _resourceViewModel;
        private readonly ShellViewModel _shellViewModel;

        [ImportingConstructor]
        public VsixShellViewModel(ResourceManager resourceManager, ResourceViewModel resourceViewModel, ShellViewModel shellViewModel)
        {
            _resourceViewModel = resourceViewModel;
            _shellViewModel = shellViewModel;
            resourceManager.Loaded += (_, __) => SelectedCodeGeneratorsChanged();

            resourceViewModel.SelectedEntities.CollectionChanged += (_, __) => SelectedCodeGeneratorsChanged();
        }

        public ICommand SetCodeProviderCommand => new DelegateCommand<CodeGenerator>(CanSetCodeProvider, SetCodeProvider);

        public CodeGenerator SelectedCodeGenerators
        {
            get
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

                var items = SelectedItemsCodeGenerators().ToArray();
                var generator = items.FirstOrDefault();

                if ((items.Length == 1) && Enum.IsDefined(typeof(CodeGenerator), generator))
                    return generator;

                return CodeGenerator.None;
            }
        }

        public void SelectEntry(ResourceTableEntry entry)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            VsPackage.Instance.ShowToolWindow();

            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                _shellViewModel.SelectEntry(entry);
            });
        }

        private IEnumerable<CodeGenerator> SelectedItemsCodeGenerators()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            return _resourceViewModel.SelectedEntities
                .Select(x => x.NeutralProjectFile)
                .OfType<DteProjectFile>()
                .Select(pf => pf.CodeGenerator)
                .Distinct();
        }

        private bool CanSetCodeProvider(CodeGenerator obj)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            return SelectedItemsCodeGenerators().All(g => g != CodeGenerator.None);
        }

        private void SetCodeProvider(CodeGenerator codeGenerator)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            _resourceViewModel.SelectedEntities
                .Select(x => x.NeutralProjectFile)
                .OfType<DteProjectFile>()
                .ForEach(pf => pf.CodeGenerator = codeGenerator);

            SelectedCodeGeneratorsChanged();
        }

        [Throttled(typeof(DispatcherThrottle))]
        private void SelectedCodeGeneratorsChanged()
        {
            OnPropertyChanged(nameof(SelectedCodeGenerators));
        }
    }
}
