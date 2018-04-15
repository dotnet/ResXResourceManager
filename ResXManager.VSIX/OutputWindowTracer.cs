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

    public class OutputWindowTracer : ITracer
    {
        [NotNull]
        private readonly IServiceProvider _serviceProvider;

        private static Guid _outputPaneGuid = new Guid("{C49C2D45-A34D-4255-9382-40CE2BDAD575}");

        public OutputWindowTracer([NotNull]IServiceProvider serviceProvider)
        {
            Contract.Requires(serviceProvider != null);
            _serviceProvider = serviceProvider;
        }

        private void LogMessageToOutputWindow([CanBeNull] string value)
        {
            if (!(_serviceProvider.GetService(typeof(SVsOutputWindow)) is IVsOutputWindow outputWindow))
                return;

            var errorCode = outputWindow.GetPane(ref _outputPaneGuid, out var pane);

            if (ErrorHandler.Failed(errorCode) || pane == null)
            {
                outputWindow.CreatePane(ref _outputPaneGuid, Resources.ToolWindowTitle, Convert.ToInt32(true), Convert.ToInt32(false));
                outputWindow.GetPane(ref _outputPaneGuid, out pane);
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
