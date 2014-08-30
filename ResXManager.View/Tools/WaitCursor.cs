namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Threading;

    /// <summary>
    /// Helper class to ease automatic display of the wait cursor.
    /// </summary>
    public static class WaitCursor
    {
        /// <summary>
        /// Sets the cursor property of the framework element to the "Wait" cursor and 
        /// automatically resets the cursor to the default cursor when the dispatcher becomes idle again.
        /// </summary>
        /// <param name="frameworkElement">The element on which to set the cursor.</param>
        public static void StartLocal(FrameworkElement frameworkElement)
        {
            Contract.Requires(frameworkElement != null);

            frameworkElement.Cursor = Cursors.Wait;
            frameworkElement.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frameworkElement.Cursor = null));
        }

        /// <summary>
        /// Sets the cursor property of the framework elements root visual to the "Wait" cursor and 
        /// automatically resets the cursor to the default cursor when the dispatcher becomes idle again.
        /// </summary>
        /// <param name="frameworkElement">An element in the visual tree to start looking for the root visual.</param>
        /// <remarks>
        /// The root visual usually is the whole window, except for controls embedded in native or WindowsForms windows.
        /// </remarks>
        public static void Start(FrameworkElement frameworkElement)
        {
            Contract.Requires(frameworkElement != null);

            StartLocal(GetRootVisual(frameworkElement) ?? frameworkElement);
        }

        private static FrameworkElement GetRootVisual(FrameworkElement item)
        {
            Contract.Requires(item != null);

            var hwndSource = (HwndSource)PresentationSource.FromDependencyObject(item);
            if (hwndSource == null)
                return null;

            var compositionTarget = hwndSource.CompositionTarget;
            if (compositionTarget == null)
                return null;

            var rootVisual = (FrameworkElement)compositionTarget.RootVisual;
            if (rootVisual == null)
                return null;

            return rootVisual;
        }
    }
}