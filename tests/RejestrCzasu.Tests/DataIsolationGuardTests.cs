using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;
using RejestrCzasu.Options;

namespace RejestrCzasu.Tests;

/// <summary>
/// Guards of the data separation (AD-16) that live outside <see cref="AppDbContext"/>: the database registration
/// without pooling and the banned API list (RS0030) with its only allowed suppressions. No database needed.
/// </summary>
public sealed partial class DataIsolationGuardTests
{
    /// <summary>The only files allowed to suppress RS0030 (AD-16), relative to <c>src/RejestrCzasu</c>.</summary>
    private static readonly string[] AllowedRs0030Suppressions = ["Accounts/AccountStats.cs", "Accounts/DeleteAccount.cs"];

    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string AppDirectory = Path.Combine(RepositoryRoot, "src", "RejestrCzasu");

    [Fact]
    public async Task AddAppDatabase_DoesNotPoolContexts()
    {
        var services = new ServiceCollection();
        services.AddOptions<DatabaseOptions>();
        services.AddAppDatabase();
        // UserDbContextFactory takes the User from CurrentUser (registered by AddAccounts in the application).
        services.AddHttpContextAccessor();
        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
        services.AddScoped<CurrentUser>();
        await using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.DoesNotContain(services, s => s.ServiceType.Name.Contains("Pool", StringComparison.Ordinal)
            || (s.ImplementationType?.Name.Contains("Pool", StringComparison.Ordinal) ?? false));
        var factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        Assert.False(factory.GetType().IsGenericType
            && factory.GetType().GetGenericTypeDefinition() == typeof(PooledDbContextFactory<>));

        // Behaviour: a context bound to a User and disposed never comes back as the next system-mode context.
        await using (var scope = provider.CreateAsyncScope())
        {
            var userFactory = scope.ServiceProvider.GetRequiredService<UserDbContextFactory>();
            await using var userContext = await userFactory.CreateDbContextAsync(7, TestContext.Current.CancellationToken);
            Assert.Equal(7, userContext.CurrentUserId);
        }
        await using var next = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Null(next.CurrentUserId);
    }

    [Fact]
    public void DatabaseRegistration_IsOnlyInAddAppDatabase()
    {
        var registration = RegistrationCall();
        var offenders = SourceFiles(AppDirectory, "*.cs")
            .Where(file => Path.GetFileName(file) != "DatabaseServiceCollectionExtensions.cs")
            .Where(file => registration.IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(RepositoryRoot, file))
            .ToList();
        Assert.Empty(offenders);
        Assert.Contains("AddAppDatabase()", File.ReadAllText(Path.Combine(AppDirectory, "Program.cs")));
    }

    [Fact]
    public void Rs0030_IsSuppressedOnlyInAllowedFiles()
    {
        var allowed = AllowedRs0030Suppressions
            .Select(path => Path.GetFullPath(Path.Combine(AppDirectory, path)))
            .Append(Path.Combine(AppDirectory, "BannedSymbols.txt"))
            .Append(Path.Combine(AppDirectory, "RejestrCzasu.csproj"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = SourceFiles(Path.Combine(RepositoryRoot, "src"), "*")
            .Concat(Directory.EnumerateFiles(RepositoryRoot)); // root .editorconfig, Directory.Build.*, *.globalconfig

        var offenders = candidates
            .Where(file => !allowed.Contains(Path.GetFullPath(file)))
            .Where(file => File.ReadAllText(file).Contains("RS0030", StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(RepositoryRoot, file))
            .ToList();
        Assert.Empty(offenders);

        // In the project file RS0030 only turns the warning into an error.
        var project = File.ReadAllText(Path.Combine(AppDirectory, "RejestrCzasu.csproj"));
        Assert.Contains("<WarningsAsErrors>$(WarningsAsErrors);RS0030</WarningsAsErrors>", project);
        Assert.Single(Regex.Matches(project, "RS0030"));
        Assert.Contains("<AdditionalFiles Include=\"BannedSymbols.txt\" />", project);
    }

    /// <summary>
    /// Every overload of the banned EF Core methods is on the list with its exact documentation id
    /// (built from the method's signature), also after an EF Core upgrade.
    /// </summary>
    [Fact]
    public void BannedSymbols_CoverEveryOverload()
    {
        var banned = File.ReadAllLines(Path.Combine(AppDirectory, "BannedSymbols.txt"))
            .Where(line => line.StartsWith("M:", StringComparison.Ordinal))
            .Select(line => line.Split(';')[0])
            .ToHashSet(StringComparer.Ordinal);
        (Type Type, string[] Methods)[] expected =
        [
            (typeof(EntityFrameworkQueryableExtensions), ["IgnoreQueryFilters"]),
            (typeof(RelationalDatabaseFacadeExtensions),
                ["SqlQuery", "SqlQueryRaw", "ExecuteSql", "ExecuteSqlAsync", "ExecuteSqlRaw", "ExecuteSqlRawAsync",
                 "ExecuteSqlInterpolated", "ExecuteSqlInterpolatedAsync"]),
            (typeof(RelationalQueryableExtensions), ["FromSqlRaw"]),
        ];

        foreach (var (type, methods) in expected)
        {
            foreach (var method in methods)
            {
                var overloads = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == method)
                    .Select(DocumentationId)
                    .ToList();
                Assert.True(overloads.Count > 0, $"{type.FullName}.{method}");
                Assert.All(overloads, id => Assert.True(banned.Contains(id), $"Brak na liście: {id}"));
            }
        }
    }

    /// <summary>The documentation comment id of a method (<c>M:Namespace.Type.Method``1(Param,...)</c>).</summary>
    private static string DocumentationId(MethodInfo method)
    {
        var id = $"M:{TypeId(method.DeclaringType!)}.{method.Name}";
        if (method.IsGenericMethod)
        {
            id += "``" + method.GetGenericArguments().Length;
        }
        var parameters = method.GetParameters();
        if (parameters.Length > 0)
        {
            id += "(" + string.Join(",", parameters.Select(p => TypeId(p.ParameterType))) + ")";
        }
        return id;
    }

    private static string TypeId(Type type)
    {
        if (type.IsGenericMethodParameter)
        {
            return "``" + type.GenericParameterPosition;
        }
        if (type.IsGenericTypeParameter)
        {
            return "`" + type.GenericParameterPosition;
        }
        if (type.IsByRef)
        {
            return TypeId(type.GetElementType()!) + "@";
        }
        if (type.IsArray)
        {
            return TypeId(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        }
        if (type.IsGenericType)
        {
            var name = type.GetGenericTypeDefinition().FullName!;
            name = name[..name.IndexOf('`', StringComparison.Ordinal)].Replace('+', '.');
            return $"{name}{{{string.Join(",", type.GetGenericArguments().Select(TypeId))}}}";
        }
        return type.FullName!.Replace('+', '.');
    }

    /// <summary>A call that registers a context or context factory (pooled or not).</summary>
    private static Regex RegistrationCall() => RegistrationCallRegex();

    [GeneratedRegex(@"\.(AddDbContext|AddDbContextFactory|AddPooledDbContextFactory|AddDbContextPool)\s*<")]
    private static partial Regex RegistrationCallRegex();

    private static IEnumerable<string> SourceFiles(string directory, string pattern) =>
        Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(directory, file).Split(Path.DirectorySeparatorChar)
                .Any(part => part is "bin" or "obj"));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RejestrCzasu.sln")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Nie znaleziono RejestrCzasu.sln.");
    }
}
