namespace tomenglertde.ResXManager
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq.Expressions;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.Properties;

    public class Configuration : ConfigurationBase
    {
        private readonly XmlConfiguration _configuration;
        private readonly Settings _settings = Settings.Default;

        public Configuration()
        {
            _configuration = new XmlConfiguration(new StringReader(_settings.Configuration ?? string.Empty));
        }

        protected override T GetValue<T>(Expression<Func<T>> propertyExpression)
        {
            var key = GetKey(propertyExpression);

            try
            {
                return ConvertFromString<T>(_configuration.GetValue(key));
            }
            catch (InvalidCastException)
            {
            }
            return default(T);
        }

        protected override void SetValue<T>(T value, Expression<Func<T>> propertyExpression)
        {
            var key = GetKey(propertyExpression);

            _configuration.SetValue(key, ConvertToString<T>(value));
            _settings.Configuration = _configuration.ToString();
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_settings != null);
        }
    }
}
