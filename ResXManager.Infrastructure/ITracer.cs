namespace tomenglertde.ResXManager.Infrastructure
{
    using System.ComponentModel;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using TomsToolbox.Desktop.Composition;

    [ContractClass(typeof(TracerContract))]
    public interface ITracer
    {
        void TraceError([Localizable(false)] string value);

        void TraceWarning([Localizable(false)] string value);

        void WriteLine([Localizable(false)] string value);
    }

    [ContractClassFor(typeof(ITracer))]
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
        public static void TraceError(this ITracer tracer, [Localizable(false)] string format, params object[] args)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            tracer.TraceError(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void TraceWarning(this ITracer tracer, [Localizable(false)] string format, params object[] args)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            tracer.TraceWarning(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void WriteLine(this ITracer tracer, [Localizable(false)] string format, params object[] args)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            tracer.WriteLine(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void TraceError(this ExportProvider exportProvider, [Localizable(false)] string format, params object[] args)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            exportProvider.GetExportedValue<ITracer>().TraceError(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void TraceError(this ICompositionHost exportProvider, [Localizable(false)] string format, params object[] args)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            exportProvider.GetExportedValue<ITracer>().TraceError(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void TraceError(this ExportProvider exportProvider, [Localizable(false)] string message)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(message != null);

            exportProvider.GetExportedValue<ITracer>().TraceError(message);
        }

        public static void TraceError(this ICompositionHost exportProvider, [Localizable(false)] string message)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(message != null);

            exportProvider.GetExportedValue<ITracer>().TraceError(message);
        }
    }
}