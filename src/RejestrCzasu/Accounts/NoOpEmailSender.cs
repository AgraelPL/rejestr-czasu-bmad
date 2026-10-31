using Microsoft.AspNetCore.Identity;
using RejestrCzasu.Data;

namespace RejestrCzasu.Accounts;

/// <summary>Accounts have no e-mail (AD-15): Identity gets a sender that never sends anything.</summary>
internal sealed class NoOpEmailSender : IEmailSender<AppUser>
{
    public Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink) => Task.CompletedTask;

    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink) => Task.CompletedTask;

    public Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode) => Task.CompletedTask;
}
