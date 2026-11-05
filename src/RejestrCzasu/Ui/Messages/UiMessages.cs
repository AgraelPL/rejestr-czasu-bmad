using System.Net;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace RejestrCzasu.Ui;

/// <summary>Nazwy sekcji, w których <see cref="FluentMessageBarProvider"/> pokazuje paski komunikatów.</summary>
public static class EodSections
{
    /// <summary>Jedno stałe miejsce komunikatów na stronie (EodPage, EodCaseForm).</summary>
    /// <remarks>Do tej sekcji trafiają wszystkie paski wysyłane przez <see cref="UiMessages"/>.</remarks>
    public const string Page = "eod-page";
}

/// <summary>Rodzaj okna MessageBox – wyznacza ikonę 32 px i jej kolor (specyfikacja UI, sekcja 11).</summary>
public enum MessageBoxKind
{
    /// <summary>Pytanie przed operacją istotną lub nieodwracalną; ikona znaku zapytania w kolorze marki.</summary>
    Confirmation,

    /// <summary>Wynik operacji istotnej zakończonej powodzeniem; zielona ikona potwierdzenia.</summary>
    Success,

    /// <summary>Informacja, o której użytkownik musi wiedzieć; ikona informacji w kolorze marki.</summary>
    Information,

    /// <summary>Ostrzeżenie, np. lista skutków operacji nieodwracalnej; żółta ikona ostrzeżenia.</summary>
    Warning,

    /// <summary>Błąd operacji istotnej albo potwierdzenie operacji niszczącej (np. anulowania sprawy); czerwona ikona.</summary>
    Error,
}

/// <summary>Jedna akcja w pasku komunikatu, np. „Przejdź do pola”, „Spróbuj ponownie”.</summary>
/// <param name="Label">Tekst przycisku – czasownik nazywający czynność.</param>
/// <param name="Execute">Czynność wykonywana po kliknięciu; po jej zakończeniu pasek się zamyka.</param>
public sealed record MessageBarAction(string Label, Func<Task> Execute);

/// <summary>
/// Komunikaty dla użytkownika według zasad wzorca:
/// <list type="bullet">
/// <item>pasek (MessageBar) – operacje mniej istotne i walidacja; sukces i informacja znikają po 7 s, ostrzeżenie i błąd zostają;
/// najwyżej 3 paski naraz; najwyżej jedna akcja;</item>
/// <item>okno (MessageBox) – operacje istotne, ich wyniki i każde potwierdzenie; przyciski nazwane czynnością, główny pierwszy.</item>
/// </list>
/// Paski trafiają do sekcji <see cref="EodSections.Page"/> (EodPage / EodCaseForm). Zmiana strony czyści paski (robi to EodShell).
/// </summary>
/// <remarks>
/// Usługa o zasięgu połączenia (scoped), rejestrowana przez <see cref="ServiceCollectionExtensions.AddEodPlatformUI"/>.
/// Ekrany nie wywołują bezpośrednio <see cref="INotificationService"/> ani <c>ShowMessageBoxAsync</c> biblioteki –
/// tylko tę usługę (specyfikacja UI, sekcja 11). Okna z danymi (np. powód zwrotu) to własne komponenty w <c>Dialogs/</c>.
/// </remarks>
/// <example>
/// <code>
/// @inject UiMessages Messages
/// …
/// await Messages.ShowSuccessAsync("Zapisano zmiany.");
/// if (await Messages.ConfirmAsync("Przekazać sprawę do akceptacji?",
///         "&lt;p&gt;Sprawa &lt;strong&gt;ZR/2026/0041&lt;/strong&gt; trafi do akceptującego.&lt;/p&gt;", "Przekaż"))
/// {
///     …
/// }
/// </code>
/// </example>
/// <param name="notifications">Usługa pasków komunikatów Fluent UI.</param>
/// <param name="dialogs">Usługa okien Fluent UI.</param>
public sealed class UiMessages(INotificationService notifications, IDialogService dialogs)
{
    private static readonly TimeSpan BarLifetime = TimeSpan.FromSeconds(7);
    private const int MaxVisibleBars = 3;
    private readonly List<string> _barIds = [];

    // ── Paski ────────────────────────────────────────────────────────────

    // Każda metoda paska zwraca identyfikator paska (null, gdy strona nie ma miejsca na komunikaty) – do CloseAsync.

    /// <summary>Drobna operacja zakończona powodzeniem („Zapisano zmiany.”). Znika po 7 s (z akcją – zostaje).</summary>
    /// <param name="title">Pogrubiony tytuł – co się stało.</param>
    /// <param name="message">Opcjonalne zdanie – co z tego wynika.</param>
    /// <param name="action">Opcjonalna jedna akcja paska, np. „Pobierz” po wygenerowaniu dokumentu (specyfikacja UI, sekcja 9.12).</param>
    /// <returns>Identyfikator paska do <see cref="CloseAsync"/> albo <c>null</c>, gdy strona nie ma miejsca na komunikaty.</returns>
    public Task<string?> ShowSuccessAsync(string title, string? message = null, MessageBarAction? action = null) => ShowBarAsync(MessageBarIntent.Success, title, message, action);

