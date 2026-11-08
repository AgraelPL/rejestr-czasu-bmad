using RejestrCzasu.Domain;
using RejestrCzasu.Features.Zestawienie;

namespace RejestrCzasu.Tests.Features;

/// <summary>The file name of the Zestawienie (R5) and the address of its endpoint (AD-14).</summary>
public sealed class SummaryFileTests
{
    [Theory]
    [InlineData(2026, 9, null, "Zestawienie-wrzesień_2026.xlsx")]
    [InlineData(2026, 10, null, "Zestawienie-październik_2026.xlsx")]
    [InlineData(2027, 2, null, "Zestawienie-luty_2027.xlsx")]
    [InlineData(2026, 9, "", "Zestawienie-wrzesień_2026.xlsx")]
    [InlineData(2026, 9, "ABC", "ABC_Zestawienie-wrzesień_2026.xlsx")]
    public void Name(int year, int month, string? prefix, string expected) =>
        Assert.Equal(expected, SummaryFile.Name(new YearMonth(year, month), prefix));

    [Fact]
    public void Path_WithoutLeadingZeros() =>
        Assert.Equal("/zestawienie/2026/9", SummaryEndpoint.Path(new YearMonth(2026, 9)));
}
