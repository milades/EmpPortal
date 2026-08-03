namespace EmpPortal.Application.Configuration;

public interface IRuntimeSettingsService
{
    public Task<IReadOnlyList<RuntimeSettingItem>> GetAllAsync(
        CancellationToken cancellationToken = default);

    public Task UpdateAsync(
        string key,
        string value,
        Guid actorUserId,
        string actorUpn,
        string correlationId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
