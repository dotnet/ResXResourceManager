namespace tomenglertde.ResXManager.Translators
{
    using TomsToolbox.Desktop;

    public class CredentialItem : ObservableObject, ICredentialItem
    {
        private readonly string _key;
        private readonly string _description;
        private string _value;

        public CredentialItem(string key, string description)
        {
            _key = key;
            _description = description;
        }

        public string Key => _key;

        public string Description => _description;

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                SetProperty(ref _value, value, () => Value);
            }
        }
    }
}