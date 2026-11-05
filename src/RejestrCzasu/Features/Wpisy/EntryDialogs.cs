using System.Net;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;
using RejestrCzasu.Domain;
using RejestrCzasu.Shared;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Features.Wpisy;

/// <summary>
/// The row actions of an Entry in every report (R6, R7, AD-10): edit and copy in <see cref="EntryDialog"/>, delete
/// after <see cref="UiMessages.ConfirmAsync"/> (<c>DeleteEntry</c>). The input is the Entry's id, so the Lista wpisów and the Widok
/// miesiąca call the same methods; every method returns whether the report has to load its data again.
/// </summary>
public sealed class EntryDialogs(
    EntryStore store,
    DictionaryOptions options,
    EntryDateMemory dateMemory,
    CurrentUser currentUser,
    TimeProvider timeProvider,
    IDialogService dialogs,
    UiMessages messages)
{
    /// <summary>Opens the „Edycja Wpisu” window with the Entry as saved; <c>true</c> after a save or a missing Entry.</summary>
    public async Task<bool> EditAsync(int entryId)
    {
        if (await FindAsync(entryId) is not { } row)
        {
            return true;
        }
        // The Entry's current items are offered even when inactive (R2).
        var projects = await options.ProjectsAsync(row.ProjectId, CancellationToken.None);
        var workTypes = await options.WorkTypesAsync(row.WorkTypeId, CancellationToken.None);
        var input = InputOf(row, row.Date, projects, workTypes);
        return await ShowAsync("Edycja Wpisu", input, projects, workTypes, entryId, row.ProjectId, row.WorkTypeId);
    }

    /// <summary>
    /// Opens the „Kopia Wpisu” window: the original's Project, Work type, description and Czas with the date last set on
    /// Nowy wpis (or today in Poland). An inactive item of the original leaves its field empty (R7). Nothing is saved
    /// before Zapisz; <c>true</c> after a save or a missing Entry.
    /// </summary>
    public async Task<bool> CopyAsync(int entryId)
    {
        if (await FindAsync(entryId) is not { } row)
        {
            return true;
        }
        var userId = await currentUser.GetUserIdAsync();
        var date = dateMemory.Find(userId) ?? PolishCalendar.Today(timeProvider);
        var projects = await options.ProjectsAsync(null, CancellationToken.None);
        var workTypes = await options.WorkTypesAsync(null, CancellationToken.None);
        var input = InputOf(row, date, projects, workTypes);
        return await ShowAsync("Kopia Wpisu", input, projects, workTypes, null, null, null);
    }

    /// <summary>Deletes the Entry after a confirmation; <c>true</c> when it was deleted or no longer exists.</summary>
    public async Task<bool> DeleteAsync(int entryId)
    {
        if (await FindAsync(entryId) is not { } row)
        {
            return true;
        }
        if (!await messages.ConfirmAsync("Usunąć Wpis?", EntryMessages.DeleteQuestionHtml(row), "Usuń"))
        {
            return false;
        }
        if (await store.DeleteAsync(entryId) is EntryChange.Done)
        {
            await messages.ShowSuccessAsync(EntryMessages.Deleted(row));
        }
        else
        {
            await messages.ShowWarningAsync(EntryMessages.NotFound);
        }
        return true;
    }

    private async Task<EntryRow?> FindAsync(int entryId)
    {
        var row = await store.FindAsync(entryId);
        if (row is null)
        {
            await messages.ShowWarningAsync(EntryMessages.NotFound);
        }
        return row;
    }

    private static EntryInput InputOf(
        EntryRow row, DateOnly date, IReadOnlyList<ProjectOption> projects, IReadOnlyList<WorkTypeOption> workTypes) => new()
    {
        Date = date,
        // The instances of the option lists, chosen by id; a missing one (inactive in a copy) leaves the field empty.
        Project = projects.FirstOrDefault(p => p.Id == row.ProjectId),
        WorkType = workTypes.FirstOrDefault(w => w.Id == row.WorkTypeId),
        Description = row.Description,
        Czas = Czas.Format(row.Minutes),
    };

    private async Task<bool> ShowAsync(
        string title,
        EntryInput input,
        IReadOnlyList<ProjectOption> projects,
        IReadOnlyList<WorkTypeOption> workTypes,
        int? entryId,
        int? currentProjectId,
        int? currentWorkTypeId)
    {
        var result = await dialogs.ShowDialogAsync<EntryDialog>(dialog =>
        {
            dialog.Modal = true;
            // The library renders the title as (sanitized) HTML; the title is plain text.
            dialog.Header.Title = WebUtility.HtmlEncode(title);
            // The library footer stays hidden and without shortcuts, as in UiMessages.ConfirmAsync: Zapisz and Anuluj (from
            // EntryDialog) are in the button row of EntryForm. Escape closes the native dialog, which the library reports as Cancel.
            DialogFooter.HideLibraryFooter(dialog);
            dialog.Parameters.Add(nameof(EntryDialog.Model), input);
            dialog.Parameters.Add(nameof(EntryDialog.Projects), projects);
            dialog.Parameters.Add(nameof(EntryDialog.WorkTypes), workTypes);
            dialog.Parameters.Add(nameof(EntryDialog.EntryId), entryId);
            dialog.Parameters.Add(nameof(EntryDialog.CurrentProjectId), currentProjectId);
            dialog.Parameters.Add(nameof(EntryDialog.CurrentWorkTypeId), currentWorkTypeId);
        });
        if (result.Cancelled)
        {
            return false;
        }
        // The window has closed, so the bar is shown on the page, not under the modal (AD-10). The form's model holds the
        // saved values.
        if (result.GetValue<EntryChange>() is EntryChange.Done done)
        {
            await messages.ShowSuccessAsync(EntryMessages.Saved(input.Date!.Value, input.Project!.Code, done.Minutes));
        }
        else
        {
            await messages.ShowWarningAsync(EntryMessages.NotFound);
        }
        return true;
    }
}
