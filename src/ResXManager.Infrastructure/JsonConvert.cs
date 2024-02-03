namespace ResXManager.Infrastructure;

public static class JsonConvert
{
    public static string? SerializeObject(object value)
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(value);
    }

    public static T? DeserializeObject<T>(string value)
        where T : class
    {
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);
    }

    public static void PopulateObject(string value, object target)
    {
        Newtonsoft.Json.JsonConvert.PopulateObject(value, target);
    }
}