namespace tomenglertde.ResXManager
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    [DataTemplate(typeof(MainViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class MainView
    {
        [ImportingConstructor]
        public MainView([NotNull] ExportProvider exportProvider)
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
