namespace tomenglertde.ResXManager
{
    using System.ComponentModel.Composition;
    using System.Diagnostics;

    using tomenglertde.ResXManager.Infrastructure;

    [Export(typeof(ITracer))]
    class Tracer : ITracer
    {
        void ITracer.TraceError(string value)
        {
            Trace.TraceError(value);
        }

        void ITracer.TraceWarning(string value)
        {
            Trace.TraceWarning(value);
        }

        void ITracer.WriteLine(string value)
        {
            Trace.TraceInformation(value);
        }
    }
}
