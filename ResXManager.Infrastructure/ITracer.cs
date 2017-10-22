namespace tomenglertde.ResXManager.Infrastructure
{
    using System.ComponentModel;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using JetBrains.Annotations;

    using TomsToolbox.Desktop.Composition;

    [ContractClass(typeof(TracerContract))]
    public interface ITracer
    {
        void TraceError([Localizable(false)][NotNull] string value);

        void TraceWarning([Localizable(false)][NotNull] string value);

        void WriteLine([Localizable(false)][NotNull] string value);
    }

    [ContractClassFor(typeof(ITracer))]
    internal abstract class TracerContract : ITracer
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
        public static void TraceError([NotNull] this ITracer tracer, [Localizable(false)][NotNull] string format, [NotNull][ItemNotNull] params object[] args)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            tracer.TraceError(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void TraceWarning([NotNull] this ITracer tracer, [Localizable(false)][NotNull] string format, [NotNull][ItemNotNull] params object[] args)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            tracer.TraceWarning(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void WriteLine([NotNull] this ITracer tracer, [Localizable(false)][NotNull] string format, [NotNull][ItemNotNull] params object[] args)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            tracer.WriteLine(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        [StringFormatMethod("format")]
        public static void TraceError([NotNull] this ExportProvider exportProvider, [Localizable(false)][NotNull] string format, [NotNull][ItemNotNull] params object[] args)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            // ReSharper disable once PossibleNullReferenceException
            exportProvider.GetExportedValue<ITracer>().TraceError(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        [StringFormatMethod("format")]
        public static void TraceError([NotNull] this ICompositionHost exportProvider, [Localizable(false)][NotNull] string format, [NotNull][ItemNotNull] params object[] args)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);

            exportProvider.GetExportedValue<ITracer>().TraceError(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void TraceError([NotNull] this ExportProvider exportProvider, [Localizable(false)][NotNull] string message)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(message != null);

            // ReSharper disable once PossibleNullReferenceException
            exportProvider.GetExportedValue<ITracer>().TraceError(message);
        }

        public static void TraceError([NotNull] this ICompositionHost exportProvider, [Localizable(false)][NotNull] string message)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(message != null);

            exportProvider.GetExportedValue<ITracer>().TraceError(message);
        }
    }
}