namespace tomenglertde.ResXManager.View.Visuals
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Interactivity;

    using DataGridExtensions;

    using tomenglertde.ResXManager.Translators;

    /// <summary>
    /// Assemblies only referenced via reflection (XAML) can cause problems at runtime, sometimes they are not correctly installed
    /// by the VSIX installer. Add some code references to avoid this problem by forcing the assemblies to be loaded before the XAML is loaded.
    /// </summary>
    internal static class References
    {
        private static readonly DependencyProperty _hardReferenceToDgx = DataGridFilterColumn.FilterProperty;
        private static readonly ITranslator[] _hardRefToTranslators = TranslatorHost.Translators;

        public static void Resolve(DependencyObject view)
        {
            if (_hardReferenceToDgx == null) // just use this to avoid warnings...
            {
                Trace.WriteLine("HardReferenceToDgx failed");
            }

            if (_hardRefToTranslators == null) // just use this to avoid warnings...
            {
                Trace.WriteLine("HardReferenceToTranlators failed");
            }

            Interaction.GetBehaviors(view);
        }
    }
}