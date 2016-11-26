namespace tomenglertde.ResXManager
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport(RegionId.Content, Sequence = 99)]
    [Export(typeof(ITracer))]
    public sealed class OutputViewModel : ObservableObject, ITracer
    {
        [NotNull]
        private readonly ObservableCollection<string> _lines = new ObservableCollection<string>();

        [NotNull]
        public ICollection<string> Lines
        {
            get
            {
                Contract.Ensures(Contract.Result<ICollection<string>>() != null);

                return _lines;
            }
        }

        [NotNull]
        public ICommand CopyCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(Copy);
            }
        }

        private void Copy()
        {
            Clipboard.SetText(string.Join(Environment.NewLine, _lines));
        }

        private void Append([NotNull] string prefix, [NotNull] string value)
        {
            Contract.Requires(prefix != null);
            Contract.Requires(value != null);

            var lines = value.Split('\n');

            // ReSharper disable once PossibleNullReferenceException
            _lines.Add(DateTime.Now.ToShortTimeString() + "\t" + prefix + lines[0].Trim('\r'));
            // ReSharper disable once PossibleNullReferenceException
            _lines.AddRange(lines.Skip(1).Select(l => l.Trim('\r')));
        }

        void ITracer.TraceError(string value)
        {
            Append("Error: ", value);
        }

        void ITracer.TraceWarning(string value)
        {
            Append("Warning: ", value);
        }

        void ITracer.WriteLine(string value)
        {
            Append(string.Empty, value);
        }

        public override string ToString()
        {
            return "Output";
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_lines != null);
        }
    }
}
