namespace ResXManager.View.Visuals
{
    using System;
    using System.Composition;
    using System.Windows;
    using System.Windows.Input;

    using TomsToolbox.Composition;
    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Input box shows a prompt to enter a string.
    /// </summary>
    [Export(typeof(InputBox))]
    public partial class InputBox
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputBox"/> class.
        /// </summary>
        [ImportingConstructor]
        public InputBox(IExportProvider exportProvider)
        {
            this.SetExportProvider(exportProvider);

            InitializeComponent();
        }

        /// <summary>
        /// A callback to validate the input text.
        /// </summary>
        public event EventHandler<TextEventArgs>? TextChanged;

        /// <summary>
        /// Gets or sets the prompt to be displayed.
        /// </summary>
        public string? Prompt
        {
            get => (string)GetValue(PromptProperty);
            set => SetValue(PromptProperty, value);
        }
        /// <summary>
        /// Identifies the Prompt dependency property
        /// </summary>
        public static readonly DependencyProperty PromptProperty =
            DependencyProperty.Register("Prompt", typeof(string), typeof(InputBox));

        /// <summary>
        /// Gets or sets the text that the user has entered.
        /// </summary>
        public string? Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        /// <summary>
        /// Identifies the Text dependency property
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(InputBox), new FrameworkPropertyMetadata(null, (sender, e) => ((InputBox)sender).Text_Changed((string)e.NewValue)));

        private void Text_Changed(string? newValue)
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
        public static readonly DependencyProperty IsInputValidProperty =
            DependencyProperty.Register("IsInputValid", typeof(bool), typeof(InputBox), new FrameworkPropertyMetadata(false));


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
