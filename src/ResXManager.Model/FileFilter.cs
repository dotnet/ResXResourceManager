namespace ResXManager.Model
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using ResXManager.Infrastructure;

    public class FileFilter : IFileFilter
    {
        private readonly string[] _extensions;
        private readonly Regex? _fileExclusionFilter;

        public FileFilter(IConfiguration configuration)
        {
            _extensions = configuration.CodeReferences
                .Items.SelectMany(item => item.ParseExtensions())
                .Concat(new[] { ".t4" })
                .Distinct()
                .ToArray();

            _fileExclusionFilter = configuration.FileExclusionFilter.TryCreateRegex();
        }

        public bool IsSourceFile(ProjectFile file)
        {
            return _extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase);
        }

        public bool IncludeFile(ProjectFile file)
        {
            return _fileExclusionFilter?.IsMatch(file.RelativeFilePath) != true;
        }
    }
}