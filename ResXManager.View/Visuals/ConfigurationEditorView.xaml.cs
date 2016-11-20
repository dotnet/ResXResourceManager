namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Windows;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Converters;

    /// <summary>
    /// Interaction logic for ConfigurationEditorView.xaml
    /// </summary>
    [DataTemplate(typeof(ConfigurationEditorViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ConfigurationEditorView
    {
        [NotNull]
        private readonly ITracer _tracer;

        [ImportingConstructor]
        public ConfigurationEditorView(ExportProvider exportProvider, [NotNull] ITracer tracer)
        {
            Contract.Requires(tracer != null);

            try
            {

                this.SetExportProvider(exportProvider);

                _tracer = tracer;

                InitializeComponent();
            }
            catch (Exception ex)
            {
                tracer.TraceError(ex.ToString());
            }
        }

        private void CommandConverter_Error(object sender, [NotNull] ErrorEventArgs e)
        {
            var ex = e.GetException();
            if (ex == null)
                return;

            _tracer.TraceError(ex.ToString());

            MessageBox.Show(ex.Message, Properties.Resources.Title);
        }

        private void SortNodesByKeyCommandConverter_Executing(object sender, ConfirmedCommandEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.SortNodesByKey_Confirmation, Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
        }
    }
}
