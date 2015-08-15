namespace tomenglertde.ResXManager.Infrastructure
{
    using System.Diagnostics.Contracts;
    using System.Globalization;

    public interface ITracer
    {
        void TraceError(string value);
        void TraceWarning(string value);
        void WriteLine(string value);
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