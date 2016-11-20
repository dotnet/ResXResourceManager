namespace tomenglertde.ResXManager.Model
{
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    [ContractClass(typeof (SourceFileFilterContract))]
    public interface ISourceFileFilter
    {
        bool IsSourceFile([NotNull] ProjectFile file);
    }

    [ContractClassFor(typeof (ISourceFileFilter))]
    internal abstract class SourceFileFilterContract : ISourceFileFilter
    {
        public bool IsSourceFile(ProjectFile file)
        {
            Contract.Requires(file != null);

            throw new System.NotImplementedException();
        }
    }
}