    /// <summary>Informacja bez decyzji. Znika po 7 s (z akcją – zostaje).</summary>
    /// <param name="title">Pogrubiony tytuł – co się stało.</param>
    /// <param name="message">Opcjonalne zdanie – co z tego wynika.</param>
    /// <param name="action">Opcjonalna jedna akcja paska.</param>
    /// <returns>Identyfikator paska do <see cref="CloseAsync"/> albo <c>null</c>, gdy strona nie ma miejsca na komunikaty.</returns>
    public Task<string?> ShowInfoAsync(string title, string? message = null, MessageBarAction? action = null) => ShowBarAsync(MessageBarIntent.Info, title, message, action);

    /// <summary>Ostrzeżenie bez decyzji. Zostaje do zamknięcia.</summary>
    /// <param name="title">Pogrubiony tytuł – co się stało.</param>
    /// <param name="message">Opcjonalne zdanie – co z tego wynika lub co zrobić.</param>
    /// <param name="action">Opcjonalna jedna akcja paska.</param>
    /// <returns>Identyfikator paska do <see cref="CloseAsync"/> albo <c>null</c>, gdy strona nie ma miejsca na komunikaty.</returns>
    public Task<string?> ShowWarningAsync(string title, string? message = null, MessageBarAction? action = null) => ShowBarAsync(MessageBarIntent.Warning, title, message, action);

    /// <summary>
    /// Błąd (np. walidacji formularza). Zostaje do zamknięcia przez użytkownika albo do usunięcia przyczyny –
    /// wtedy ekran zamyka go przez <see cref="CloseAsync"/> z identyfikatorem zwróconym przez tę metodę.
    /// </summary>
    /// <param name="title">Pogrubiony tytuł – co się stało, np. „Formularz zawiera 1 pole do poprawy.”.</param>
    /// <param name="message">Opcjonalne zdanie – jak poprawić.</param>
    /// <param name="action">Opcjonalna jedna akcja paska, np. „Przejdź do pola”.</param>
    /// <returns>Identyfikator paska do <see cref="CloseAsync"/> albo <c>null</c>, gdy strona nie ma miejsca na komunikaty.</returns>
    /// <remarks>Pasek błędu ma <c>aria-live="assertive"</c> i <c>role="alert"</c> (specyfikacja UI, sekcja 16).</remarks>
    public Task<string?> ShowErrorAsync(string title, string? message = null, MessageBarAction? action = null) => ShowBarAsync(MessageBarIntent.Error, title, message, action);

    /// <summary>Zamyka jeden pasek (np. błąd walidacji po poprawieniu pól). Pusty lub nieaktualny identyfikator jest ignorowany.</summary>
    /// <param name="id">Identyfikator zwrócony przez metodę pokazującą pasek.</param>
    /// <returns>Zadanie zakończone po zamknięciu paska.</returns>
    public async Task CloseAsync(string? id)
    {
        if (id is null || !_barIds.Remove(id))
        {
            return;
        }

        await notifications.CloseAsync(id);
    }

    /// <summary>Zamyka wszystkie paski (wywoływane przy zmianie strony).</summary>
    /// <returns>Zadanie zakończone po zamknięciu pasków.</returns>
    public async Task ClearAsync()
    {
        _barIds.Clear();
        await notifications.CloseAllMessageBarsAsync();
    }

    private async Task<string?> ShowBarAsync(MessageBarIntent intent, string title, string? message, MessageBarAction? action)
    {
        while (_barIds.Count >= MaxVisibleBars)
        {
            var oldest = _barIds[0];
            _barIds.RemoveAt(0);
            await notifications.CloseAsync(oldest);
        }

        var id = "eod-bar-" + Guid.NewGuid().ToString("N");
        var autoDismiss = action is null && intent is MessageBarIntent.Success or MessageBarIntent.Info;
        _barIds.Add(id);

        try
        {
            await notifications.ShowMessageBarAsync<EodMessageBar>(options =>
            {
                options.Id = id;
                options.Section = EodSections.Page;
                options.Intent = intent;
                options.Title = title;
                options.Message = message;
                options.Lifetime = autoDismiss ? BarLifetime : null;
                options.AriaLive = intent == MessageBarIntent.Error ? AriaLive.Assertive : AriaLive.Polite;
                options.Parameters.Add(nameof(EodMessageBar.BarAction), action);
            });
            return id;
        }
        catch (Exception ex) when (ex.GetType().Name.StartsWith("FluentServiceProviderException", StringComparison.Ordinal))
        {
            // Strona bez miejsca na komunikaty (EodPage / EodCaseForm) – komunikat się nie pokaże, ale połączenie nie zostanie zerwane.
            _barIds.Remove(id);
            return null;
        }
    }

