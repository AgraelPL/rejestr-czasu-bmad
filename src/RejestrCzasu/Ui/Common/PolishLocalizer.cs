using Microsoft.FluentUI.AspNetCore.Components;

namespace RejestrCzasu.Ui;

/// <summary>
/// Polskie teksty komponentów Fluent UI Blazor (przyciski okien, paginacja, menu kolumn tabeli…).
/// Rejestracja: <c>AddFluentUIComponents(o =&gt; o.Localizer = new PolishLocalizer())</c> – robi to <see cref="ServiceCollectionExtensions.AddEodPlatformUI"/>.
/// </summary>
/// <remarks>
/// Klucz spoza słownika idzie do <c>IFluentLocalizer.GetDefault</c> biblioteki: daje on angielski tekst klucza nowszej
/// wersji Fluent UI, a dla klucza spoza zasobu (Fluent UI 5.0.1) rzuca <c>ArgumentException</c>.
/// Słownik ma dokładnie klucze zasobu <c>LanguageResource</c> zainstalowanego Fluent UI, z tymi samymi miejscami <c>{n}</c>;
/// pilnuje tego test pokrycia w <c>PolishTextsTests</c> (aktualizacja Fluent UI z nowym kluczem = czerwony test z nazwą klucza).
/// </remarks>
public sealed class PolishLocalizer : IFluentLocalizer
{
    /// <summary>Polskie teksty po kluczach zasobu <c>LanguageResource</c> zainstalowanego Fluent UI.</summary>
    internal static readonly IReadOnlyDictionary<string, string> Texts = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["LayoutHamburger_Title"] = "Menu",
        ["LayoutHamburger_Close"] = "Zamknij",
        ["FluentInputBase_Required"] = "Wymagane",
        ["FluentInputBase_RequiredMessage"] = "To pole jest wymagane.",
        ["MessageBox_ButtonYes"] = "Tak",
        ["MessageBox_ButtonOk"] = "OK",
        ["MessageBox_ButtonNo"] = "Nie",
        ["MessageBox_ButtonCancel"] = "Anuluj",
        ["MessageBox_Success"] = "Sukces",
        ["MessageBox_Warning"] = "Ostrzeżenie",
        ["MessageBox_Error"] = "Błąd",
        ["MessageBox_Information"] = "Informacja",
        ["MessageBox_Confirmation"] = "Potwierdzenie",
        ["Calendar_FieldMustBeADate"] = "Pole {0} musi zawierać datę.",
        ["Field_ErrorMessage"] = "Wystąpił błąd.",
        ["Field_SuccessMessage"] = "Dane poprawne.",
        ["Field_WarningMessage"] = "Sprawdź tę wartość.",
        ["TimeAgo_SecondAgo"] = "Przed chwilą",
        ["TimeAgo_SecondsAgo"] = "{0} s temu",
        ["TimeAgo_MinuteAgo"] = "{0} minutę temu",
        ["TimeAgo_MinutesAgo"] = "{0} min temu",
        ["TimeAgo_HourAgo"] = "{0} godzinę temu",
        ["TimeAgo_HoursAgo"] = "{0} godz. temu",
        ["TimeAgo_DayAgo"] = "{0} dzień temu",
        ["TimeAgo_DaysAgo"] = "{0} dni temu",
        ["TimeAgo_MonthAgo"] = "{0} miesiąc temu",
        ["TimeAgo_MonthsAgo"] = "{0} mies. temu",
        ["TimeAgo_YearAgo"] = "{0} rok temu",
        ["TimeAgo_YearsAgo"] = "{0} lat temu",
        ["FluentInputFile_LoadingBefore"] = "Wczytywanie…",
        ["FluentInputFile_LoadingCompleted"] = "Zakończono",
        ["FluentInputFile_LoadingCanceled"] = "Anulowano",
        ["FluentInputFile_LoadingInProgress"] = "Wczytywanie {0}/{1} - {2}",
        ["TreeItem_LoadingMessage"] = "Wczytywanie…",
        ["ErrorBoundary_Close"] = "Zamknij",
        ["ErrorBoundary_Title"] = "Wystąpił błąd",
        ["ErrorBoundary_Message"] = "Wystąpił nieoczekiwany błąd. Zgłoś go administratorowi.",
        ["Paginator_GoFirstPage"] = "Pierwsza strona",
        ["Paginator_GoPreviousPage"] = "Poprzednia strona",
        ["Paginator_GoNextPage"] = "Następna strona",
        ["Paginator_GoLastPage"] = "Ostatnia strona",
        ["Paginator_Status"] = "Strona {0} z {1}",
        ["Paginator_SummaryItem"] = "{0} pozycja",
        ["Paginator_SummaryItems"] = "{0} pozycji",
        ["Paginator_SummaryNoItems"] = "Brak pozycji",
        ["DataGrid_SortMenuAscending"] = "Sortuj rosnąco",
        ["DataGrid_SortMenuDescending"] = "Sortuj malejąco",
        ["DataGrid_SortMenu"] = "Sortuj",
        ["DataGrid_SortAscending"] = "Sortuj rosnąco",
        ["DataGrid_SortDescending"] = "Sortuj malejąco",
        ["DataGrid_SortMenuAddToSort"] = "Dodaj do sortowania",
        ["DataGrid_SortMenuClearSort"] = "Usuń z sortowania",
        ["DataGrid_SortMenuResetAllSorts"] = "Przywróć",
        ["DataGrid_SortMenuClearAllSorts"] = "Wyczyść sortowanie",
        ["DataGrid_SortDirectionAscending"] = "rosnąco",
        ["DataGrid_SortDirectionDescending"] = "malejąco",
        ["DataGrid_SortColumnAscending"] = "{0} rosnąco",
        ["DataGrid_SortColumnDescending"] = "{0} malejąco",
        ["DataGrid_SortLevel"] = "Sortowanie {0}, poziom {1} z {2}",
        ["DataGrid_SortAnnouncement"] = "Posortowano według: {0}",
        ["DataGrid_SortAnnouncementSeparator"] = ", potem według: ",
        ["DataGrid_SortAnnouncementCleared"] = "Wyczyszczono sortowanie",
        ["DataGrid_ResizeMenu"] = "Szerokość kolumny",
        ["DataGrid_OptionsMenu"] = "Opcje kolumny",
        ["DataGrid_ReorderMenu"] = "Kolejność kolumn",
        ["DataGrid_ReorderMoveToStart"] = "Przesuń na początek",
        ["DataGrid_ReorderMoveLeft"] = "Przesuń w lewo",
        ["DataGrid_ReorderMoveRight"] = "Przesuń w prawo",
        ["DataGrid_ReorderMoveToEnd"] = "Przesuń na koniec",
        ["DataGrid_ResizeDiscrete"] = "Szerokość kolumny",
        ["DataGrid_ResizeExact"] = "Szerokość kolumny (w pikselach)",
        ["DataGrid_ResizeGrow"] = "Poszerz kolumnę",
        ["DataGrid_ResizeShrink"] = "Zwęź kolumnę",
        ["DataGrid_ResizeReset"] = "Przywróć szerokości kolumn",
        ["DataGrid_ResizeSubmit"] = "Ustaw szerokości kolumn",
        ["DataGrid_SelectColumn_AllChecked"] = "Zaznaczono wszystkie wiersze",
        ["DataGrid_SelectColumn_AllUnchecked"] = "Nie zaznaczono wierszy",
        ["DataGrid_SelectColumn_AllIndeterminate"] = "Zaznaczono część wierszy",
        ["DataGrid_SelectColumn_RowChecked"] = "Wiersz zaznaczony",
        ["DataGrid_SelectColumn_RowUnchecked"] = "Wiersz niezaznaczony",
        ["DataGrid_EmptyContent"] = "Brak danych do wyświetlenia.",
        ["DataGrid_ErrorContent"] = "Nie udało się pobrać danych.",
        ["DataGrid_LoadingContent"] = "Wczytywanie…",
        ["DataGrid_ToggleNesting"] = "Rozwiń lub zwiń",
        ["DataGrid_ToggleRowDetails"] = "Pokaż lub ukryj szczegóły wiersza",
        ["MessageBar_Dismiss"] = "Zamknij komunikat",
        ["DatePicker_ButtonTitle"] = "Otwórz kalendarz",
        ["Nav_ToggleTitle"] = "Zwiń lub rozwiń menu",
        ["AppBar_MoreItems"] = "Więcej aplikacji",
        ["AutoComplete_MaximumSelectedOptionsMessage"] = "Osiągnięto maksymalną liczbę wybranych pozycji: {0}.",
        ["Wizard_LabelButtonPrevious"] = "Wstecz",
        ["Wizard_LabelButtonNext"] = "Dalej",
        ["Wizard_LabelButtonDone"] = "Gotowe",
        ["LabelInfo_InfoActionText"] = "Dowiedz się więcej",
        ["LabelInfo_InfoText"] = "Więcej informacji",
        ["Tabs_MoreItems"] = "Pokaż więcej kart: {0}",
        ["ColorPickerInput_InvalidHexMessage"] = "Niepoprawny kolor szesnastkowy",
        ["ColorPickerInput_OpenPickerTitle"] = "Otwórz wybór koloru",
        ["PresenceStatus_Available"] = "dostępny",
        ["PresenceStatus_Away"] = "nieobecny",
        ["PresenceStatus_Blocked"] = "zablokowany",
        ["PresenceStatus_Busy"] = "zajęty",
        ["PresenceStatus_DoNotDisturb"] = "nie przeszkadzać",
        ["PresenceStatus_Offline"] = "offline",
        ["PresenceStatus_OutOfOffice"] = "poza biurem",
        ["PresenceStatus_Unknown"] = "nieznany",
        ["Fake_Hello"] = "Witaj, {0}",
    };

    /// <summary>Tekst komponentu Fluent dla klucza zasobu, z podstawionymi argumentami.</summary>
    /// <param name="key">Klucz zasobu biblioteki, np. <c>MessageBox_ButtonCancel</c>, <c>Paginator_Status</c>.</param>
    /// <param name="arguments">Wartości do wstawienia w miejsca <c>{0}</c>, <c>{1}</c>… (formatowane w kulturze <see cref="Formats.Culture"/>).</param>
    /// <returns>Tekst polski albo - gdy klucza nie ma w słowniku - wynik <c>IFluentLocalizer.GetDefault</c>.</returns>
    public string this[string key, params object[] arguments]
        => Texts.TryGetValue(key, out var text)
            ? (arguments.Length == 0 ? text : string.Format(Formats.Culture, text, arguments))
            : IFluentLocalizer.GetDefault(key, arguments);
}
