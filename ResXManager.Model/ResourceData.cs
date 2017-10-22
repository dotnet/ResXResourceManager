namespace tomenglertde.ResXManager.Model
{
    using JetBrains.Annotations;

    public class ResourceData
    {
        [CanBeNull]
        public string Text
        {
            get;
            set;
        }

        [CanBeNull]
        public string Comment
        {
            get;
            set;
        }
    }
}
