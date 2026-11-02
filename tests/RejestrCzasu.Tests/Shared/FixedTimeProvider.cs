namespace RejestrCzasu.Tests.Shared;

/// <summary>A clock stopped at <paramref name="utcNow"/>, for "today" in Poland in tests.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
