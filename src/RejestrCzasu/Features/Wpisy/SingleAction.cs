namespace RejestrCzasu.Features.Wpisy;

/// <summary>
/// One row action at a time on a screen (AD-10): an action started while another one is still running (e.g. a second
/// click while its window is open) is ignored. The screen holds one instance for its whole report, not one per row.
/// </summary>
public sealed class SingleAction
{
    private bool _running;

    /// <summary>Runs <paramref name="action"/> unless another one is running; the lock is released even when it throws.</summary>
    public async Task RunAsync(Func<Task> action)
    {
        if (_running)
        {
            return;
        }
        _running = true;
        try
        {
            await action();
        }
        finally
        {
            _running = false;
        }
    }
}
