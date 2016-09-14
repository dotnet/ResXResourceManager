namespace tomenglertde.ResXManager.Model
{
    using System.Diagnostics.Contracts;

    [ContractClass(typeof (SourceFileFilterContract))]
    public interface ISourceFileFilter
    {
        bool IsSourceFile(ProjectFile file);
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