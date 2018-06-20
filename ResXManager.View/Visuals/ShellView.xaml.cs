namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Windows.Markup;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    [Export]
    [DataTemplate(typeof(ShellViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ShellView
    {
        [ImportingConstructor]
        public ShellView([NotNull] ExportProvider exportProvider)
        {
            Contract.Requires(exportProvider != null);

            try
            {
                this.SetExportProvider(exportProvider);

                InitializeComponent();

                Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
            }
            catch (Exception ex)
            {
                exportProvider.TraceXamlLoaderError(ex);
            }
        }
    }
}
