namespace tomenglertde.ResXManager.Infrastructure
{
    public interface ICredentialItem
    {
        string Key
        {
            get;
        }

        string Description
        {
            get;
        }

        string Value
        {
            get;
            set;
        }
    }
}