using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;

namespace RejestrCzasu.Tests;

/// <summary>Project and Work type options of the Entry form (R2, AD-10, AD-16).</summary>
public sealed class DictionaryOptionsTests(PostgresFixture postgres) : IAsyncLifetime
{
    /// <summary>U+00AD: ignorable in the culture comparison, so two otherwise equal strings tie.</summary>
    private const char SoftHyphen = (char)0xAD;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private int _user;
    private int _otherUser;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetAsync();
        _user = await postgres.CreateUserAsync("opcje-a");
        _otherUser = await postgres.CreateUserAsync("opcje-b");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Projects_NewEntry_ReturnsOnlyActive()
    {
        var (active, _) = await SeedProjectsAsync(_user);

        var options = await OptionsFor(_user).ProjectsAsync(null, Ct);

        Assert.Equal([new ProjectOption(active, "AKT", "Projekt AKT", true)], options);
    }

    [Fact]
    public async Task Projects_EditWithInactive_AddsCurrentInactive()
    {
        var (active, inactive) = await SeedProjectsAsync(_user);

        var options = await OptionsFor(_user).ProjectsAsync(inactive, Ct);

        Assert.Equal(
            [new ProjectOption(active, "AKT", "Projekt AKT", true), new ProjectOption(inactive, "NIE", "Projekt NIE", false)],
            options);
    }

    [Fact]
    public async Task Projects_EditWithActive_ReturnsItOnce()
    {
        var (active, _) = await SeedProjectsAsync(_user);

        var options = await OptionsFor(_user).ProjectsAsync(active, Ct);

        Assert.Equal([active], options.Select(o => o.Id));
    }

    [Fact]
    public async Task Projects_CurrentOfAnotherUser_ReturnsOnlyOwnActive()
    {
        var (active, _) = await SeedProjectsAsync(_user);
        var (otherActive, otherInactive) = await SeedProjectsAsync(_otherUser);
        var options = OptionsFor(_user);

        Assert.Equal([active], (await options.ProjectsAsync(otherInactive, Ct)).Select(o => o.Id));
        Assert.Equal([active], (await options.ProjectsAsync(otherActive, Ct)).Select(o => o.Id));
    }

    [Fact]
    public async Task Projects_OfAnotherUser_NeverAppear()
    {
        await SeedProjectsAsync(_otherUser);

        Assert.Empty(await OptionsFor(_user).ProjectsAsync(null, Ct));
    }

    [Fact]
    public async Task Projects_AreSortedByCode_InPolish()
    {
        await using (var db = await postgres.CreateUserContextAsync(_user))
        {
            db.Projects.AddRange(
                new Project { Code = "MIELEC", Name = "Projekt M" },
                new Project { Code = "ŁÓDŹ", Name = "Projekt Ł" },
                new Project { Code = "LUBLIN", Name = "Projekt L" });
            await db.SaveChangesAsync(Ct);
        }

        var options = await OptionsFor(_user).ProjectsAsync(null, Ct);

        Assert.Equal(["LUBLIN", "ŁÓDŹ", "MIELEC"], options.Select(o => o.Code));
    }

    /// <summary>
    /// Data only pl-PL orders correctly (Ą is a separate letter after A; the root collation gives ĄB, AC) and the
    /// <c>Id</c> tie-break. Codes differing only in case cannot coexist (stored upper-case, unique per User), so the
    /// tie uses a soft hyphen (U+00AD): ignorable in the culture comparison, distinct in the database.
    /// </summary>
    [Fact]
    public async Task Projects_AreSortedByCode_OnlyInPolish_WithIdTieBreak()
    {
        int first, second;
        await using (var db = await postgres.CreateUserContextAsync(_user))
        {
            db.Projects.AddRange(new Project { Code = "ĄB", Name = "Projekt Ą" }, new Project { Code = "AC", Name = "Projekt A" });
            var tieFirst = new Project { Code = $"X{SoftHyphen}Y", Name = "Projekt X1" };
            await db.SaveChangesAsync(Ct);
            db.Projects.Add(tieFirst);
            await db.SaveChangesAsync(Ct);
            var tieSecond = new Project { Code = "XY", Name = "Projekt X2" };
            db.Projects.Add(tieSecond);
            await db.SaveChangesAsync(Ct);
            (first, second) = (tieFirst.Id, tieSecond.Id);
        }
        Assert.True(first < second);

        var options = await OptionsFor(_user).ProjectsAsync(null, Ct);

        Assert.Equal(["AC", "ĄB", $"X{SoftHyphen}Y", "XY"], options.Select(o => o.Code));
        Assert.Equal([first, second], options.Skip(2).Select(o => o.Id));
    }

    [Fact]
    public async Task WorkTypes_NewEntry_ReturnsOnlyActive()
    {
        var (active, _) = await SeedWorkTypesAsync(_user);

        var options = await OptionsFor(_user).WorkTypesAsync(null, Ct);

        Assert.Equal([new WorkTypeOption(active, "Aktywny", true)], options);
    }

    [Fact]
    public async Task WorkTypes_EditWithInactive_AddsCurrentInactive()
    {
        var (active, inactive) = await SeedWorkTypesAsync(_user);

        var options = await OptionsFor(_user).WorkTypesAsync(inactive, Ct);

        Assert.Equal([new WorkTypeOption(active, "Aktywny", true), new WorkTypeOption(inactive, "Nieaktywny", false)], options);
    }

