namespace ResXManager
{
    using System;
    using System.Composition;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    [DataTemplate(typeof(MainViewModel))]
    public partial class MainView
    {
        [ImportingConstructor]
        public MainView([NotNull] IExportProvider exportProvider)
        {
            try
            {
                this.SetExportProvider(exportProvider);

                InitializeComponent();
            }
            catch (Exception ex)
            {
                exportProvider.TraceError(ex.ToString());
            }
        }
    }
}
