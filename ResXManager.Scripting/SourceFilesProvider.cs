namespace ResXManager.Scripting
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Text.RegularExpressions;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;

    [Export]
    [Export(typeof(ISourceFilesProvider))]
    internal class SourceFilesProvider : ISourceFilesProvider, IFileFilter
    {
        private Regex _fileExclusionFilter;

        public string Folder { get; set; }

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