    // ── Okna ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Okno potwierdzenia przed operacją istotną lub nieodwracalną. Tytuł jest pytaniem, treść podaje skutek, numer sprawy i odbiorców.
    /// </summary>
    /// <param name="title">Pytanie, np. „Przekazać sprawę do akceptacji?”. Zwykły tekst – metoda sama go koduje (<c>HtmlEncode</c>), ekran nie koduje go drugi raz.</param>
    /// <param name="messageHtml">Treść (HTML: p, strong, ul, li).</param>
    /// <param name="primaryButton">Czynność, np. „Przekaż”, „Usuń”. Nigdy „Tak”.</param>
    /// <param name="secondaryButton">Zwykle „Anuluj” (przy „Anuluj sprawę” – „Wróć”).</param>
    /// <param name="kind">
    /// <see cref="MessageBoxKind.Confirmation"/> (domyślnie), <see cref="MessageBoxKind.Warning"/> (lista skutków operacji
    /// nieodwracalnej) albo <see cref="MessageBoxKind.Error"/> (np. anulowanie sprawy).
    /// </param>
    /// <returns><c>true</c>, gdy użytkownik wybrał przycisk główny.</returns>
    /// <remarks>
    /// Okno jest modalne (<see cref="EodConfirmDialog"/>, Rejestr – AD-23 poz. 6): Enter nie zatwierdza (przyciski bez skrótów),
    /// fokus ma drugi przycisk, Escape działa jak drugi przycisk. Każde potwierdzenie w aplikacji używa tej metody.
    /// </remarks>
    public async Task<bool> ConfirmAsync(string title, string messageHtml, string primaryButton, string secondaryButton = "Anuluj", MessageBoxKind kind = MessageBoxKind.Confirmation)
    {
        var result = await dialogs.ShowDialogAsync<EodConfirmDialog>(options =>
        {
            options.Modal = true;
            // Biblioteka renderuje tytuł jako HTML; tytuł jest zwykłym tekstem.
            options.Header.Title = WebUtility.HtmlEncode(title);
            // Stopka biblioteki ukryta i bez skrótów (domyślny skrót przycisku głównego to Enter); przyciski są w EodConfirmDialog.
            // Escape zamyka natywne okno, co biblioteka zgłasza jako Cancel.
            HideLibraryAction(options.Footer.PrimaryAction);
            HideLibraryAction(options.Footer.SecondaryAction);
            options.Parameters.Add(nameof(EodConfirmDialog.Message), messageHtml);
            options.Parameters.Add(nameof(EodConfirmDialog.PrimaryButton), primaryButton);
            options.Parameters.Add(nameof(EodConfirmDialog.SecondaryButton), secondaryButton);
            options.Parameters.Add(nameof(EodConfirmDialog.Kind), kind);
        });
        return !result.Cancelled;
    }

    /// <summary>Okno wyniku operacji istotnej (sukces, błąd) albo informacji, o której użytkownik musi wiedzieć.</summary>
    /// <param name="kind">Rodzaj okna – zwykle <see cref="MessageBoxKind.Success"/>, <see cref="MessageBoxKind.Error"/> albo <see cref="MessageBoxKind.Information"/>.</param>
    /// <param name="title">Wynik, np. „Ogłoszenie opublikowane”. Jak we wzorcu biblioteka renderuje go jako HTML – wartości użytkownika koduje ekran.</param>
    /// <param name="messageHtml">Treść (HTML: p, strong, ul, li) – skutek, numer sprawy, odbiorcy.</param>
    /// <param name="button">Jedyny przycisk okna; domyślnie „Zamknij”.</param>
    /// <returns>Zadanie zakończone po zamknięciu okna.</returns>
    public async Task ShowMessageBoxAsync(MessageBoxKind kind, string title, string messageHtml, string button = "Zamknij")
        => await dialogs.ShowMessageBoxAsync(CreateOptions(kind, title, messageHtml, button, null));

    /// <summary>Ikona 32 px i jej kolor dla rodzaju okna – wspólne dla okien wyniku i <see cref="EodConfirmDialog"/>.</summary>
    internal static (Icon Icon, Color Color) KindIcon(MessageBoxKind kind) => kind switch
    {
        MessageBoxKind.Success => (new Icons.Filled.Size32.CheckmarkCircle(), Color.Success),
        MessageBoxKind.Warning => (new Icons.Filled.Size32.Warning(), Color.Warning),
        MessageBoxKind.Error => (new Icons.Filled.Size32.DismissCircle(), Color.Error),
        MessageBoxKind.Information => (new Icons.Filled.Size32.Info(), Color.Primary),
        _ => (new Icons.Filled.Size32.QuestionCircle(), Color.Primary),
    };

    private static void HideLibraryAction(DialogOptionsFooterAction action)
    {
        action.Label = null;
        action.Visible = false;
        action.ShortCut = null;
    }

    private static MessageBoxOptions CreateOptions(MessageBoxKind kind, string title, string messageHtml, string primaryButton, string? secondaryButton)
    {
        var (icon, color) = KindIcon(kind);

        return new MessageBoxOptions
        {
            Title = title,
            Message = messageHtml,
            PrimaryButton = primaryButton,
            SecondaryButton = secondaryButton,
            PrimaryShortCut = "Enter",
            SecondaryShortCut = secondaryButton is null ? null : "Escape",
            Icon = icon,
            IconColor = color,
        };
    }
}
