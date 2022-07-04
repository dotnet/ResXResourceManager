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
    using ResXManager.VSIX.Compatibility;

    using Throttle;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    using static Microsoft.VisualStudio.Shell.ThreadHelper;

    [Export(typeof(IVsixShellViewModel))]
    internal sealed class VsixShellViewModel : ObservableObject, IVsixShellViewModel
    {
        private readonly ResourceViewModel _resourceViewModel;
        private readonly ShellViewModel _shellViewModel;
        private readonly IVsixCompatibility _vsixCompatibility;

        [ImportingConstructor]
        public VsixShellViewModel(ResourceManager resourceManager, ResourceViewModel resourceViewModel, ShellViewModel shellViewModel, IVsixCompatibility vsixCompatibility)
        {
            _resourceViewModel = resourceViewModel;
            _shellViewModel = shellViewModel;
            _vsixCompatibility = vsixCompatibility;
            resourceManager.Loaded += (_, __) => SelectedCodeGeneratorsChanged();

            resourceViewModel.SelectedEntities.CollectionChanged += (_, __) => SelectedCodeGeneratorsChanged();
        }

        public ICommand SetCodeProviderCommand => new DelegateCommand<CodeGenerator>(CanSetCodeProvider, SetCodeProvider);

        public CodeGenerator SelectedCodeGenerators
        {
            get
            {
                ThrowIfNotOnUIThread();

                var items = SelectedItemsCodeGenerators().ToArray();
                var generator = items.FirstOrDefault();

                if ((items.Length == 1) && Enum.IsDefined(typeof(CodeGenerator), generator))
                    return generator;

                return CodeGenerator.None;
            }
        }

        public void SelectEntry(ResourceTableEntry entry)
        {
            ThrowIfNotOnUIThread();

            VsPackage.Instance.ShowToolWindow();

#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            // Must defer selection until tool window is fully shown!
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => _shellViewModel.SelectEntry(entry));
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
        }

        public IMoveToResourceViewModel CreateMoveToResourceViewModel(ICollection<string> patterns, ICollection<ResourceEntity> resourceEntities, string text, string extension, string? className, string? functionName, string fileName)
        {
            return new MoveToResourceViewModel(_vsixCompatibility, patterns, resourceEntities, text, extension, className, functionName, fileName);
        }

        private IEnumerable<CodeGenerator> SelectedItemsCodeGenerators()
        {
            ThrowIfNotOnUIThread();

            return _resourceViewModel.SelectedEntities
                .Select(x => x.NeutralProjectFile)
                .OfType<IDteProjectFile>()
                .Select(pf => pf.CodeGenerator)
                .Distinct();
        }

        private bool CanSetCodeProvider(CodeGenerator obj)
        {
            ThrowIfNotOnUIThread();

            return SelectedItemsCodeGenerators().All(g => g != CodeGenerator.None);
        }

        private void SetCodeProvider(CodeGenerator codeGenerator)
        {
            ThrowIfNotOnUIThread();

            _resourceViewModel.SelectedEntities
                .Select(x => x.NeutralProjectFile)
                .OfType<IDteProjectFile>()
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
