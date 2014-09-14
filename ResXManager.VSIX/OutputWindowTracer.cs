namespace tomenglertde.ResXManager.VSIX
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using tomenglertde.ResXManager.Model;

    internal class OutputWindowTracer : ITracer
    {
        private const string NewLine = "\r\n";
        private readonly IServiceProvider _serviceProvider;

        public OutputWindowTracer(IServiceProvider serviceProvider)
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

            if (pane != null)
            {
                pane.OutputString(value);
            }
        }

        public void TraceError(string value)
        {
            WriteLine(Resources.Error + value);
        }

        public void WriteLine(string value)
        {
            LogMessageToOutputWindow(value + NewLine);
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_serviceProvider != null);
        }

    }
}