    [Fact]
    public async Task WorkTypes_EditWithActive_ReturnsItOnce()
    {
        var (active, _) = await SeedWorkTypesAsync(_user);

        var options = await OptionsFor(_user).WorkTypesAsync(active, Ct);

        Assert.Equal([active], options.Select(o => o.Id));
    }

    [Fact]
    public async Task WorkTypes_CurrentOfAnotherUser_ReturnsOnlyOwnActive()
    {
        var (active, _) = await SeedWorkTypesAsync(_user);
        var (otherActive, otherInactive) = await SeedWorkTypesAsync(_otherUser);
        var options = OptionsFor(_user);

        Assert.Equal([active], (await options.WorkTypesAsync(otherInactive, Ct)).Select(o => o.Id));
        Assert.Equal([active], (await options.WorkTypesAsync(otherActive, Ct)).Select(o => o.Id));
    }

    [Fact]
    public async Task WorkTypes_OfAnotherUser_NeverAppear()
    {
        await SeedWorkTypesAsync(_otherUser);

        Assert.Empty(await OptionsFor(_user).WorkTypesAsync(null, Ct));
    }

    [Fact]
    public async Task WorkTypes_AreSortedByName_InPolish()
    {
        await using (var db = await postgres.CreateUserContextAsync(_user))
        {
            db.WorkTypes.AddRange(new WorkType { Name = "Mielec" }, new WorkType { Name = "łódź" }, new WorkType { Name = "Lublin" });
            await db.SaveChangesAsync(Ct);
        }

        var options = await OptionsFor(_user).WorkTypesAsync(null, Ct);

        Assert.Equal(["Lublin", "łódź", "Mielec"], options.Select(o => o.Name));
    }

    /// <summary>
    /// Case-insensitive order (an ordinal comparison puts „ABD” before „abc”) and the <c>Id</c> tie-break. Names
    /// differing only in case cannot coexist (unique per User on <c>lower(name)</c>), so the tie uses a soft hyphen
    /// (U+00AD): ignorable in the culture comparison, distinct in the database.
    /// </summary>
    [Fact]
    public async Task WorkTypes_AreSortedIgnoringCase_WithIdTieBreak()
    {
        int first, second;
        await using (var db = await postgres.CreateUserContextAsync(_user))
        {
            db.WorkTypes.AddRange(new WorkType { Name = "ABD" }, new WorkType { Name = "abc" });
            var tieFirst = new WorkType { Name = $"x{SoftHyphen}y" };
            await db.SaveChangesAsync(Ct);
            db.WorkTypes.Add(tieFirst);
            await db.SaveChangesAsync(Ct);
            var tieSecond = new WorkType { Name = "xy" };
            db.WorkTypes.Add(tieSecond);
            await db.SaveChangesAsync(Ct);
            (first, second) = (tieFirst.Id, tieSecond.Id);
        }
        Assert.True(first < second);

        var options = await OptionsFor(_user).WorkTypesAsync(null, Ct);

        Assert.Equal(["abc", "ABD", $"x{SoftHyphen}y", "xy"], options.Select(o => o.Name));
        Assert.Equal([first, second], options.Skip(2).Select(o => o.Id));
    }

    [Fact]
    public async Task WithoutUser_Throws()
    {
        var options = new DictionaryOptions(postgres.CreateUserDbContextFactory());

        await Assert.ThrowsAsync<NoCurrentUserException>(() => options.ProjectsAsync(null, Ct));
        await Assert.ThrowsAsync<NoCurrentUserException>(() => options.WorkTypesAsync(null, Ct));
    }

    /// <summary>
    /// <see cref="DictionaryOptions"/> for a logged-in User, through the public <see cref="UserDbContextFactory"/> path.
    /// <see cref="CurrentUser"/> runs in circuit mode, so the User is per instance (<c>HttpContextAccessor</c> is a
    /// static AsyncLocal shared by all instances). The only place that builds <see cref="CurrentUser"/>.
    /// </summary>
    private DictionaryOptions OptionsFor(int userId)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture))], "test"));
        var provider = new ServerAuthenticationStateProvider();
        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(principal)));
        var currentUser = new CurrentUser(new HttpContextAccessor(), provider);
        _ = new CurrentUserCircuitHandler(currentUser);
        return new DictionaryOptions(new UserDbContextFactory(postgres.ContextFactory, currentUser));
    }

    /// <summary>An active Project <c>AKT</c> and an inactive Project <c>NIE</c> of the User.</summary>
    private async Task<(int Active, int Inactive)> SeedProjectsAsync(int userId)
    {
        await using var db = await postgres.CreateUserContextAsync(userId);
        var active = new Project { Code = "AKT", Name = "Projekt AKT" };
        var inactive = new Project { Code = "NIE", Name = "Projekt NIE", IsActive = false };
        db.Projects.AddRange(active, inactive);
        await db.SaveChangesAsync(Ct);
        return (active.Id, inactive.Id);
    }

    /// <summary>An active Work type <c>Aktywny</c> and an inactive Work type <c>Nieaktywny</c> of the User.</summary>
    private async Task<(int Active, int Inactive)> SeedWorkTypesAsync(int userId)
    {
        await using var db = await postgres.CreateUserContextAsync(userId);
        var active = new WorkType { Name = "Aktywny" };
        var inactive = new WorkType { Name = "Nieaktywny", IsActive = false };
        db.WorkTypes.AddRange(active, inactive);
        await db.SaveChangesAsync(Ct);
        return (active.Id, inactive.Id);
    }
}
