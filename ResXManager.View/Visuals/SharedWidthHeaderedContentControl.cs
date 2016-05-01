namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    using TomsToolbox.Desktop;

    /// <summary>
    /// A headered content control with it's header on the left side.
    /// Host in a panel with the <see cref="Grid.IsSharedSizeScopeProperty"/> set to <c>true</c> to use the same width for all headers.
    /// </summary>
    /// <example>
    /// <code language="XAML">
    /// <![CDATA[
    /// <StackPanel Grid.IsSharedSizeScope="True" >
    ///   <SharedWidthHeaderedContentControl Header="Name:">
    ///     <TextBox Text="{Binding Name}" />
    ///   </SharedWidthHeaderedContentControl>
    ///   <Decorator Height="10"/>
    ///   <SharedWidthHeaderedContentControl Header="Description:">
    ///     <TextBox Text="{Binding Description}" />
    ///   </SharedWidthHeaderedContentControl>
    /// </StackPanel>]]>
    /// </code>
    /// <code>
    /// This will look something like:
    ///
    /// Name:        [............]
    ///
    /// Description: [............]
    /// </code>
    /// </example>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Headered", Justification = "Same as the base class!")]
    public class SharedWidthHeaderedContentControl : HeaderedContentControl
    {
        static SharedWidthHeaderedContentControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SharedWidthHeaderedContentControl), new FrameworkPropertyMetadata(typeof(SharedWidthHeaderedContentControl)));
        }

        /// <summary>
        /// Gets or sets the padding applied to the header.
        /// </summary>
        public Thickness HeaderPadding
        {
            get { return this.GetValue<Thickness>(HeaderPaddingProperty); }
            set { SetValue(HeaderPaddingProperty, value); }
        }
        /// <summary>
        /// Identifies the <see cref="HeaderPadding"/> dependency property
        /// </summary>
        public static readonly DependencyProperty HeaderPaddingProperty =
            DependencyProperty.Register("HeaderPadding", typeof(Thickness), typeof(SharedWidthHeaderedContentControl));
    }
}
