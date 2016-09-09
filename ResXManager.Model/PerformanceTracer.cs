namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Threading;
    using System.Windows.Threading;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Desktop;

    [Export]
    public class PerformanceTracer
    {
        private readonly ITracer _tracer;
        private readonly Configuration _configuration;
        private int _index;

        [ImportingConstructor]
        public PerformanceTracer(ITracer tracer, Configuration configuration)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(configuration != null);

            _tracer = tracer;
            _configuration = configuration;
        }

        public IDisposable Start([Localizable(false)] string message)
        {
            Contract.Requires(message != null);

            if (!_configuration.ShowPerformanceTraces)
                return null;

            return new Tracer(_tracer, Interlocked.Increment(ref _index), message);
        }

        public void Start([Localizable(false)] string message, DispatcherPriority priority)
        {
            Contract.Requires(message != null);

            var tracer = Start(message);
            if (tracer == null)
                return;

            Dispatcher.CurrentDispatcher.BeginInvoke(priority, () => tracer.Dispose());
        }

        private sealed class Tracer : IDisposable
        {
            private readonly ITracer _tracer;
            private readonly int _index;
            private readonly string _message;
            private readonly Stopwatch _stopwatch = new Stopwatch();

            public Tracer(ITracer tracer, int index, string message)
            {
                Contract.Requires(tracer != null);
                Contract.Requires(message != null);

                _tracer = tracer;
                _index = index;
                _message = message;

                _stopwatch.Start();

                _tracer.WriteLine(">>> {0}: {1} @{2}", _index, _message, DateTime.Now.ToString("HH:mm:ss.f", CultureInfo.InvariantCulture));
            }


            public void Dispose()
            {
                _tracer.WriteLine("<<< {0}: {1} {2}", _index, _message, _stopwatch.Elapsed);
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_tracer != null);
                Contract.Invariant(_message != null);
                Contract.Invariant(_stopwatch != null);
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
            Contract.Invariant(_configuration != null);
        }
    }
}
