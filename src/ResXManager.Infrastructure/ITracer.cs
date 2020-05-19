namespace ResXManager.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using JetBrains.Annotations;

    using TomsToolbox.Composition;
    using TomsToolbox.Essentials;

    public interface ITracer
    {
        void TraceError([Localizable(false)][NotNull] string value);

        void TraceWarning([Localizable(false)][NotNull] string value);

        void WriteLine([Localizable(false)][NotNull] string value);
    }

    public static class TracerExtensions
    {
        [StringFormatMethod("format")]
        public static void TraceError([NotNull] this ITracer tracer, [Localizable(false)][NotNull] string format, [NotNull][ItemNotNull] params object[] args)
        {
            tracer.TraceError(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        [StringFormatMethod("format")]
        public static void TraceWarning([NotNull] this ITracer tracer, [Localizable(false)][NotNull] string format, [NotNull][ItemNotNull] params object[] args)
        {
            tracer.TraceWarning(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        [StringFormatMethod("format")]
        public static void WriteLine([NotNull] this ITracer tracer, [Localizable(false)][NotNull] string format, [NotNull][ItemNotNull] params object[] args)
        {
            tracer.WriteLine(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        [StringFormatMethod("format")]
        public static void TraceError([NotNull] this IExportProvider exportProvider, [Localizable(false)][NotNull] string format, [NotNull][ItemNotNull] params object[] args)
        {
            exportProvider.GetExportedValue<ITracer>().TraceError(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void TraceError([NotNull] this IExportProvider exportProvider, [Localizable(false)][NotNull] string message)
        {
            exportProvider.GetExportedValue<ITracer>().TraceError(message);
        }

        public static void WriteLine([NotNull] this IExportProvider exportProvider, [Localizable(false)] [NotNull] string message)
        {
            exportProvider.GetExportedValue<ITracer>().WriteLine(message);
        }

        public static void TraceXamlLoaderError([NotNull] this IExportProvider exportProvider, [CanBeNull] Exception ex)
        {
            var exceptions = ex?.ExceptionChain().Select(e => e.Message);

            if (exceptions != null)
                exportProvider.TraceError(string.Join("\n ---> ", exceptions));

            var path = Path.GetDirectoryName(typeof(ITracer).Assembly.Location);

            // ReSharper disable once AssignNullToNotNullAttribute
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

            exportProvider.WriteLine("Please read https://github.com/dotnet/ResXResourceManager/Documentation/Topics/Troubleshooting.md before creating an issue.");
        }
    }
}