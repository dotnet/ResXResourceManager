namespace tomenglertde.ResXManager.Infrastructure
{
    using JetBrains.Annotations;

    public static class JsonConvert
    {
        [CanBeNull]
        public static string SerializeObject(object value)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(value);
        }

        [CanBeNull]
        public static T DeserializeObject<T>(string value)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);
        }

        public static void PopulateObject(string value, object target)
        {
            Newtonsoft.Json.JsonConvert.PopulateObject(value, target);
        }
    }
}
