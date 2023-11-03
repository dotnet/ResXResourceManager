namespace ResXManager.View.Controls
{
    using System;
    using System.Windows.Controls;

    internal class AutoSuggestBox : ComboBox
    {
        private const string InternalTextBoxIdentifier = "PART_EditableTextBox";
        private TextBox? _textBox;
        private int _previousCaretIndex;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _textBox = GetTemplateChild(InternalTextBoxIdentifier) as TextBox;
            if (_textBox != null)
            {
                _textBox.TextChanged += TextBox_TextChanged;
            }

            DropDownOpened += AutoSuggestBox_DropDownOpened;
        }

        private void AutoSuggestBox_DropDownOpened(object sender, EventArgs e)
        {
            if (_textBox != null)
            {
                _textBox.CaretIndex = _previousCaretIndex; // prevent automatic text selection on ComboBox open
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                _previousCaretIndex = textBox.CaretIndex;

                if (!IsDropDownOpen)
                {
                    IsDropDownOpen = true;
                }
            }
        }
    }
}
