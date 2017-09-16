namespace ResXManager.Scripting
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    [Export]
    [Export(typeof(ISourceFilesProvider))]
    internal class SourceFilesProvider : ISourceFilesProvider, IFileFilter
    {
        [CanBeNull]
        private Regex _fileExclusionFilter;
        [CanBeNull]
        public string Folder { get; set; }
        [CanBeNull]
        public string ExclusionFilter { get; set; }

        public IList<ProjectFile> SourceFiles
        {
            get
            {
                var folder = Folder;
                if (string.IsNullOrEmpty(folder))
                    return new ProjectFile[0];

                _fileExclusionFilter = ExclusionFilter.TryCreateRegex();

                return new DirectoryInfo(folder).GetAllSourceFiles(this);
            }
        }

        public void Invalidate()
        {
        }

        public bool IsSourceFile(ProjectFile file)
        {
            return false;
        }

        public bool IncludeFile(FileInfo fileInfo)
        {
            return _fileExclusionFilter?.IsMatch(fileInfo.FullName) != true;
        }
    }
}