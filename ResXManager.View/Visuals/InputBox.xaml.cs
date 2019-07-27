namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel.Composition;
    using System.Windows;
    using System.Windows.Input;

    using JetBrains.Annotations;

    using TomsToolbox.Composition;
    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Input box shows a prompt to enter a string.
    /// </summary>
    [Export(typeof(InputBox))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class InputBox
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputBox"/> class.
        /// </summary>
        [ImportingConstructor]
        public InputBox([NotNull] IExportProvider exportProvider)
        {
            this.SetExportProvider(exportProvider);

            InitializeComponent();
        }

        /// <summary>
        /// A callback to validate the input text.
        /// </summary>
        public event EventHandler<TextEventArgs> TextChanged;

        /// <summary>
        /// Gets or sets the prompt to be displayed.
        /// </summary>
        [CanBeNull]
        public string Prompt
        {
            get => (string)GetValue(PromptProperty);
            set => SetValue(PromptProperty, value);
        }
        /// <summary>
        /// Identifies the Prompt dependency property
        /// </summary>
        [NotNull]
        public static readonly DependencyProperty PromptProperty =
            DependencyProperty.Register("Prompt", typeof(string), typeof(InputBox));

        /// <summary>
        /// Gets or sets the text that the user has entered.
        /// </summary>
        [CanBeNull]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        /// <summary>
        /// Identifies the Text dependency property
        /// </summary>
        [NotNull]
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(InputBox), new FrameworkPropertyMetadata(null, (sender, e) => ((InputBox)sender).Text_Changed((string)e.NewValue)));

        private void Text_Changed([CanBeNull] string newValue)
        {
            TextChanged?.Invoke(this, new TextEventArgs(newValue ?? string.Empty));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the text is valid input.
        /// </summary>
        public bool IsInputValid
        {
            get => this.GetValue<bool>(IsInputValidProperty);
            set => SetValue(IsInputValidProperty, value);
        }

        /// <summary>
        /// Identifies the IsInputValid dependency property
        /// </summary>
        [NotNull]
        public static readonly DependencyProperty IsInputValidProperty =
            DependencyProperty.Register("IsInputValid", typeof(bool), typeof(InputBox), new FrameworkPropertyMetadata(false));


        [NotNull]
        public ICommand CommitCommand => new DelegateCommand(CanCommit, Commit);

        private void Commit()
        {
            DialogResult = true;
        }

        private bool CanCommit()
        {
            return IsInputValid;
        }
    }
}
