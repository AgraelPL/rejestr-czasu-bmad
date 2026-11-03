using System.Globalization;
using System.Net;
using RejestrCzasu.Domain;

namespace RejestrCzasu.Features.Wpisy;

/// <summary>
/// The texts about an Entry shown by the Entry screens and windows (Nowy wpis, Lista wpisów, <see cref="EntryDialog"/>):
/// one place, so a saved, deleted or missing Entry reads the same everywhere.
/// </summary>
internal static class EntryMessages
{
    /// <summary>Nothing saved: the chosen Project or Work type was deleted in the meantime.</summary>
    public const string MissingDictionaryItem = "Wybrany Projekt lub Typ pracy już nie istnieje.";

    /// <summary>The Entry was deleted in the meantime (or is not the User's); the report loads its data again.</summary>
    public const string NotFound = "Ten Wpis już nie istnieje. Lista została wczytana ponownie.";

    /// <summary>A date of an Entry as shown on the screens: <c>dd.MM.yyyy</c>.</summary>
    public static string Date(DateOnly date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    /// <summary>The short description of a saved Entry: date, Project code and Czas.</summary>
    public static string Saved(DateOnly date, string projectCode, int minutes) =>
        $"Zapisano Wpis: {Date(date)}, {projectCode}, {Czas.Format(minutes)}.";

    /// <summary>The question of the delete confirmation.</summary>
    public static string DeleteQuestion(EntryRow row) =>
        $"Usunąć Wpis z {row.DateText} ({row.ProjectCode}, {row.CzasText}): {row.Description}?";

    /// <summary>The content of the delete confirmation window: <see cref="DeleteQuestion"/> as HTML, the User's values encoded.</summary>
    public static string DeleteQuestionHtml(EntryRow row) => $"<p>{WebUtility.HtmlEncode(DeleteQuestion(row))}</p>";

    /// <summary>The message after a deleted Entry.</summary>
    public static string Deleted(EntryRow row) => $"Usunięto Wpis: {row.DateText}, {row.ProjectCode}, {row.CzasText}.";
}
