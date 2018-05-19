namespace tomenglertde.ResXManager.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using JetBrains.Annotations;

    using TomsToolbox.Desktop.Composition;

    public interface ITracer
    {
        void TraceError([Localizable(false)][NotNull] string value);

        void TraceWarning([Localizable(false)][NotNull] string value);

        void WriteLine([Localizable(false)][NotNull] string value);
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

        public static void WriteLine([NotNull] this ExportProvider exportProvider, [Localizable(false)] [NotNull] string message)
        {
            Contract.Requires(exportProvider != null);
            Contract.Requires(message != null);

            exportProvider.GetExportedValue<ITracer>().WriteLine(message);
        }

        public static void TraceXamlLoaderError([NotNull] this ExportProvider exportProvider, [NotNull] Exception ex)
        {
            exportProvider.TraceError(ex.Message);

            var path = Path.GetDirectoryName(typeof(ITracer).Assembly.Location);
            Contract.Assume(!string.IsNullOrEmpty(path));

            var assemblyFileNames = Directory.EnumerateFiles(path, @"*.dll")
                .ToArray();

            var assemblyNames = new HashSet<string>(assemblyFileNames.Select(Path.GetFileNameWithoutExtension));

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            var assemblies = loadedAssemblies
                .Where(a => assemblyNames.Contains(a.GetName().Name))
                .ToArray();

            var messages = assemblies
                .Select(assembly => string.Format(CultureInfo.CurrentCulture, "Assembly '{0}' loaded from {1}", assembly.FullName, assembly.CodeBase))
                .OrderBy(text => text, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var message in messages)
            {
                exportProvider.WriteLine(message);
            }

            var assembliesByName = assemblies
                .GroupBy(a => a.FullName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToArray();

            if (assembliesByName.Any())
            {
                exportProvider.WriteLine("Duplicate assemblies found: " + string.Join(", ", assembliesByName));
            }

            exportProvider.WriteLine("Please read https://github.com/tom-englert/ResXResourceManager/wiki/Fixing-errors before creating an issue.");
        }
    }
}