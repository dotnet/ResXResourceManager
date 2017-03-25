namespace ResXManager.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;
    using System.IO;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Model;

    [Export]
    [Export(typeof(ISourceFilesProvider))]
    internal class SourceFilesProvider : ISourceFilesProvider, ISourceFileFilter
    {
        [NotNull]
        private readonly Configuration _configuration;

        [ImportingConstructor]
        public SourceFilesProvider([NotNull] Configuration configuration)
        {
            Contract.Requires(configuration != null);

            _configuration = configuration;
        }

        public string Folder { get; set; }

        public IList<ProjectFile> SourceFiles
        {
            get
            {
                var folder = Folder;
                if (String.IsNullOrEmpty(folder))
                    return new ProjectFile[0];

                return new DirectoryInfo(folder).GetAllSourceFiles(_configuration, this);
            }
        }

        public bool IsSourceFile(ProjectFile file)
        {
            return false;
        }
    }
}