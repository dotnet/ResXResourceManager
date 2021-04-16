namespace ResXManager.View.Tools
{
    using System;
    using System.ComponentModel;
    using System.Composition;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;
    using System.Windows.Threading;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using TomsToolbox.Wpf;

    [Export, Shared]
    public class PerformanceTracer
    {
        private readonly ITracer _tracer;
        private readonly Configuration _configuration;
        private int _index;

        [ImportingConstructor]
        public PerformanceTracer(ITracer tracer, Configuration configuration)
        {
            _tracer = tracer;
            _configuration = configuration;
        }

        public IDisposable? Start([Localizable(false)] string message)
        {
            if (!_configuration.ShowPerformanceTraces)
                return null;

            return new Tracer(_tracer, Interlocked.Increment(ref _index), message);
        }

        public void Start([Localizable(false)] string message, DispatcherPriority priority)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope => will be disposed deferred
            var tracer = Start(message);
#pragma warning restore CA2000 // Dispose objects before losing scope
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
                _tracer = tracer;
                _index = index;
                _message = message;

                _stopwatch.Start();

                _tracer.WriteLine(">>> {0}: {1} @{2}", _index, _message, DateTime.Now.ToString("HH:mm:ss.f", CultureInfo.InvariantCulture));
            }


            public void Dispose()
            {
                _tracer.WriteLine("<<< {0}: {1} {2}", _index, _message, _stopwatch.Elapsed);

                _stopwatch.Stop();
            }
        }
    }
}
