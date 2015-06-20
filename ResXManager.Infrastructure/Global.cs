namespace tomenglertde.ResXManager.Infrastructure
{
    using System.Windows;

    public static class Global
    {
        public static readonly DependencyProperty TextFontSizeProperty =
            DependencyProperty.RegisterAttached("TextFontSize", typeof(double), typeof(Global), new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.Inherits));
    }
}
