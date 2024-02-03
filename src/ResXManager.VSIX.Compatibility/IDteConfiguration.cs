namespace ResXManager.VSIX.Compatibility;

using Microsoft.VisualStudio.Shell;

using ResXManager.Model;

public interface IDteConfiguration : IConfiguration
{
    TaskErrorCategory TaskErrorCategory { get; set; }

    bool ShowErrorsInErrorList { get; set; }

    MoveToResourceConfiguration MoveToResources { get; }
}