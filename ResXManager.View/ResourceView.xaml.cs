namespace tomenglertde.ResXManager.View
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Markup;
    using DataGridExtensions;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    /// <summary>
    /// Interaction logic for ResourceView.xaml
    /// </summary>
    [ContractVerification(false)]   // Too many warnings from generated code.
    public partial class ResourceView
    {
        private static readonly DependencyProperty HardReferenceToDgx = DataGridFilterColumn.FilterProperty;

        private DataGridTextColumn _neutralLanguageColumn;
        private DataGridTextColumn _neutralCommentColumn;

        public ResourceView()
        {
            Instance = this;

            if (HardReferenceToDgx == null) // just use this...
            {
                Trace.WriteLine("HardReferenceToDgx failed");
            }

            InitializeComponent();
        }

        public event EventHandler<ResourceBeginEditingEventArgs> BeginEditing
        {
            add
            {
                ViewModel.BeginEditing += value;
            }
            remove
            {
                ViewModel.BeginEditing -= value;
            }
        }

        public event RoutedEventHandler NavigateClick
        {
            add
            {
                LikeButton.Click += value;
                DonateButton.Click += value;
                HelpButton.Click += value;
            }
            remove
            {
                LikeButton.Click -= value;
                DonateButton.Click -= value;
                HelpButton.Click -= value;
            }
        }

        public event EventHandler ReloadRequested;

        public event EventHandler<LanguageEventArgs> LanguageSaved;

        private static readonly DependencyProperty EntityFilterProperty =
            DependencyProperty.Register("EntityFilter", typeof(string), typeof(ResourceView), new FrameworkPropertyMetadata(null, (sender, e) => Settings.Default.ResourceFilter = (string)e.NewValue));

        private ResourceManager ViewModel
        {
            get
            {
                return (ResourceManager)DataContext;
            }
        }

        private IEnumerable<CultureInfo> Languages
        {
            get
            {
                return ViewModel.Languages;
            }
        }

        internal static ResourceView Instance
        {
            get;
            private set;
        }

        private void self_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var oldValue = e.OldValue as ResourceManager;
            if (oldValue != null)
            {
                oldValue.LanguageChanging -= ResourceManager_LanguageChanging;
                oldValue.LanguageChanged -= ResourceManager_LanguageChanged;
                BindingOperations.ClearBinding(this, EntityFilterProperty);
            }

            var newValue = e.NewValue as ResourceManager;
            if (newValue != null)
            {
                newValue.LanguageChanging += ResourceManager_LanguageChanging;
                newValue.LanguageChanged += ResourceManager_LanguageChanged;
                newValue.EntityFilter = Settings.Default.ResourceFilter;
                BindingOperations.SetBinding(this, EntityFilterProperty, new Binding("EntityFilter") { Source = newValue });
            }
        }

        private void NeutralLanguage_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.NeutralResourceLanguage = (CultureInfo)(((MenuItem)sender).DataContext);

            if (_neutralLanguageColumn != null)
            {
                _neutralLanguageColumn.Header = new LanguageHeader(null);
            }

            if (_neutralCommentColumn != null)
            {
                _neutralCommentColumn.Header = new CommentHeader(null);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (ReloadRequested != null)
            {
                ReloadRequested(this, EventArgs.Empty);
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetupColumns();
        }

        private void ResourceManager_LanguageChanging(object sender, LanguageChangingEventArgs e)
        {
            SetupColumns();
        }

        private void ResourceManager_LanguageChanged(object sender, LanguageChangedEventArgs e)
        {
            // Defer save to avoid repeated file access
            Dispatcher.BeginInvoke(new Action(
                delegate
                {
                    try
                    {
                        if (!e.Language.HasChanges)
                            return;

                        e.Language.Save();

                        if (LanguageSaved != null)
                        {
                            LanguageSaved(this, e);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, Properties.Resources.Title);
                    }
                }));
        }

        private void SetupColumns()
        {
            var columns = DataGrid.Columns;

            if (columns.Count == 0)
            {
                var keyColumn = new DataGridTextColumn
                {
                    Header = new ColumnHeader(Properties.Resources.Key, ColumnType.Key),
                    Binding = new Binding(@"Key") { ValidatesOnExceptions = true },
                    Width = 200,
                    CanUserReorder = false,
                };

                columns.Add(keyColumn);

                columns.Add(CreateCodeReferencesColumn());
            }

            var languages = Languages.ToArray();
            var languageColumns = columns.Skip(2).ToArray();

            var disconnectedColumns = languageColumns.Where(col => languages.All(language => !Equals(((ILanguageColumnHeader)col.Header).Language, language)));

            foreach (var column in disconnectedColumns)
            {
                columns.Remove(column);
            }

            var addedColumns = languages.Where(language => languageColumns.All(col => !Equals(((ILanguageColumnHeader)col.Header).Language, language)));

            foreach (var language in addedColumns)
            {
                AddLanguageColumn(columns, language);
            }
        }

        private DataGridTextColumn CreateCodeReferencesColumn()
        {
            var elementStyle = new Style();
            elementStyle.Setters.Add(new Setter(ToolTipProperty, new CodeReferencesToolTip()));
            elementStyle.Setters.Add(new Setter(ToolTipService.ShowDurationProperty, int.MaxValue));
            elementStyle.Setters.Add(new Setter(HorizontalAlignmentProperty, HorizontalAlignment.Center));

            var columnHeader = new ColumnHeader(FindResource("CodeReferencesImage"), ColumnType.Other)
            {
                ToolTip = Properties.Resources.CodeReferencesToolTip,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            var column = new DataGridTextColumn
            {
                Header = columnHeader,
                ElementStyle = elementStyle,
                Binding = new Binding(@"CodeReferences.Count"),
                CanUserReorder = false,
                CanUserResize = false,
                IsReadOnly = true,
            };

            column.SetIsFilterVisible(false);
            BindingOperations.SetBinding(column, DataGridColumn.VisibilityProperty, new Binding(@"IsFindCodeReferencesEnabled") { Source = Settings.Default, Converter = new BooleanToVisibilityConverter() });

            return column;
        }

        private void AddLanguageColumn(ICollection<DataGridColumn> columns, CultureInfo language)
        {
            var key = language != null ? @"." + language : string.Empty;

            var isFirstCommentColumn = !columns.Any(col => col.Header is CommentHeader);

            var commentColumn = new DataGridTextColumn
            {
                Header = new CommentHeader(language),
                Binding = new Binding(@"Comments[" + key + @"]"),
                Width = 300,
                Visibility = isFirstCommentColumn ? Visibility.Visible : Visibility.Hidden
            };

            commentColumn.EnableMultilineEditing();
            commentColumn.EnableSpellChecker(language);

            columns.Add(commentColumn);

            if (language == null)
            {
                _neutralCommentColumn = commentColumn;
            }

            var column = new DataGridTextColumn
            {
                Header = new LanguageHeader(language),
                Binding = new Binding(@"Values[" + key + @"]"),
                Width = 300,
            };

            column.EnableMultilineEditing();
            column.EnableSpellChecker(language);

            columns.Add(column);

            if (language == null)
            {
                _neutralLanguageColumn = column;
            }
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter on a cell should start editing the cell without clearing the content.

            var dependencyObject = e.OriginalSource as DependencyObject;

            if (dependencyObject.IsChildOfEditingElement())
                return;

            var key = e.Key;
            if ((key != Key.Return) || (!Key.LeftCtrl.IsKeyDown() && !Key.RightCtrl.IsKeyDown()))
                return;

            var grid = (DataGrid)sender;

            grid.BeginEdit();
            e.Handled = true;
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            var entry = (ResourceTableEntry)e.Row.Item;
            var resourceEntity = entry.Owner;

            var language = resourceEntity.Languages.First().Culture;

            var languageHeader = e.Column.Header as ILanguageColumnHeader;
            if (languageHeader != null)
            {
                language = languageHeader.Language;
            }

            if (!resourceEntity.CanEdit(language))
            {
                e.Cancel = true;
            }

            if (!e.Cancel)
            {
                ToolBar.IsEnabled = false;
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            ToolBar.IsEnabled = true;
        }

        private void ColumnChooserPopup_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue.Equals(false))
            {
                ColumnChooserToggleButton.IsChecked = false;
            }
        }

        private void ColumnChooserPopup_Opened(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => ColumnChooserListBox.Focus()));
        }

        private void ColumnChooserPopup_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                case Key.Enter:
                case Key.Tab:
                    ColumnChooserToggleButton.Focus();
                    break;
            }
        }

        private void AddLanguage_Click(object sender, RoutedEventArgs e)
        {
            var inputBox = new InputBox
            {
                Title = Properties.Resources.Title,
                Prompt = Properties.Resources.NewLanguageIdPrompt,
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var languages = Languages.Where(l => l != null).Select(l => l.ToString()).ToArray();

            inputBox.TextChanged += (_, args) =>
                inputBox.IsInputValid = !languages.Contains(args.Text, StringComparer.OrdinalIgnoreCase) && ResourceManager.IsValidLanguageName(args.Text);

            if (inputBox.ShowDialog() == true)
            {
                AddLanguageColumn(DataGrid.Columns, new CultureInfo(inputBox.Text));
            }
        }

        private void ListBoxGroupHeader_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var visual = (FrameworkElement)sender;
            var group = (CollectionViewGroup)visual.DataContext;

            ListBox.BeginInit();

            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            {
                ListBox.SelectedItems.Clear();
            }

            foreach (var item in group.Items)
            {
                ListBox.SelectedItems.Add(item);
            }

            ListBox.EndInit();
        }

        private void ErrorsOnlyFilter_Changed(object sender, EventArgs e)
        {
            var button = (ToggleButton)sender;

            if (button.IsChecked.GetValueOrDefault())
            {
                var languageCount = Languages.Count();
                DataGrid.SetIsAutoFilterEnabled(false);
                DataGrid.Items.Filter = row =>
                {
                    var entry = (ResourceTableEntry)row;
                    return !entry.IsInvariant && ((entry.Values.Keys.Count() < languageCount) || entry.Values.Values.Any(string.IsNullOrEmpty));
                };
            }
            else
            {
                DataGrid.Items.Filter = null;
                DataGrid.SetIsAutoFilterEnabled(true);
            }
        }

        private void DataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var currentCell = DataGrid.CurrentCell;
            var column = currentCell.Column as DataGridBoundColumn;
            if (column == null)
                return;

            var header = column.Header as ILanguageColumnHeader;
            if (header != null)
            {
                TextBox.IsEnabled = true;
                TextBox.DataContext = currentCell.Item;

                var ieftLanguageTag = (header.Language ?? Settings.Default.NeutralResourceLanguage).IetfLanguageTag;
                TextBox.Language = XmlLanguage.GetLanguage(ieftLanguageTag);

                BindingOperations.SetBinding(TextBox, TextBox.TextProperty, column.Binding);
            }
            else
            {
                TextBox.IsEnabled = false;
                TextBox.DataContext = null;
                BindingOperations.ClearBinding(TextBox, TextBox.TextProperty);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used via XAML!")]
        private void DeleteCommandConverter_OnExecuting(object sender, CancelEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.ConfirmDeleteItems, Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                e.Cancel = true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used via XAML!")]
        private void CutCommandConverter_OnExecuting(object sender, CancelEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.ConfirmCutItems, Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                e.Cancel = true;
        }
    }
}
