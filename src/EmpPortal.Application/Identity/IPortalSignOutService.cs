namespace EmpPortal.Application.Identity;

public interface IPortalSignOutService
{
    public Task SignOutAsync(
        Guid? sessionId,
        CancellationToken cancellationToken = default);
}
