using RejestrCzasu.Shared;

namespace RejestrCzasu.Tests.Shared;

/// <summary>The date of the Nowy wpis screen remembered per User (AD-10, R7).</summary>
public sealed class EntryDateMemoryTests
{
    [Fact]
    public void Remember_IsPerUser_LastDateWins()
    {
        var memory = new EntryDateMemory();

        memory.Remember(1, new DateOnly(2026, 9, 1));
        memory.Remember(1, new DateOnly(2026, 9, 3));

        Assert.Equal(new DateOnly(2026, 9, 3), memory.Find(1));
        Assert.Null(memory.Find(2));
    }

    [Fact]
    public void Forget_RemovesOnlyThatUser()
    {
        var memory = new EntryDateMemory();
        memory.Remember(1, new DateOnly(2026, 9, 3));
        memory.Remember(2, new DateOnly(2026, 9, 4));

        memory.Forget(1);
        memory.Forget(3);

        Assert.Null(memory.Find(1));
        Assert.Equal(new DateOnly(2026, 9, 4), memory.Find(2));
    }
}
