namespace EmpPortal.Domain.Configuration;

public sealed class RuntimeSetting
{
    private RuntimeSetting(
        string key,
        string value,
        DateTimeOffset updatedAtUtc,
        Guid updatedByUserId)
    {
        Key = key;
        Value = value;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public string Key { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static RuntimeSetting Create(
        string key,
        string value,
        DateTimeOffset updatedAtUtc,
        Guid updatedByUserId)
    {
        Validate(key, value, updatedByUserId);
        return new RuntimeSetting(key.Trim(), value.Trim(), updatedAtUtc, updatedByUserId);
    }

    public void Update(string value, DateTimeOffset updatedAtUtc, Guid updatedByUserId)
    {
        Validate(Key, value, updatedByUserId);
        Value = value.Trim();
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    private static void Validate(string key, string value, Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfEqual(updatedByUserId, Guid.Empty);
    }
}
