using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RejestrCzasu.Options;

namespace RejestrCzasu.Data;

/// <summary>The only place that registers the application database (AD-4, AD-16).</summary>
public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IDbContextFactory{TContext}"/> of <see cref="AppDbContext"/> (system mode: Startup,
    /// Identity, Data Protection) and <see cref="UserDbContextFactory"/> (the only source of User mode, scoped: it
    /// takes the User from the scoped <c>Accounts/CurrentUser</c>). <c>AddDbContextFactory</c> also registers a scoped
    /// <see cref="AppDbContext"/> created by the factory: system mode, used by the Identity stores
    /// (<c>AddEntityFrameworkStores</c>) and <c>Accounts/</c>.
    /// </summary>
    /// <remarks>
    /// Never use <c>AddPooledDbContextFactory</c> or <c>AddDbContextPool</c> here: a pooled context keeps
    /// <see cref="AppDbContext.CurrentUserId"/> after it returns to the pool, so the next "system" context
    /// would be bound to the previous User (AD-16). A test checks that the registration does not pool.
    /// </remarks>
    public static IServiceCollection AddAppDatabase(this IServiceCollection services)
    {
        services.AddDbContextFactory<AppDbContext>((provider, options) =>
            options.UseAppDatabase(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.BuildConnectionString()));
        services.AddScoped<UserDbContextFactory>();
        return services;
    }

    /// <summary>Provider and naming convention of the application database.</summary>
    public static DbContextOptionsBuilder UseAppDatabase(this DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
}
