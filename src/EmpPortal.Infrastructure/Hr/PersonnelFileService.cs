using System.Text.Json;
using EmpPortal.Application.Authorization;
using EmpPortal.Application.Hr;
using EmpPortal.Domain.Auditing;
using EmpPortal.Domain.Hr;
using EmpPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Infrastructure.Hr;

public sealed class PersonnelFileService(
    IDbContextFactory<PortalDbContext> dbContextFactory,
    IPortalAccessEvaluator accessEvaluator) : IPersonnelFileService
{
    public async Task<PersonnelFileData> GetMineAsync(PortalActor actor, CancellationToken cancellationToken = default)
    {
        await EnsureCanViewAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        PersonnelProfile profile = await GetOrCreateProfileAsync(dbContext, actor, cancellationToken);
        PersonnelVehicle[] vehicles = await dbContext.PersonnelVehicles.AsNoTracking()
            .Where(vehicle => vehicle.UserId == actor.UserId)
            .OrderByDescending(vehicle => vehicle.UpdatedAtUtc)
            .ToArrayAsync(cancellationToken);

        return new PersonnelFileData(
            new PersonnelProfileData(profile.UserId, profile.InternalPhone, profile.UpdatedAtUtc),
            vehicles.Select(MapVehicle).ToArray());
    }

    public async Task SaveInternalPhoneAsync(
        PortalActor actor,
        string? internalPhone,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanViewAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        PersonnelProfile profile = await GetOrCreateProfileAsync(dbContext, actor, cancellationToken);
        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        profile.SetInternalPhone(internalPhone, actor.UserId, nowUtc);
        dbContext.AuditEvents.Add(AuditEvent.Create(
            nowUtc,
            "PersonnelInternalPhoneSaved",
            "Succeeded",
            actor.UserId,
            actor.Upn,
            actor.UserId.ToString("D"),
            actor.CorrelationId,
            actor.IpAddress,
            JsonSerializer.Serialize(new { profile.InternalPhone })));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PersonnelVehicleData> UpsertVehicleAsync(
        PortalActor actor,
        Guid? vehicleId,
        string plateNumber,
        string vehicleType,
        string? trim,
        string? model,
        string? color,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanViewAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        PersonnelVehicle vehicle;
        if (vehicleId is null || vehicleId == Guid.Empty)
        {
            vehicle = PersonnelVehicle.Create(
                actor.UserId,
                plateNumber,
                vehicleType,
                trim,
                model,
                color,
                notes,
                actor.UserId,
                nowUtc);
            dbContext.PersonnelVehicles.Add(vehicle);
        }
        else
        {
            vehicle = await dbContext.PersonnelVehicles
                .FirstOrDefaultAsync(item => item.Id == vehicleId && item.UserId == actor.UserId, cancellationToken)
                ?? throw new KeyNotFoundException("خودرو یافت نشد.");
            vehicle.Update(plateNumber, vehicleType, trim, model, color, notes, actor.UserId, nowUtc);
        }

        dbContext.AuditEvents.Add(AuditEvent.Create(
            nowUtc,
            "PersonnelVehicleSaved",
            "Succeeded",
            actor.UserId,
            actor.Upn,
            vehicle.Id.ToString("D"),
            actor.CorrelationId,
            actor.IpAddress,
            JsonSerializer.Serialize(new { vehicle.PlateNumber, vehicle.VehicleType })));
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapVehicle(vehicle);
    }

    public async Task DeleteVehicleAsync(
        PortalActor actor,
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanViewAsync(actor, cancellationToken);
        await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        PersonnelVehicle vehicle = await dbContext.PersonnelVehicles
            .FirstOrDefaultAsync(item => item.Id == vehicleId && item.UserId == actor.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("خودرو یافت نشد.");
        dbContext.PersonnelVehicles.Remove(vehicle);
        dbContext.AuditEvents.Add(AuditEvent.Create(
            DateTimeOffset.UtcNow,
            "PersonnelVehicleDeleted",
            "Succeeded",
            actor.UserId,
            actor.Upn,
            vehicle.Id.ToString("D"),
            actor.CorrelationId,
            actor.IpAddress));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCanViewAsync(PortalActor actor, CancellationToken cancellationToken)
    {
        if (!await accessEvaluator.HasAccessAsync(actor, PortalResources.PersonnelView, cancellationToken))
        {
            throw new UnauthorizedAccessException("اجازه دسترسی به پرونده پرسنلی را ندارید.");
        }
    }

    private static async Task<PersonnelProfile> GetOrCreateProfileAsync(
        PortalDbContext dbContext,
        PortalActor actor,
        CancellationToken cancellationToken)
    {
        PersonnelProfile? profile = await dbContext.PersonnelProfiles
            .FirstOrDefaultAsync(item => item.UserId == actor.UserId, cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

        profile = PersonnelProfile.Create(actor.UserId, actor.UserId, DateTimeOffset.UtcNow);
        dbContext.PersonnelProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private static PersonnelVehicleData MapVehicle(PersonnelVehicle vehicle) =>
        new(
            vehicle.Id,
            vehicle.PlateNumber,
            vehicle.VehicleType,
            vehicle.Trim,
            vehicle.Model,
            vehicle.Color,
            vehicle.Notes,
            vehicle.UpdatedAtUtc);
}
