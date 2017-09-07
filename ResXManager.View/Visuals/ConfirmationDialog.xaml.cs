namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;

    using JetBrains.Annotations;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for MoveToResourceDialog.xaml
    /// </summary>
    public partial class ConfirmationDialog
    {
        private ConfirmationDialog()
        {
            InitializeComponent();
        }

        public static bool? Show([NotNull] ExportProvider exportProvider, object content, string title)
        {
            Contract.Requires(exportProvider != null);

            var window = new Window
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current?.MainWindow,
                Title = title,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
                SizeToContent = SizeToContent.WidthAndHeight,
                Icon = new BitmapImage(new Uri("pack://application:,,,/ResXManager.View;component/16x16.png"))
            };

            window.SetExportProvider(exportProvider);
            window.Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(exportProvider));
            window.SetResourceReference(StyleProperty, TomsToolbox.Wpf.Styles.ResourceKeys.WindowStyle);
            window.Content = new ConfirmationDialog { Content = content };

            return window.ShowDialog();
        }

        [NotNull]
        public ICommand CommitCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);
                return new DelegateCommand(CanCommit, Commit);
            }
        }

        private void Commit()
        {
            var window = Window.GetWindow(this);
            if (window == null)
                return;

            window.DialogResult = true;
        }

        private bool CanCommit()
        {
            return !this.VisualDescendants().Any(Validation.GetHasError);
        }
    }
}
