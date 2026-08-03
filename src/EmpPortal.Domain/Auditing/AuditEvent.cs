namespace EmpPortal.Domain.Auditing;

public sealed class AuditEvent
{
    private AuditEvent()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string Outcome { get; private set; } = string.Empty;

    public Guid? ActorUserId { get; private set; }

    public string? ActorUpn { get; private set; }

    public string? Subject { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public string? IpAddress { get; private set; }

    public string? DetailsJson { get; private set; }

    public static AuditEvent Create(
        DateTimeOffset occurredAtUtc,
        string eventType,
        string outcome,
        Guid? actorUserId,
        string? actorUpn,
        string? subject,
        string correlationId,
        string? ipAddress,
        string? detailsJson = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        return new AuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = occurredAtUtc,
            EventType = eventType.Trim(),
            Outcome = outcome.Trim(),
            ActorUserId = actorUserId,
            ActorUpn = actorUpn?.Trim(),
            Subject = subject?.Trim(),
            CorrelationId = correlationId.Trim(),
            IpAddress = ipAddress?.Trim(),
            DetailsJson = detailsJson
        };
    }
}
