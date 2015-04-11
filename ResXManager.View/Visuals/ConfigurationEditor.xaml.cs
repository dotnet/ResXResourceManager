namespace tomenglertde.ResXManager.View.Visuals
{
    using System.IO;
    using System.Windows;

    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class ConfigurationEditor
    {
        public ConfigurationEditor()
        {
            InitializeComponent();
        }

        private void CommandConverter_Error(object sender, ErrorEventArgs e)
        {
            var ex = e.GetException();
            if (ex == null)
                return;

            MessageBox.Show(ex.Message, Properties.Resources.Title);
        }
    }
}
