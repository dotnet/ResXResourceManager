namespace tomenglertde.ResXManager.Infrastructure
{
    using System.Diagnostics.Contracts;
    using System.Globalization;

    [ContractClass(typeof (TracerContract))]
    public interface ITracer
    {
        void TraceError(string value);
        void TraceWarning(string value);
        void WriteLine(string value);
    }

    [ContractClassFor(typeof (ITracer))]
    abstract class TracerContract : ITracer
    {
        void ITracer.TraceError(string value)
        {
            Contract.Requires(value != null);
            throw new System.NotImplementedException();
        }

        void ITracer.TraceWarning(string value)
        {
            Contract.Requires(value != null);
            throw new System.NotImplementedException();
        }

        void ITracer.WriteLine(string value)
        {
            Contract.Requires(value != null);
            throw new System.NotImplementedException();
        }
    }

    public static class TracerExtensions
    {
        public static void TraceError(this ITracer tracer, string format, params object[] args)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            tracer.TraceError(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void TraceWarning(this ITracer tracer, string format, params object[] args)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            tracer.TraceWarning(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void WriteLine(this ITracer tracer, string format, params object[] args)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            tracer.WriteLine(string.Format(CultureInfo.CurrentCulture, format, args));
        }
    }
}