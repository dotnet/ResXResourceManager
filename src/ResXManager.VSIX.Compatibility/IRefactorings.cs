namespace ResXManager.VSIX.Compatibility
{
    using System.Threading.Tasks;

    using ResXManager.Model;

    public interface IRefactorings
    {
        bool CanMoveToResource();

        Task<ResourceTableEntry?> MoveToResourceAsync();
    }
}