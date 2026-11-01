using System.Net;
using Microsoft.FluentUI.AspNetCore.Components;

namespace RejestrCzasu.Shared;

/// <summary>
/// Every confirmation (AD-11): deleting an Entry or a Dictionary item, Account actions. A modal window of
/// <see cref="IDialogService"/> with its own buttons (<see cref="ConfirmDialog"/>): Anuluj has the focus, the primary
/// button has no keyboard shortcut (neither Enter nor "Y" confirms), Escape cancels.
/// </summary>
public sealed class Confirm(IDialogService dialogs)
{
    /// <summary>Asks the User; <c>true</c> only when the primary button was clicked.</summary>
    /// <param name="title">Window title, plain text.</param>
    /// <param name="message">Question, plain text.</param>
    /// <param name="primaryLabel">Label of the primary button, e.g. <c>Usuń</c>.</param>
    public async Task<bool> AskAsync(string title, string message, string primaryLabel)
    {
        var result = await dialogs.ShowDialogAsync<ConfirmDialog>(options =>
        {
            options.Modal = true;
            // The library renders the title as (sanitized) HTML; the title is plain text.
            options.Header.Title = WebUtility.HtmlEncode(title);
            // The library footer stays hidden and without shortcuts (its primary default is Enter); the buttons are in
            // ConfirmDialog. Escape closes the native dialog, which the library reports as Cancel.
            HideLibraryAction(options.Footer.PrimaryAction);
            HideLibraryAction(options.Footer.SecondaryAction);
            options.Parameters.Add(nameof(ConfirmDialog.Message), message);
            options.Parameters.Add(nameof(ConfirmDialog.PrimaryLabel), primaryLabel);
        });
        return !result.Cancelled;
    }

    private static void HideLibraryAction(DialogOptionsFooterAction action)
    {
        action.Label = null;
        action.Visible = false;
        action.ShortCut = null;
    }
}
