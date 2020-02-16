namespace ResXManager.Model
{
    using JetBrains.Annotations;

    public interface IExportParameters
    {
        [CanBeNull]
        IResourceScope Scope
        {
            get;
        }

        [CanBeNull]
        string FileName
        {
            get;
        }
    }
}
