using RejestrCzasu.Features.Wpisy;

namespace RejestrCzasu.Tests.Features;

/// <summary>One row action at a time on a screen (AD-10): <see cref="SingleAction"/> shared by every row of a report.</summary>
public sealed class SingleActionTests
{
    [Fact]
    public async Task Run_WhileAnotherIsRunning_IsIgnored()
    {
        var gate = new SingleAction();
        var first = new TaskCompletionSource();
        var second = false;

        var running = gate.RunAsync(() => first.Task);
        await gate.RunAsync(() => { second = true; return Task.CompletedTask; });
        first.SetResult();
        await running;

        Assert.False(second);
    }

    [Fact]
    public async Task Run_AfterTheFirstFinished_Runs()
    {
        var gate = new SingleAction();
        var count = 0;

        await gate.RunAsync(() => { count++; return Task.CompletedTask; });
        await gate.RunAsync(() => { count++; return Task.CompletedTask; });

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Run_AfterTheFirstThrew_RunsAndTheExceptionGoesOn()
    {
        var gate = new SingleAction();
        var second = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => gate.RunAsync(() => throw new InvalidOperationException()));
        await gate.RunAsync(() => { second = true; return Task.CompletedTask; });

        Assert.True(second);
    }
}
