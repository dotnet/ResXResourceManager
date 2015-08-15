namespace tomenglertde.ResXManager.View
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Interactivity;

    using DataGridExtensions;

    /// <summary>
    /// Assemblies only referenced via reflection (XAML) can cause problems at runtime, sometimes they are not correctly installed
    /// by the VSIX installer. Add some code references to avoid this problem by forcing the assemblies to be loaded before the XAML is loaded.
    /// </summary>
    internal static class References
    {
        private static readonly DependencyProperty _hardReferenceToDgx = DataGridFilterColumn.FilterProperty;

        public static void Resolve(DependencyObject view)
        {
            if (_hardReferenceToDgx == null) // just use this to avoid warnings...
            {
                Trace.WriteLine("HardReferenceToDgx failed");
            }

            Interaction.GetBehaviors(view);
        }
    }
}