namespace ResXManager.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using TomsToolbox.Essentials;

    internal sealed class SourceFilesProvider : IFileFilter
    {
        private readonly Regex? _fileExclusionFilter;
        private readonly string? _folder;

        public SourceFilesProvider(string? folder, string? exclusionFilter)
        {
            _folder = folder;
            _fileExclusionFilter = exclusionFilter.TryCreateRegex();
        }

        public IList<ProjectFile> EnumerateSourceFiles()
        {
            if (_folder.IsNullOrEmpty())
                return Array.Empty<ProjectFile>();


            return new DirectoryInfo(_folder).GetAllSourceFiles(this, null);
        }

        public bool IsSourceFile(ProjectFile file)
        {
            return false;
        }

        public bool IncludeFile(ProjectFile file)
        {
            return _fileExclusionFilter?.IsMatch(file.RelativeFilePath) != true;
        }
    }
}