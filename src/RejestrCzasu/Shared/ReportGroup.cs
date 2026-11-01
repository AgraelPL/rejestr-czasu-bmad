namespace RejestrCzasu.Shared;

/// <summary>A fixed grouping level of <see cref="ReportGrid{T}"/> (AD-9), set by the screen; the grid never computes sums.</summary>
/// <param name="Key">Group key of a row; groups follow the first appearance of their key in the grid's items.</param>
/// <param name="Title">Header text of a group, from its key.</param>
/// <param name="Sum">Optional sum text of a group, from its key, given by the screen (e.g. from <c>MonthReport</c>); the filter never changes it.</param>
public sealed record ReportGroup<T>(Func<T, string> Key, Func<string, string> Title, Func<string, string>? Sum = null);
