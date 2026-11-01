using RejestrCzasu.Domain;

namespace RejestrCzasu.Tests.Domain;

/// <summary>Czas conversions (AD-6, AD-8); no database.</summary>
public sealed class CzasTests
{
    [Theory]
    [InlineData("0:15", 15)]
    [InlineData("24:00", 1440)]
    [InlineData("1:30", 90)]
    [InlineData("01:30", 90)]
    [InlineData("  7:45 ", 465)]
    [InlineData("30:30", 1830)]
    public void TryParse_AcceptsHoursAndTwoDigitMinutes(string text, int expected)
    {
        Assert.True(Czas.TryParse(text, out var minutes));
        Assert.Equal(expected, minutes);
    }

    [Theory]
    [InlineData("0:15")]
    [InlineData("24:00")]
    public void EntryOnTheLimits_IsValid(string text)
    {
        Assert.True(Czas.TryParse(text, out var minutes));
        Assert.True(Czas.IsValidEntry(minutes));
    }

    [Theory]
    [InlineData("0:10", 10)]
    [InlineData("24:15", 1455)]
    [InlineData("0:00", 0)]
    public void EntryOutsideTheRule_ParsesButIsNotValid(string text, int expected)
    {
        Assert.True(Czas.TryParse(text, out var minutes));
        Assert.Equal(expected, minutes);
        Assert.False(Czas.IsValidEntry(minutes));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1:5")]
    [InlineData("1:60")]
    [InlineData("-1:00")]
    [InlineData("1:30:00")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(":30")]
    [InlineData("1:")]
    [InlineData("1 :30")]
    [InlineData("1:3a")]
    [InlineData("+1:30")]
    [InlineData("١:٣٠")]
    [InlineData(null)]
    public void TryParse_RejectsMalformedText(string? text)
    {
        Assert.False(Czas.TryParse(text, out _));
    }

    [Fact]
    public void IsValidEntry_RequiresMultipleOf15()
    {
        Assert.Equal(15, Czas.MinEntryMinutes);
        Assert.Equal(1440, Czas.MaxEntryMinutes);
        Assert.Equal(15, Czas.EntryStepMinutes);
        Assert.True(Czas.IsValidEntry(30));
        Assert.False(Czas.IsValidEntry(20));
        Assert.False(Czas.IsValidEntry(-15));
    }

    [Theory]
    [InlineData(1830, "30:30")]
    [InlineData(0, "0:00")]
    [InlineData(5, "0:05")]
    [InlineData(1440, "24:00")]
    public void Format_WritesHoursAndMinutes(int minutes, string expected)
    {
        Assert.Equal(expected, Czas.Format(minutes));
    }

    [Fact]
    public void Format_RejectsNegativeMinutes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Czas.Format(-1));
    }

    [Theory]
    [InlineData(90, "1.5")]
    [InlineData(45, "0.75")]
    [InlineData(240, "4")]
    public void ToDecimalHours_ConvertsMinutes(int minutes, string expected)
    {
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), Czas.ToDecimalHours(minutes));
    }
}
