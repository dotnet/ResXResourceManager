namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    using TomsToolbox.Wpf;

    /// <summary>
    /// Interaction logic for LanguageSelectionBox.xaml
    /// </summary>
    public partial class LanguageSelectionBox
    {
        public LanguageSelectionBox(IEnumerable<CultureInfo> existingLanguages)
        {
            Languages = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(culture => !CultureInfo.InvariantCulture.Equals(culture))
                .Except(existingLanguages)
                .OrderBy(culture => culture.DisplayName)
                .ToArray();

            InitializeComponent();
        }

        public CultureInfo SelectedLanguage
        {
            get { return (CultureInfo)GetValue(SelectedLanguageProperty); }
            set { SetValue(SelectedLanguageProperty, value); }
        }
        /// <summary>
        /// Identifies the SelectedLanguage dependency property
        /// </summary>
        public static readonly DependencyProperty SelectedLanguageProperty =
            DependencyProperty.Register("SelectedLanguage", typeof (CultureInfo), typeof (LanguageSelectionBox));  
        

        public ICommand CommitCommand
        {
            get
            {
                return new DelegateCommand(CanCommit, Commit);
            }
        }

        public ICollection<CultureInfo> Languages
        {
            get;
            private set;
        }

        private void Commit()
        {
            DialogResult = true;
        }

        private bool CanCommit()
        {
            return SelectedLanguage != null;
        }
    }
}