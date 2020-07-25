namespace ResXManager
{
    using System;
    using System.Collections.ObjectModel;
    using System.Composition;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [VisualCompositionExport(RegionId.Content, Sequence = 99)]
    [Export(typeof(ITracer))]
    [Shared]
    public sealed class OutputViewModel : ObservableObject, ITracer
    {
        public ObservableCollection<string> Lines { get; } = new ObservableCollection<string>();

        public ICommand CopyCommand => new DelegateCommand(Copy);

        private void Copy()
        {
            Clipboard.SetText(string.Join(Environment.NewLine, Lines));
        }

        private void Append(string prefix, string value)
        {
            var lines = value.Split('\n');

            Lines.Add(DateTime.Now.ToShortTimeString() + "\t" + prefix + lines[0].Trim('\r'));
            Lines.AddRange(lines.Skip(1).Select(l => l.Trim('\r')));
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
    }
}
