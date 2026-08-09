namespace EmpPortal.Domain.Hr;

public sealed class PayslipPeriodSetting
{
    private PayslipPeriodSetting()
    {
    }

    public Guid Id { get; private set; }

    public int PersianYear { get; private set; }

    public int PersianMonth { get; private set; }

    public bool IsVisibleToEmployees { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static PayslipPeriodSetting Create(
        int persianYear,
        int persianMonth,
        bool isVisibleToEmployees,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        ValidatePeriod(persianYear, persianMonth);
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);

        return new PayslipPeriodSetting
        {
            Id = Guid.NewGuid(),
            PersianYear = persianYear,
            PersianMonth = persianMonth,
            IsVisibleToEmployees = isVisibleToEmployees,
            UpdatedByUserId = actorUserId,
            UpdatedAtUtc = nowUtc
        };
    }

    public void SetVisibility(bool isVisibleToEmployees, Guid actorUserId, DateTimeOffset nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
        IsVisibleToEmployees = isVisibleToEmployees;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = nowUtc;
    }

    private static void ValidatePeriod(int persianYear, int persianMonth)
    {
        if (persianYear is < 1300 or > 1500)
        {
            throw new ArgumentOutOfRangeException(nameof(persianYear));
        }

        if (persianMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(persianMonth));
        }
    }
}
