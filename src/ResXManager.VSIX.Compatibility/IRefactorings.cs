namespace ResXManager.VSIX
{
    using System.Threading.Tasks;

    using ResXManager.Model;

    public interface IRefactorings
    {
        bool CanMoveToResource();

        Task<ResourceTableEntry?> MoveToResourceAsync();
    }
}