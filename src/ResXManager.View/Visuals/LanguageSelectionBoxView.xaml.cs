namespace ResXManager.View.Visuals
{
    using System;
    using System.Composition;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Controls;

    using ResXManager.Infrastructure;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    /// <summary>
    /// Interaction logic for LanguageSelectionBoxView.xaml
    /// </summary>
    [DataTemplate(typeof(LanguageSelectionBoxViewModel))]
    public partial class LanguageSelectionBoxView
    {
        private TextBox? _textBox;
        private int _caretIndex;

        [ImportingConstructor]
        public LanguageSelectionBoxView(IExportProvider exportProvider)
        {
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

        private void ComboBox_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _textBox = ComboBox.VisualDescendants()
                .OfType<TextBox>()
                .FirstOrDefault();

            if (_textBox is null)
                return;

            _textBox.TextChanged += TextBox_TextChanged;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _caretIndex = _textBox?.CaretIndex ?? -1;

            switch (e.Changes.FirstOrDefault())
            {
                case null:
                    return;

                // only open dropdown on first change only, not when it manually has been closed
                case { Offset: 0, AddedLength: 1 } when _textBox?.Text.Length == 1:
                    ComboBox.IsDropDownOpen = true;
                    break;
            }
        }

        private void ComboBox_OnDropDownOpened(object sender, EventArgs e)
        {
            // prevent automatic text selection on ComboBox open
            if (_textBox is null)
                return;

            _textBox.CaretIndex = _caretIndex;
        }
    }
}