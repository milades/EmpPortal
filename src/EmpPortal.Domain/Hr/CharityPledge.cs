namespace EmpPortal.Domain.Hr;

public enum CharityPledgeMode
{
    OneTime = 1,
    MonthlyRange = 2
}

public sealed class CharityPledge
{
    private CharityPledge()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public decimal Amount { get; private set; }

    public CharityPledgeMode Mode { get; private set; }

    public int StartPersianYear { get; private set; }

    public int StartPersianMonth { get; private set; }

    public int? EndPersianYear { get; private set; }

    public int? EndPersianMonth { get; private set; }

    public string? Note { get; private set; }

    public bool IsConfirmed { get; private set; }

    public DateTimeOffset? ConfirmedAtUtc { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? ResultsExportedAtUtc { get; private set; }

    public Guid? ResultsExportedByUserId { get; private set; }

    public bool IsResultsExported => ResultsExportedAtUtc.HasValue;

    public static CharityPledge CreateOneTime(
        Guid userId,
        decimal amount,
        int persianYear,
        int persianMonth,
        string? note,
        bool confirm,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        CharityPledge pledge = CreateBase(userId, amount, CharityPledgeMode.OneTime, note, actorUserId, nowUtc);
        ValidatePeriod(persianYear, persianMonth);
        pledge.StartPersianYear = persianYear;
        pledge.StartPersianMonth = persianMonth;
        pledge.EndPersianYear = null;
        pledge.EndPersianMonth = null;
        if (confirm)
        {
            pledge.Confirm(actorUserId, nowUtc);
        }

        return pledge;
    }

    public static CharityPledge CreateMonthlyRange(
        Guid userId,
        decimal amount,
        int startPersianYear,
        int startPersianMonth,
        int endPersianYear,
        int endPersianMonth,
        string? note,
        bool confirm,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        ValidatePeriod(startPersianYear, startPersianMonth);
        ValidatePeriod(endPersianYear, endPersianMonth);
        int startKey = startPersianYear * 100 + startPersianMonth;
        int endKey = endPersianYear * 100 + endPersianMonth;
        if (endKey < startKey)
        {
            throw new ArgumentException("بازه ماهانه انفاق نامعتبر است.");
        }

        CharityPledge pledge = CreateBase(userId, amount, CharityPledgeMode.MonthlyRange, note, actorUserId, nowUtc);
        pledge.StartPersianYear = startPersianYear;
        pledge.StartPersianMonth = startPersianMonth;
        pledge.EndPersianYear = endPersianYear;
        pledge.EndPersianMonth = endPersianMonth;
        if (confirm)
        {
            pledge.Confirm(actorUserId, nowUtc);
        }

        return pledge;
    }

    public void Confirm(Guid actorUserId, DateTimeOffset nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
        IsConfirmed = true;
        ConfirmedAtUtc = nowUtc;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkResultsExported(Guid actorUserId, DateTimeOffset nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
        ResultsExportedAtUtc = nowUtc;
        ResultsExportedByUserId = actorUserId;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = nowUtc;
    }

    private static CharityPledge CreateBase(
        Guid userId,
        decimal amount,
        CharityPledgeMode mode,
        string? note,
        Guid actorUserId,
        DateTimeOffset nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "مبلغ انفاق باید بزرگ‌تر از صفر باشد.");
        }

        string? normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (normalizedNote is { Length: > 500 })
        {
            throw new ArgumentOutOfRangeException(nameof(note));
        }

        return new CharityPledge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = decimal.Round(amount, 0, MidpointRounding.AwayFromZero),
            Mode = mode,
            Note = normalizedNote,
            CreatedByUserId = actorUserId,
            CreatedAtUtc = nowUtc,
            UpdatedByUserId = actorUserId,
            UpdatedAtUtc = nowUtc
        };
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
