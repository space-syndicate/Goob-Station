namespace Content.Server._BlackM.Underground;

public enum UndergroundExpeditionPhase : byte
{
    Idle,
    Generating,
    Ready,
    Outbound,
    Exploring,
    Returning,
    AwaitingCleanup,
    Faulted,
}

public sealed class UndergroundExpeditionSession
{
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(10);
    private static readonly int[] WarningSeconds = [300, 180, 120, 60, 30, 10];

    public UndergroundExpeditionPhase Phase { get; private set; } = UndergroundExpeditionPhase.Idle;
    public long GenerationId { get; private set; }
    public TimeSpan? DepartureDeadline { get; private set; }
    public bool EarlyReturnRequested { get; private set; }

    private readonly HashSet<int> _sentWarnings = new();

    public bool TryBeginGeneration(out long generationId)
    {
        generationId = GenerationId;
        if (Phase != UndergroundExpeditionPhase.Idle)
            return false;

        generationId = ++GenerationId;
        DepartureDeadline = null;
        EarlyReturnRequested = false;
        _sentWarnings.Clear();
        Phase = UndergroundExpeditionPhase.Generating;
        return true;
    }

    public bool GenerationFinished(long generationId, bool succeeded)
    {
        if (Phase != UndergroundExpeditionPhase.Generating || generationId != GenerationId)
            return false;

        Phase = succeeded ? UndergroundExpeditionPhase.Ready : UndergroundExpeditionPhase.Idle;
        return true;
    }

    public bool TryBeginOutbound()
    {
        if (Phase != UndergroundExpeditionPhase.Ready)
            return false;

        Phase = UndergroundExpeditionPhase.Outbound;
        return true;
    }

    public bool OutboundFinished(bool succeeded, TimeSpan now, TimeSpan duration)
    {
        if (Phase != UndergroundExpeditionPhase.Outbound)
            return false;

        if (!succeeded || duration <= TimeSpan.Zero)
        {
            Phase = UndergroundExpeditionPhase.Faulted;
            return true;
        }

        DepartureDeadline = now + duration;
        _sentWarnings.Clear();
        foreach (var seconds in WarningSeconds)
        {
            if (TimeSpan.FromSeconds(seconds) >= duration)
                _sentWarnings.Add(seconds);
        }
        Phase = UndergroundExpeditionPhase.Exploring;
        return true;
    }

    public TimeSpan TimeRemaining(TimeSpan now)
    {
        if (Phase != UndergroundExpeditionPhase.Exploring || DepartureDeadline is not { } deadline)
            return TimeSpan.Zero;

        return deadline > now ? deadline - now : TimeSpan.Zero;
    }

    // Returns only the most urgent unsent warning if multiple thresholds were skipped.
    public int? ConsumeWarning(TimeSpan now)
    {
        if (Phase != UndergroundExpeditionPhase.Exploring || DepartureDeadline is not { } deadline)
            return null;

        var remaining = (deadline - now).TotalSeconds;
        if (remaining <= 0)
            return null;

        int? warning = null;
        foreach (var seconds in WarningSeconds)
        {
            if (remaining <= seconds && _sentWarnings.Add(seconds))
                warning = seconds;
        }

        return warning;
    }

    // Do not postpone the normal departure, and never reset an existing early countdown.
    public bool CanRequestEarlyReturn(TimeSpan now, TimeSpan warningDuration)
    {
        return Phase == UndergroundExpeditionPhase.Exploring && !EarlyReturnRequested &&
               warningDuration > TimeSpan.Zero && DepartureDeadline is { } deadline &&
               now + warningDuration < deadline;
    }

    public bool TryRequestEarlyReturn(TimeSpan now, TimeSpan warningDuration)
    {
        if (!CanRequestEarlyReturn(now, warningDuration))
            return false;

        DepartureDeadline = now + warningDuration;
        EarlyReturnRequested = true;
        // The caller sends the initial warning; suppress thresholds already passed.
        foreach (var seconds in WarningSeconds)
        {
            if (TimeSpan.FromSeconds(seconds) >= warningDuration)
                _sentWarnings.Add(seconds);
        }
        return true;
    }

    public bool TryBeginReturn(TimeSpan now)
    {
        if (Phase != UndergroundExpeditionPhase.Exploring || DepartureDeadline is not { } deadline ||
            now < deadline)
        {
            return false;
        }

        Phase = UndergroundExpeditionPhase.Returning;
        return true;
    }

    public bool ReturnFinished(bool succeeded)
    {
        if (Phase != UndergroundExpeditionPhase.Returning)
            return false;

        Phase = succeeded ? UndergroundExpeditionPhase.AwaitingCleanup : UndergroundExpeditionPhase.Faulted;
        return true;
    }

    public void Fail()
    {
        Phase = UndergroundExpeditionPhase.Faulted;
    }

    public bool RecoverFromFault()
    {
        if (Phase != UndergroundExpeditionPhase.Faulted)
            return false;

        Phase = UndergroundExpeditionPhase.Idle;
        DepartureDeadline = null;
        EarlyReturnRequested = false;
        _sentWarnings.Clear();
        return true;
    }

    public bool CleanupFinished()
    {
        if (Phase != UndergroundExpeditionPhase.AwaitingCleanup)
            return false;

        Phase = UndergroundExpeditionPhase.Idle;
        DepartureDeadline = null;
        EarlyReturnRequested = false;
        _sentWarnings.Clear();
        return true;
    }
}
