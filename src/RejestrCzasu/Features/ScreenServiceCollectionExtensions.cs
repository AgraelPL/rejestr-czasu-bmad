using RejestrCzasu.Features.Projekty;
using RejestrCzasu.Features.TypyPracy;
using RejestrCzasu.Shared;

namespace RejestrCzasu.Features;

/// <summary>
/// Registers the services of the screens, one instance per circuit (scoped). Lives in <c>Features/</c>, because it
/// knows the screens' data stores; <c>Shared/</c> never depends on <c>Features/</c>.
/// </summary>
public static class ScreenServiceCollectionExtensions
{
    /// <summary>
    /// Registers screen messages and confirmations of every screen (<see cref="Notify"/>, <see cref="Confirm"/>,
    /// AD-11) and the data of the Dictionary screens (<see cref="ProjectStore"/>, <see cref="WorkTypeStore"/>), which
    /// sit on top of the scoped <c>Data/UserDbContextFactory</c> (AD-4, AD-16). Needs <c>AddFluentUIComponents</c>.
    /// </summary>
    public static IServiceCollection AddAppScreens(this IServiceCollection services)
    {
        services.AddScoped<Notify>();
        services.AddScoped<Confirm>();
        services.AddScoped<ProjectStore>();
        services.AddScoped<WorkTypeStore>();
        return services;
    }
}
