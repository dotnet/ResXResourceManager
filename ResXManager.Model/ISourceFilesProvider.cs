namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    [ContractClass(typeof (SourceFilesProviderContract))]
    public interface ISourceFilesProvider
    {
        IEnumerable<ProjectFile> SourceFiles { get; }
    }

    [ContractClassFor(typeof (ISourceFilesProvider))]
    abstract class SourceFilesProviderContract : ISourceFilesProvider
    {
        public IEnumerable<ProjectFile> SourceFiles
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<ProjectFile>>() != null);
                throw new System.NotImplementedException();
            }
        }
    }
}
