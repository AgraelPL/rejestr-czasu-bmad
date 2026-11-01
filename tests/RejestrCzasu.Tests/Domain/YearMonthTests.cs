using RejestrCzasu.Domain;

namespace RejestrCzasu.Tests.Domain;

/// <summary>Miesiąc (YearMonth); no database.</summary>
public sealed class YearMonthTests
{
    [Fact]
    public void February2026_HasItsDaysNeighboursAndName()
    {
        var month = new YearMonth(2026, 2);

        Assert.Equal(new DateOnly(2026, 2, 1), month.FirstDay);
        Assert.Equal(new DateOnly(2026, 2, 28), month.LastDay);
        Assert.Equal(28, month.DaysInMonth);
        Assert.Equal(new YearMonth(2026, 3), month.Next);
        Assert.Equal(new YearMonth(2026, 1), month.Previous);
        Assert.Equal("Luty 2026", month.Name);
    }

    [Fact]
    public void NextAndPrevious_CrossTheYear()
    {
        Assert.Equal(new YearMonth(2027, 1), new YearMonth(2026, 12).Next);
        Assert.Equal(new YearMonth(2025, 12), new YearMonth(2026, 1).Previous);
    }

    [Theory]
    [InlineData(2026, 13)]
    [InlineData(2026, 0)]
    [InlineData(0, 1)]
    public void Constructor_RejectsInvalidMonth(int year, int month)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new YearMonth(year, month));
    }

    [Fact]
    public void FromAndContains()
    {
        var month = YearMonth.From(new DateOnly(2026, 9, 17));

        Assert.Equal(new YearMonth(2026, 9), month);
        Assert.True(month.Contains(new DateOnly(2026, 9, 1)));
        Assert.True(month.Contains(new DateOnly(2026, 9, 30)));
        Assert.False(month.Contains(new DateOnly(2026, 10, 1)));
        Assert.False(month.Contains(new DateOnly(2025, 9, 15)));
        Assert.Equal("Wrzesień 2026", month.Name);
        Assert.Equal("2026-09", month.ToString());
    }

    [Fact]
    public void LeapFebruary_Has29Days()
    {
        Assert.Equal(new DateOnly(2028, 2, 29), new YearMonth(2028, 2).LastDay);
    }

    [Fact]
    public void Ordering_IsChronological()
    {
        var months = new[] { new YearMonth(2027, 1), new YearMonth(2026, 12), new YearMonth(2026, 2) };

        Assert.Equal([new YearMonth(2026, 2), new YearMonth(2026, 12), new YearMonth(2027, 1)], months.Order());
        Assert.True(new YearMonth(2026, 12) < new YearMonth(2027, 1));
        Assert.True(new YearMonth(2026, 3) > new YearMonth(2026, 2));
    }
}
