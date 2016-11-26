namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;

    using tomenglertde.ResXManager.Infrastructure;

    [Export(typeof(ITracer))]
    public class OutputWindowTracer : ITracer
    {
        [NotNull]
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public OutputWindowTracer([NotNull][Import(nameof(VSPackage))] IServiceProvider serviceProvider)
        {
            Contract.Requires(serviceProvider != null);
            _serviceProvider = serviceProvider;
        }

        private void LogMessageToOutputWindow(string value)
        {
            var outputWindow = _serviceProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null)
                return;

            var outputPaneGuid = new Guid("{C49C2D45-A34D-4255-9382-40CE2BDAD575}");

            IVsOutputWindowPane pane;
            var errorCode = outputWindow.GetPane(ref outputPaneGuid, out pane);

            if (ErrorHandler.Failed(errorCode) || pane == null)
            {
                outputWindow.CreatePane(ref outputPaneGuid, Resources.ToolWindowTitle, Convert.ToInt32(true), Convert.ToInt32(false));
                outputWindow.GetPane(ref outputPaneGuid, out pane);
            }

            pane?.OutputString(value);
        }

        public void TraceError(string value)
        {
            WriteLine(string.Concat(Resources.Error, @" ", value));
        }

        public void TraceWarning(string value)
        {
            WriteLine(string.Concat(Resources.Warning, @" ", value));
        }

        public void WriteLine(string value)
        {
            LogMessageToOutputWindow(value + Environment.NewLine);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_serviceProvider != null);
        }

    }
}
