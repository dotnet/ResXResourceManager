namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    [ContractClass(typeof (SourceFilesProviderContract))]
    public interface ISourceFilesProvider
    {
        [NotNull]
        IList<ProjectFile> SourceFiles { get; }
    }

    [ContractClassFor(typeof (ISourceFilesProvider))]
    internal abstract class SourceFilesProviderContract : ISourceFilesProvider
    {
        public IList<ProjectFile> SourceFiles
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<ProjectFile>>() != null);
                throw new System.NotImplementedException();
            }
        }
    }
}
