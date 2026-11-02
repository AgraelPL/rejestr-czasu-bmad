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
    [InlineData("1h 30m", 90)]
    [InlineData("1h30m", 90)]
    [InlineData("1H 30M", 90)]
    [InlineData(" 1 h  30 m ", 90)]
    [InlineData("2h", 120)]
    [InlineData("0h", 0)]
    [InlineData("45m", 45)]
    [InlineData("90m", 90)]
    [InlineData("1440m", 1440)]
    [InlineData("1,5h", 90)]
    [InlineData("1.5h", 90)]
    [InlineData("0,25h", 15)]
    [InlineData("1.75H", 105)]
    [InlineData("24h", 1440)]
    [InlineData("30h 30m", 1830)]
    [InlineData("1h 0m", 60)]
    [InlineData("1h 59m", 119)]
    public void TryParse_AcceptsJiraUnitsAndDecimalHours(string text, int expected)
    {
        Assert.True(Czas.TryParse(text, out var minutes));
        Assert.Equal(expected, minutes);
    }

    [Theory]
    [InlineData("0:15")]
    [InlineData("24:00")]
    [InlineData("15m")]
    [InlineData("24h")]
    public void EntryOnTheLimits_IsValid(string text)
    {
        Assert.True(Czas.TryParse(text, out var minutes));
        Assert.True(Czas.IsValidEntry(minutes));
    }

    [Theory]
    [InlineData("0:10", 10)]
    [InlineData("24:15", 1455)]
    [InlineData("0:00", 0)]
    [InlineData("10m", 10)]
    [InlineData("24h 15m", 1455)]
    [InlineData("1h 10m", 70)]
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
    [InlineData("1,5")]
    [InlineData("1.5")]
    [InlineData("90")]
    [InlineData("1h 75m")]
    [InlineData("1h 60m")]
    [InlineData("1h30")]
    [InlineData("h")]
    [InlineData("m")]
    [InlineData("1d")]
    [InlineData("1w")]
    [InlineData("1h 1d")]
    [InlineData("30m 1h")]
    [InlineData("1,01h")]
    [InlineData("1,5h 30m")]
    [InlineData("-1h")]
    [InlineData("+1h")]
    [InlineData("1hh")]
    [InlineData("1h 30m 5m")]
    [InlineData("١h")]
    [InlineData("1,h")]
    [InlineData(",5h")]
    [InlineData("1 5h")]
    [InlineData("4 5m")]
    [InlineData("1 0h")]
    [InlineData("1h 3 0m")]
    [InlineData("1, 5h")]
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
    [InlineData(90, "1h 30m")]
    [InlineData(120, "2h")]
    [InlineData(45, "45m")]
    [InlineData(1830, "30h 30m")]
    [InlineData(0, "0h")]
    [InlineData(5, "5m")]
    [InlineData(1440, "24h")]
    [InlineData(61, "1h 1m")]
    public void Format_WritesLikeJira(int minutes, string expected)
    {
        Assert.Equal(expected, Czas.Format(minutes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(90)]
    [InlineData(1830)]
    public void Format_ThenTryParse_GivesTheSameMinutes(int minutes)
    {
        Assert.True(Czas.TryParse(Czas.Format(minutes), out var parsed));
        Assert.Equal(minutes, parsed);
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
