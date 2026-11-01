using Microsoft.FluentUI.AspNetCore.Components;

namespace RejestrCzasu.Shared;

/// <summary>A row action of <see cref="ReportGrid{T}"/> (AD-9): an icon button calling the screen with the row.</summary>
/// <param name="Icon">Fluent icon from <c>Microsoft.FluentUI.AspNetCore.Components.Icons</c> (inline SVG, AD-12).</param>
/// <param name="Label">Polish label, rendered as the button's <c>title</c> and <c>aria-label</c>.</param>
/// <param name="Invoke">The screen's action, called with the row whose icon was clicked.</param>
public sealed record ReportAction<T>(Icon Icon, string Label, Func<T, Task> Invoke)
{
    /// <summary>Optional condition; when it returns false for a row, the row has no icon for this action.</summary>
    public Func<T, bool>? Visible { get; init; }
}
