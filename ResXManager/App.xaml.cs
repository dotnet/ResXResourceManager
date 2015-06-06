namespace tomenglertde.ResXManager
{
    using System.Globalization;
    using System.Threading;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public App()
        {
#if DEBUG
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
#endif
        }
    }
}
