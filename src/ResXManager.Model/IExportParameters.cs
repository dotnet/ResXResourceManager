namespace ResXManager.Model;

public interface IExportParameters
{
    IResourceScope? Scope
    {
        get;
    }

    string? FileName
    {
        get;
    }
}