namespace RejestrCzasu.Shared;

/// <summary>A column of <see cref="ReportGrid{T}"/> (AD-9): its title and the cell text, which is also the value filtered on.</summary>
/// <param name="Title">Polish column title.</param>
/// <param name="Text">Cell text of a row; the value filter lists and compares these texts.</param>
public sealed record ReportColumn<T>(string Title, Func<T, string> Text);
