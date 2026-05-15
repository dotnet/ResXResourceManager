namespace ResXManager.View.Visuals;

using System.Text.RegularExpressions;

public class ProjectFilterItem(string displayName)
{
    public string DisplayName { get; } = displayName;

    public string SearchText { get; } = $"^{Regex.Escape(displayName)}$";

    public override string ToString()
    {
        return SearchText;
    }
}
