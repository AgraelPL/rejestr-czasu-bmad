using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Shared;

namespace RejestrCzasu.Tests.Shared;

/// <summary>The library footer of the Project, Work type and Entry windows (AD-11): hidden and without shortcuts.</summary>
public sealed class DialogFooterTests
{
    [Fact]
    public void HideLibraryFooter_HidesBothActions_WithoutLabelsAndShortcuts()
    {
        var options = new DialogOptions();

        DialogFooter.HideLibraryFooter(options);

        Assert.All([options.Footer.PrimaryAction, options.Footer.SecondaryAction], action =>
        {
            Assert.False(action.Visible);
            Assert.Null(action.Label);
            Assert.Null(action.ShortCut);
        });
    }
}
