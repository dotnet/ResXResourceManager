namespace ResXManager.VSIX.Visuals
{
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;

    using Microsoft.VisualStudio.Shell;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    public class ColorItemViewModel
    {
        public ColorItemViewModel(SolidColorBrush brush, string keyName)
        {
            Brush = brush;
            KeyName = keyName;
        }

        public SolidColorBrush Brush { get; }

        public string KeyName { get; }

        public double Luminance => ToGray(Brush.Color);

        public bool IsDark => Luminance < 128;

        public override string ToString()
        {
            return Brush.Color.ToString(CultureInfo.InvariantCulture);
        }

        private static double ToGray(Color color)
        {
            return color.R * 0.3 + color.G * 0.59 + color.B * 0.11;
        }
    }

    [DataTemplate(typeof(ColorViewModel))]
    public partial class ColorView
    {
        public ColorView()
        {
            InitializeComponent();
            Items = GetColors();
        }

        public ColorItemViewModel[] Items
        {
            get => (ColorItemViewModel[])GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            "Items", typeof(ColorItemViewModel[]), typeof(ColorView), new PropertyMetadata(default(ColorItemViewModel[])));

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property != ForegroundProperty)
                return;

            Items = GetColors();
        }

        private ColorItemViewModel[] GetColors()
        {
            var vsColors = typeof(VsColors).GetProperties()
                .Select(p => ToItemViewModel((p.GetValue(null), p.Name)))
                .ExceptNullItems()
                .OrderBy(item => item.Luminance);

            return _resourceKeys
                .Select(ToItemViewModel)
                .ExceptNullItems()
                .Concat(vsColors)
                .ToArray();
        }

        private static readonly (object Key, string name)[] _resourceKeys =
        {
            (SystemColors.ControlLightLightBrushKey,           nameof(SystemColors.ControlLightLightBrushKey)),
            (SystemColors.ControlLightBrushKey,                nameof(SystemColors.ControlLightBrushKey)),
            (SystemColors.ControlBrushKey,                     nameof(SystemColors.ControlBrushKey)),
            (SystemColors.ControlDarkBrushKey,                 nameof(SystemColors.ControlDarkBrushKey)),
            (SystemColors.ControlDarkDarkBrushKey,             nameof(SystemColors.ControlDarkDarkBrushKey)),

            (SystemColors.ControlTextBrushKey,                 nameof(SystemColors.ControlTextBrushKey)),

            (SystemColors.GrayTextBrushKey,                    nameof(SystemColors.GrayTextBrushKey)),

            (SystemColors.HighlightBrushKey,                   nameof(SystemColors.HighlightBrushKey)),
            (SystemColors.HighlightTextBrushKey,               nameof(SystemColors.HighlightTextBrushKey)),

            (SystemColors.InfoTextBrushKey,                    nameof(SystemColors.InfoTextBrushKey)),
            (SystemColors.InfoBrushKey,                        nameof(SystemColors.InfoBrushKey)),

            (SystemColors.MenuBrushKey,                        nameof(SystemColors.MenuBrushKey)),
            (SystemColors.MenuBarBrushKey,                     nameof(SystemColors.MenuBarBrushKey)),
            (SystemColors.MenuTextBrushKey,                    nameof(SystemColors.MenuTextBrushKey)),

            (SystemColors.WindowBrushKey,                      nameof(SystemColors.WindowBrushKey)),
            (SystemColors.WindowTextBrushKey,                  nameof(SystemColors.WindowTextBrushKey)),

            (SystemColors.ActiveCaptionBrushKey,               nameof(SystemColors.ActiveCaptionBrushKey)),
            (SystemColors.ActiveBorderBrushKey,                nameof(SystemColors.ActiveBorderBrushKey)),
            (SystemColors.ActiveCaptionTextBrushKey,           nameof(SystemColors.ActiveCaptionTextBrushKey)),

            (SystemColors.InactiveCaptionBrushKey,             nameof(SystemColors.InactiveCaptionBrushKey)),
            (SystemColors.InactiveBorderBrushKey,              nameof(SystemColors.InactiveBorderBrushKey)),
            (SystemColors.InactiveCaptionTextBrushKey,         nameof(SystemColors.InactiveCaptionTextBrushKey)),

            (TomsToolbox.Wpf.Styles.ResourceKeys.BorderBrush,   nameof(TomsToolbox.Wpf.Styles.ResourceKeys.BorderBrush)),
            (TomsToolbox.Wpf.Styles.ResourceKeys.DisabledBrush, nameof(TomsToolbox.Wpf.Styles.ResourceKeys.DisabledBrush))
        };

        private ColorItemViewModel? ToItemViewModel((object? resourceKey, string name) item)
        {
            if (item.resourceKey == null)
                return null;

            var resource = FindResource(item.resourceKey);

            if (!(resource is SolidColorBrush brush))
            {
                if (!(resource is Color color))
                    return null;

                brush = new SolidColorBrush(color);
            }

            return new ColorItemViewModel(brush, item.name);
        }

        private void ColorView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(string.Join("\r\n", Items.Select(i =>
                $"<SolidColorBrush x:Key=\"{{x:Static SystemColors.{i.KeyName}}}\" Color=\"#{i.Brush.Color}\" />"
            )));
        }
    }
}
