using System.Globalization;

namespace RejestrCzasu.Domain;

/// <summary>
/// Conversions of Czas, which is always an <see cref="int"/> number of minutes (AD-6, AD-8).
/// The only place that parses and formats <c>h:mm</c>.
/// </summary>
public static class Czas
{
    /// <summary>Shortest Czas of a Wpis, in minutes.</summary>
    public const int MinEntryMinutes = 15;

    /// <summary>Longest Czas of a Wpis (24 h), in minutes.</summary>
    public const int MaxEntryMinutes = 1440;

    /// <summary>Czas of a Wpis is a multiple of this many minutes.</summary>
    public const int EntryStepMinutes = 15;

    private const int MaxHourDigits = 5;

    /// <summary>
    /// Parses <c>h:mm</c>: one or more hour digits (leading zeros not required), exactly two minute digits 00–59.
    /// Surrounding white space is ignored. Does not check the Wpis rule; see <see cref="IsValidEntry"/>.
    /// </summary>
    public static bool TryParse(string? text, out int minutes)
    {
        minutes = 0;
        if (text is null)
        {
            return false;
        }

        var span = text.AsSpan().Trim();
        var colon = span.IndexOf(':');
        if (colon < 1 || colon > MaxHourDigits || span.Length - colon - 1 != 2)
        {
            return false;
        }

        var hoursPart = span[..colon];
        var minutesPart = span[(colon + 1)..];
        if (!IsAsciiDigits(hoursPart) || !IsAsciiDigits(minutesPart))
        {
            return false;
        }

        var hours = int.Parse(hoursPart, NumberStyles.None, CultureInfo.InvariantCulture);
        var mins = int.Parse(minutesPart, NumberStyles.None, CultureInfo.InvariantCulture);
        if (mins > 59)
        {
            return false;
        }

        minutes = hours * 60 + mins;
        return true;
    }

    /// <summary>Formats minutes as <c>h:mm</c>, also sums over 24 h (1830 → <c>30:30</c>).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Minutes are negative.</exception>
    public static string Format(int minutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minutes);
        return string.Create(CultureInfo.InvariantCulture, $"{minutes / 60}:{minutes % 60:00}");
    }

    /// <summary>Wpis rule (AD-8): from 15 to 1440 minutes, a multiple of 15.</summary>
    public static bool IsValidEntry(int minutes) =>
        minutes is >= MinEntryMinutes and <= MaxEntryMinutes && minutes % EntryStepMinutes == 0;

    /// <summary>Decimal hours (90 → 1.5; 45 → 0.75), as in the Zestawienie.</summary>
    public static decimal ToDecimalHours(int minutes) => minutes / 60m;

    private static bool IsAsciiDigits(ReadOnlySpan<char> span)
    {
        foreach (var c in span)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
