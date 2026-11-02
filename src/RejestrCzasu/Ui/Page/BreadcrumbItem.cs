namespace RejestrCzasu.Ui;

/// <summary>
/// Element ścieżki (breadcrumb) nad tytułem strony lub sprawy: proces › skąd przyszedł użytkownik › bieżąca strona.
/// </summary>
/// <remarks>
/// Ostatni element to bieżąca strona – podaje się go bez adresu; <see cref="EodBreadcrumb"/> zawsze wyświetla go jako tekst
/// z <c>aria-current="page"</c>. Wcześniejsze elementy z adresem są linkami, bez adresu – zwykłym tekstem
/// (specyfikacja UI, sekcje 7 i 9.3).
/// </remarks>
/// <param name="Text">Tekst elementu, np. „Moje zadania”, nazwa procesu albo numer sprawy.</param>
/// <param name="Href">Adres, do którego prowadzi element; <see langword="null"/> dla bieżącej strony.</param>
/// <example>
/// <code>
/// IReadOnlyList&lt;BreadcrumbItem&gt; breadcrumb =
/// [
///     new("Zlecenie rekrutacji", "/proces/rekrutacja"),
///     new("Moje zadania", "/zadania"),
///     new("ZR/2026/0041"),
/// ];
/// </code>
/// </example>
public sealed record BreadcrumbItem(string Text, string? Href = null);
