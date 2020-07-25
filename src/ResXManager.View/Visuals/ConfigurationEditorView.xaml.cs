namespace ResXManager.View.Visuals
{
    using System;
    using System.Composition;
    using System.IO;
    using System.Windows;

    using ResXManager.Infrastructure;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.AttributedModel;
    using TomsToolbox.Wpf.Converters;

    /// <summary>
    /// Interaction logic for ConfigurationEditorView.xaml
    /// </summary>
    [DataTemplate(typeof(ConfigurationEditorViewModel))]
    public partial class ConfigurationEditorView
    {
        private readonly ITracer _tracer;

        [ImportingConstructor]
        public ConfigurationEditorView(IExportProvider exportProvider, ITracer tracer)
        {
            _tracer = tracer;

            try
            {
                this.SetExportProvider(exportProvider);

                InitializeComponent();
            }
            catch (Exception ex)
            {
                exportProvider.TraceXamlLoaderError(ex);
            }
        }

        private void CommandConverter_Error(object sender, ErrorEventArgs e)
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
    }
}
