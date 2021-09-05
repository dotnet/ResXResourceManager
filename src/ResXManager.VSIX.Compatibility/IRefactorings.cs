namespace ResXManager.VSIX
{
    using System.Threading.Tasks;

    using ResXManager.Model;

    public interface IRefactorings
    {
        bool CanMoveToResource(string filePath);

        Task<ResourceTableEntry?> MoveToResourceAsync(string filePath);
    }
}