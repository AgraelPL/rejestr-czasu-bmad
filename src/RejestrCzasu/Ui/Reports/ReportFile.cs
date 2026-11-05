namespace RejestrCzasu.Ui;

/// <summary>Plik eksportu raportu zbudowany przez ekran (<see cref="EodReport.ExportFile"/>).</summary>
/// <param name="Extension">Rozszerzenie pliku bez kropki, np. <c>"xlsx"</c> – nazwa: „{FileName}-RRRR-MM-DD.{Extension}”.</param>
/// <param name="ContentType">Typ MIME pliku.</param>
/// <param name="Content">Treść pliku.</param>
/// <remarks>Dopasowanie Rejestru (AD-23 poz. 14, docs/ui/DOPASOWANIA.md).</remarks>
public sealed record ReportFile(string Extension, string ContentType, byte[] Content);
