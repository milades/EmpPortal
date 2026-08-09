namespace EmpPortal.Domain.Hr;

public sealed class PersonnelVehicle
{
    private PersonnelVehicle()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string PlateNumber { get; private set; } = string.Empty;

    public string VehicleType { get; private set; } = string.Empty;

    public string? Trim { get; private set; }

    public string? Model { get; private set; }

    public string? Color { get; private set; }

    public string? Notes { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static PersonnelVehicle Create(
        Guid userId,
        string plateNumber,
        string vehicleType,
        string? trim,
        string? model,
        string? color,
        string? notes,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
        PersonnelVehicle vehicle = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };
        vehicle.Update(plateNumber, vehicleType, trim, model, color, notes, actorUserId, nowUtc);
        return vehicle;
    }

    public void Update(
        string plateNumber,
        string vehicleType,
        string? trim,
        string? model,
        string? color,
        string? notes,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
        PlateNumber = RequireText(plateNumber, nameof(plateNumber), 32);
        VehicleType = RequireText(vehicleType, nameof(vehicleType), 80);
        Trim = NormalizeOptional(trim, 80);
        Model = NormalizeOptional(model, 80);
        Color = NormalizeOptional(color, 40);
        Notes = NormalizeOptional(notes, 500);
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = nowUtc;
    }

    private static string RequireText(string value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("مقدار الزامی است.", paramName);
        }

        string normalized = value.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(normalized.Length, maxLength, paramName);
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(normalized.Length, maxLength, nameof(value));
        return normalized;
    }
}
