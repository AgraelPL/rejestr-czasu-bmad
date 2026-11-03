using Microsoft.FluentUI.AspNetCore.Components;

namespace RejestrCzasu.Shared;

/// <summary>The library footer of an application window (AD-11): the Project, Work type and Entry windows.</summary>
internal static class DialogFooter
{
    /// <summary>
    /// Hides the library footer of a window and removes its shortcuts (its primary default is Enter); the window's own
    /// buttons are in its content.
    /// </summary>
    internal static void HideLibraryFooter(DialogOptions options)
    {
        HideLibraryAction(options.Footer.PrimaryAction);
        HideLibraryAction(options.Footer.SecondaryAction);
    }

    private static void HideLibraryAction(DialogOptionsFooterAction action)
    {
        action.Label = null;
        action.Visible = false;
        action.ShortCut = null;
    }
}
