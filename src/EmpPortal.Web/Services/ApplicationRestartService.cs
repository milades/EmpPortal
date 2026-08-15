namespace EmpPortal.Web.Services;

internal sealed class ApplicationRestartService(
    IHostApplicationLifetime applicationLifetime,
    ILogger<ApplicationRestartService> logger)
{
    private static readonly Action<ILogger, Exception?> RestartRequested =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2101, nameof(RestartRequested)),
            "A controlled application restart was requested from the administration panel.");

    private static readonly Action<ILogger, Exception?> RestartFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2102, nameof(RestartFailed)),
            "The controlled application restart could not be scheduled.");

    private int restartScheduled;

    public bool ScheduleRestart(TimeSpan delay)
    {
        if (Interlocked.Exchange(ref restartScheduled, 1) == 1)
        {
            return false;
        }

        _ = RestartAsync(delay);
        return true;
    }

    private async Task RestartAsync(TimeSpan delay)
    {
        try
        {
            RestartRequested(logger, null);
            await Task.Delay(delay);
            applicationLifetime.StopApplication();
        }
        catch (Exception exception)
        {
            Interlocked.Exchange(ref restartScheduled, 0);
            RestartFailed(logger, exception);
        }
    }
}
