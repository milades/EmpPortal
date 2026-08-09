using EmpPortal.Application.Authorization;

namespace EmpPortal.Application.Hr;

public sealed record PersonnelProfileData(Guid UserId, string? InternalPhone, DateTimeOffset UpdatedAtUtc);

public sealed record PersonnelVehicleData(
    Guid Id,
    string PlateNumber,
    string VehicleType,
    string? Trim,
    string? Model,
    string? Color,
    string? Notes,
    DateTimeOffset UpdatedAtUtc);

public sealed record PersonnelFileData(
    PersonnelProfileData Profile,
    IReadOnlyList<PersonnelVehicleData> Vehicles);

public interface IPersonnelFileService
{
    public Task<PersonnelFileData> GetMineAsync(PortalActor actor, CancellationToken cancellationToken = default);

    public Task SaveInternalPhoneAsync(
        PortalActor actor,
        string? internalPhone,
        CancellationToken cancellationToken = default);

    public Task<PersonnelVehicleData> UpsertVehicleAsync(
        PortalActor actor,
        Guid? vehicleId,
        string plateNumber,
        string vehicleType,
        string? trim,
        string? model,
        string? color,
        string? notes,
        CancellationToken cancellationToken = default);

    public Task DeleteVehicleAsync(
        PortalActor actor,
        Guid vehicleId,
        CancellationToken cancellationToken = default);
}
