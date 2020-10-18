namespace ResXManager.View
{
    using System.Windows.Media;

    public static class ExtensionMethods
    {
        public static double ToGray(this Color? color)
        {
            return color?.R * 0.3 + color?.G * 0.6 + color?.B * 0.1 ?? 0.0;
        }
    }
}
