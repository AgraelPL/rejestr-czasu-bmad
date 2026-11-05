using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Features.UstawieniaKonta;

namespace RejestrCzasu.Tests.Features;

/// <summary>Data of the Ustawienia konta (R7, R25, AD-7, AD-16) on PostgreSQL.</summary>
public sealed class AccountSettingsStoreTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private int _user;
    private int _otherUser;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetAsync();
        _user = await postgres.CreateUserAsync("ustawienia-a");
        _otherUser = await postgres.CreateUserAsync("ustawienia-b");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task NoRow_EmptyPrefix_ReadingCreatesNoRow()
    {
        Assert.Equal("", await StoreFor(_user).GetPrefixAsync(Ct));

        Assert.Empty(await RowsAsync());
    }

    [Fact]
    public async Task FirstSave_CreatesOneRowOfTheUser()
    {
        await StoreFor(_user).SavePrefixAsync("ABC", Ct);

        var row = Assert.Single(await RowsAsync());
        Assert.Equal((_user, "ABC"), (row.UserId, row.ExportPrefix));
        Assert.Equal("ABC", await StoreFor(_user).GetPrefixAsync(Ct));
    }

    [Fact]
    public async Task SecondSave_UpdatesTheSameRow()
    {
        var store = StoreFor(_user);
        await store.SavePrefixAsync("ABC", Ct);

        await store.SavePrefixAsync("XYZ", Ct);

        var row = Assert.Single(await RowsAsync());
        Assert.Equal((_user, "XYZ"), (row.UserId, row.ExportPrefix));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Clear_StoresEmpty_KeepsTheRow(string? cleared)
    {
        var store = StoreFor(_user);
        await store.SavePrefixAsync("ABC", Ct);

        await store.SavePrefixAsync(cleared, Ct);

        Assert.Equal("", Assert.Single(await RowsAsync()).ExportPrefix);
        Assert.Equal("", await store.GetPrefixAsync(Ct));
    }

    [Fact]
    public async Task SpacesAround_AreTrimmed()
    {
        await StoreFor(_user).SavePrefixAsync("  ABC  ", Ct);

        Assert.Equal("ABC", await StoreFor(_user).GetPrefixAsync(Ct));
    }

    [Fact]
    public async Task PolishLetters_StoredUnchanged()
    {
        await StoreFor(_user).SavePrefixAsync("Łódź-Zespół", Ct);

        Assert.Equal("Łódź-Zespół", await StoreFor(_user).GetPrefixAsync(Ct));
    }

    [Fact]
    public async Task TwoUsers_Separated()
    {
        await StoreFor(_otherUser).SavePrefixAsync("BBB", Ct);

        Assert.Equal("", await StoreFor(_user).GetPrefixAsync(Ct));
        Assert.Equal("BBB", await StoreFor(_otherUser).GetPrefixAsync(Ct));

        await StoreFor(_user).SavePrefixAsync("AAA", Ct);

        Assert.Equal("AAA", await StoreFor(_user).GetPrefixAsync(Ct));
        Assert.Equal("BBB", await StoreFor(_otherUser).GetPrefixAsync(Ct));
        Assert.Equal(2, (await RowsAsync()).Count);
    }

    private AccountSettingsStore StoreFor(int userId) => new(postgres.CreateLoggedInUserDbContextFactory(userId));

    /// <summary>All rows of <c>account_settings</c>, in system mode.</summary>
    private async Task<List<Data.AccountSettings>> RowsAsync()
    {
        await using var db = postgres.CreateContext();
        return await db.AccountSettings.IgnoreQueryFilters().AsNoTracking().ToListAsync(Ct);
    }
}
