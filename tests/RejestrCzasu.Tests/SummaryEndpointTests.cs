using System.Net;
using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;
using RejestrCzasu.Tests.Features;
using RejestrCzasu.Tests.Reporting;

namespace RejestrCzasu.Tests;

/// <summary><c>GET /zestawienie/{rok}/{miesiac}</c> (AD-14, AD-16, AD-17) through HTTP on PostgreSQL.</summary>
public sealed class SummaryEndpointTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Password = "haslo1";
    private const string SeptemberPath = "/zestawienie/2026/9";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await postgres.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task September_Xlsx_WithPolishFileName_SheetAndRazem()
    {
        var (app, userId) = await LoggedInAsync("zestawienie");
        await using var _ = app;
        await SeptemberShape.SeedAsync(postgres, userId, Ct);

        using var response = await app.Http.GetAsync(SeptemberPath, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);
        var disposition = string.Join(";", response.Content.Headers.GetValues("Content-Disposition"));
        // "wrzesień" in RFC 5987: only "ń" is encoded (%C5%84).
        Assert.Contains("filename*=UTF-8''Zestawienie-wrzesie%C5%84_2026.xlsx", disposition);
        Assert.Equal("Zestawienie-wrzesień_2026.xlsx", response.Content.Headers.ContentDisposition?.FileNameStar);
        Assert.Contains("attachment", disposition);
        using var workbook = XlsxDownload.Open(await response.Content.ReadAsByteArrayAsync(Ct));
        var sheet = Assert.Single(workbook.Worksheets);
        Assert.Equal("Wrzesień 2026", sheet.Name);
        Assert.All(Enumerable.Range(2, 28), r => Assert.False(sheet.Cell(r, 3).IsEmpty()));
        Assert.True(sheet.Row(30).IsEmpty());
        Assert.Equal("RAZEM WRZESIEŃ 2026", sheet.Cell(31, 5).GetText());
        Assert.Equal(103.0, sheet.Cell(31, 6).GetDouble());
        Assert.Equal(103.0, sheet.Cell(31, 8).GetDouble());
    }

    [Fact]
    public async Task AnotherUsersWpisy_NotInTheFile()
    {
        var (app, userId) = await LoggedInAsync("zestawienie");
        await using var _ = app;
        await SeptemberShape.SeedAsync(postgres, userId, Ct);
        var otherId = await postgres.CreateUserAsync("cudzy");
        await using (var other = await postgres.CreateUserContextAsync(otherId))
        {
            var project = new Project { Code = "CUD", Name = "Cudzy" };
            var workType = new WorkType { Name = "Cudza praca" };
            other.Projects.Add(project);
            other.WorkTypes.Add(workType);
            await other.SaveChangesAsync(Ct);
            other.TimeEntries.Add(new TimeEntry
            {
                Date = new DateOnly(2026, 9, 2), ProjectId = project.Id, WorkTypeId = workType.Id, Description = "Cudzy opis", Minutes = 600,
            });
            await other.SaveChangesAsync(Ct);
        }

        using var response = await app.Http.GetAsync(SeptemberPath, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var workbook = XlsxDownload.Open(await response.Content.ReadAsByteArrayAsync(Ct));
        var texts = workbook.Worksheet(1).CellsUsed().Select(c => c.GetFormattedString()).ToList();
        Assert.DoesNotContain(texts, t => t.Contains("Cudz", StringComparison.Ordinal) || t == "CUD");
        Assert.Equal(103.0, workbook.Worksheet(1).Cell(31, 6).GetDouble());
    }

    [Fact]
    public async Task EmptyMonth_404_WithPolishText_NotTheNotFoundPage()
    {
        var (app, _) = await LoggedInAsync("pusty");
        await using var __ = app;

        using var response = await app.Http.GetAsync(SeptemberPath, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("text/plain; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Equal("Brak Wpisów w Miesiącu Wrzesień 2026 - Zestawienia nie ma do pobrania.", await response.Content.ReadAsStringAsync(Ct));
    }

    [Theory]
    [InlineData("/zestawienie/2026/13")]
    [InlineData("/zestawienie/2026/0")]
    [InlineData("/zestawienie/0/9")]
    [InlineData("/zestawienie/10000/9")]
    [InlineData("/zestawienie/abc/9")]
    public async Task WrongMonth_404_NotFoundPage(string path)
    {
        var (app, _) = await LoggedInAsync("zly");
        await using var __ = app;

        using var response = await app.Http.GetAsync(path, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Nie znaleziono", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(Ct)));
    }

    [Fact]
    public async Task Anonymous_RedirectsToLogin_WithReturnUrl()
    {
        await using var app = new AppClient(postgres);

        using var response = await app.Http.GetAsync(SeptemberPath, Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal(AccountEndpoints.LoginPath, location.AbsolutePath);
        Assert.Equal("?ReturnUrl=%2Fzestawienie%2F2026%2F9", location.Query);
    }

    [Fact]
    public async Task BlockedDuringSession_RedirectsToLogin()
    {
        var (app, userId) = await LoggedInAsync("zablokowany");
        await using var _ = app;
        await SeptemberShape.SeedAsync(postgres, userId, Ct);
        await using (var db = postgres.CreateContext())
        {
            await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, AccountStatus.Blocked), Ct);
        }

        using var response = await app.Http.GetAsync(SeptemberPath, Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AccountEndpoints.LoginPath, response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task PasswordChangeRequired_RedirectsToChangePassword()
    {
        var (app, userId) = await LoggedInAsync("wymuszona", mustChangePassword: true);
        await using var _ = app;
        await SeptemberShape.SeedAsync(postgres, userId, Ct);

        using var response = await app.Http.GetAsync(SeptemberPath, Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AccountEndpoints.ChangePasswordPath, response.Headers.Location?.OriginalString);
    }

    private async Task<(AppClient App, int UserId)> LoggedInAsync(string userName, bool mustChangePassword = false)
    {
        var app = new AppClient(postgres);
        var userId = await app.CreateAccountAsync(userName, Password, AccountStatus.Active, mustChangePassword);
        using var response = await app.LoginAsync(userName, Password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return (app, userId);
    }
}
