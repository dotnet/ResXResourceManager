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
        void TraceError([Localizable(false)] string value);

        void TraceWarning([Localizable(false)] string value);

        void WriteLine([Localizable(false)] string value);
    }

    public static class TracerExtensions
    {
        [StringFormatMethod("format")]
        public static void TraceError(this ITracer tracer, [Localizable(false)] string format, params object[] args)
        {
            tracer.TraceError(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        [StringFormatMethod("format")]
        public static void TraceWarning(this ITracer tracer, [Localizable(false)] string format, params object[] args)
        {
            tracer.TraceWarning(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        [StringFormatMethod("format")]
        public static void WriteLine(this ITracer tracer, [Localizable(false)] string format, params object[] args)
        {
            tracer.WriteLine(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        [StringFormatMethod("format")]
        public static void TraceError(this IExportProvider exportProvider, [Localizable(false)] string format, params object[] args)
        {
            exportProvider.GetExportedValue<ITracer>().TraceError(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void TraceError(this IExportProvider exportProvider, [Localizable(false)] string message)
        {
            exportProvider.GetExportedValue<ITracer>().TraceError(message);
        }

        public static void WriteLine(this IExportProvider exportProvider, [Localizable(false)] string message)
        {
            exportProvider.GetExportedValue<ITracer>().WriteLine(message);
        }

        public static void TraceXamlLoaderError(this IExportProvider exportProvider, Exception? ex)
        {
            var exceptions = ex?.ExceptionChain().Select(e => e.Message);

            if (exceptions != null)
                exportProvider.TraceError(string.Join("\n ---> ", exceptions));

            var path = Path.GetDirectoryName(typeof(ITracer).Assembly.Location);

            var assemblyFileNames = Directory.EnumerateFiles(path, @"*.dll");

            var assemblyNames = new HashSet<string>(assemblyFileNames.Select(Path.GetFileNameWithoutExtension));

            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            var assemblies = loadedAssemblies
                .Where(a => assemblyNames.Contains(a.GetName().Name))
                .ToList();

            var messages = assemblies
                .Select(assembly => string.Format(CultureInfo.CurrentCulture, "Assembly '{0}' loaded from {1}", assembly.FullName, assembly.CodeBase))
                .OrderBy(text => text, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var message in messages)
            {
                exportProvider.WriteLine(message);
            }

            var assembliesByName = assemblies
                .GroupBy(a => a.FullName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (assembliesByName.Any())
            {
                exportProvider.WriteLine("Duplicate assemblies found: " + string.Join(", ", assembliesByName));
            }

            exportProvider.WriteLine("Please read https://github.com/dotnet/ResXResourceManager/blob/master/Documentation/Topics/Troubleshooting.md before creating an issue.");
        }
    }
}