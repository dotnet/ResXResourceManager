namespace tomenglertde.ResXManager.Model
{
    using System.Diagnostics.Contracts;
    using System.IO;

    using JetBrains.Annotations;

    [ContractClass(typeof(FileFilterContract))]
    public interface IFileFilter
    {
        bool IsSourceFile([NotNull] ProjectFile file);

        bool IncludeFile([NotNull] FileInfo fileInfo);
    }

    [ContractClassFor(typeof(IFileFilter))]
    internal abstract class FileFilterContract : IFileFilter
    {
        public bool IsSourceFile(ProjectFile file)
        {
            Contract.Requires(file != null);

            throw new System.NotImplementedException();
        }

        public bool IncludeFile(FileInfo fileInfo)
        {
            Contract.Requires(fileInfo != null);

            throw new System.NotImplementedException();
        }
    }
}