namespace tomenglertde.ResXManager.Model
{
    public interface IExportParameters
    {
        IResourceScope Scope
        {
            get;
        }

        string FileName
        {
            get;
        }
    }
}
