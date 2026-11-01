using System.Globalization;
using System.Text.RegularExpressions;

namespace RejestrCzasu.Domain;

/// <summary>
/// Conversions of Czas, which is always an <see cref="int"/> number of minutes (AD-6, AD-8).
/// The only place that parses and formats Czas text: entered and shown like Jira (<c>1h 30m</c>, <c>2h</c>,
/// <c>45m</c>), entry also as decimal hours (<c>1,5h</c>) and the older <c>h:mm</c> (change 01.11.2026, Author).
/// </summary>
public static partial class Czas
{
    /// <summary>Shortest Czas of a Wpis, in minutes.</summary>
    public const int MinEntryMinutes = 15;

    /// <summary>Longest Czas of a Wpis (24 h), in minutes.</summary>
    public const int MaxEntryMinutes = 1440;

    /// <summary>Czas of a Wpis is a multiple of this many minutes.</summary>
    public const int EntryStepMinutes = 15;

    private const int MaxHourDigits = 5;

    /// <summary>
    /// Parses Czas text. Accepted (case-insensitive; in the unit forms spaces are allowed between a number and its
    /// unit and between hours and minutes, never inside a number):
    /// <list type="bullet">
    /// <item>hours and minutes <c>1h 30m</c>, <c>1h30m</c> (minutes after hours 0–59);</item>
    /// <item>hours only <c>2h</c>, minutes only <c>45m</c>, <c>90m</c> (minutes only may exceed 59);</item>
    /// <item>decimal hours <c>1,5h</c> or <c>1.5h</c>, which must give whole minutes;</item>
    /// <item><c>h:mm</c>: one or more hour digits, exactly two minute digits 00–59.</item>
    /// </list>
    /// Only ASCII digits and the units <c>h</c> and <c>m</c>. Surrounding white space is ignored. Does not check the
    /// Wpis rule; see <see cref="IsValidEntry"/>.
    /// </summary>
    public static bool TryParse(string? text, out int minutes)
    {
        minutes = 0;
        if (text is null)
        {
            return false;
        }

        var trimmed = text.Trim();
        return trimmed.Contains(':')
            ? TryParseColon(trimmed, out minutes)
            : TryParseUnits(trimmed, out minutes);
    }

    /// <summary>
    /// Formats minutes like Jira, also sums over 24 h: 90 → <c>1h 30m</c>, 120 → <c>2h</c>, 45 → <c>45m</c>,
    /// 1830 → <c>30h 30m</c>, 0 → <c>0h</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Minutes are negative.</exception>
    public static string Format(int minutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minutes);
        var hours = minutes / 60;
        var rest = minutes % 60;
        return (hours, rest) switch
        {
            (_, 0) => string.Create(CultureInfo.InvariantCulture, $"{hours}h"),
            (0, _) => string.Create(CultureInfo.InvariantCulture, $"{rest}m"),
            _ => string.Create(CultureInfo.InvariantCulture, $"{hours}h {rest}m"),
        };
    }

    /// <summary>Wpis rule (AD-8): from 15 to 1440 minutes, a multiple of 15.</summary>
    public static bool IsValidEntry(int minutes) =>
        minutes is >= MinEntryMinutes and <= MaxEntryMinutes && minutes % EntryStepMinutes == 0;

    /// <summary>Decimal hours (90 → 1.5; 45 → 0.75), as in the Zestawienie.</summary>
    public static decimal ToDecimalHours(int minutes) => minutes / 60m;

    private static bool TryParseColon(string text, out int minutes)
    {
        minutes = 0;
        var span = text.AsSpan();
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

    private static bool TryParseUnits(string text, out int minutes)
    {
        minutes = 0;
        var match = UnitsPattern().Match(text);
        if (!match.Success)
        {
            return false;
        }

        var hours = match.Groups["hours"];
        var fraction = match.Groups["fraction"];
        var mins = match.Groups["minutes"];
        if (!hours.Success && !mins.Success)
        {
            return false;
        }

        if (fraction.Success)
        {
            // Decimal hours stand alone ("1,5h"), and must give whole minutes.
            if (mins.Success)
            {
                return false;
            }
            var value = decimal.Parse($"{hours.Value}.{fraction.Value}", NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture) * 60m;
            if (value != decimal.Truncate(value))
            {
                return false;
            }
            minutes = (int)value;
            return true;
        }

        var total = 0;
        if (hours.Success)
        {
            total = int.Parse(hours.Value, NumberStyles.None, CultureInfo.InvariantCulture) * 60;
        }
        if (mins.Success)
        {
            var minuteValue = int.Parse(mins.Value, NumberStyles.None, CultureInfo.InvariantCulture);
            // Minutes after hours are 0–59; minutes alone ("90m") may be more.
            if (hours.Success && minuteValue > 59)
            {
                return false;
            }
            total += minuteValue;
        }

        minutes = total;
        return true;
    }

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

    // White space only between a number and its unit and between the hours and the minutes, never inside a number.
    [GeneratedRegex(@"^(?:(?<hours>[0-9]{1,5})(?:[.,](?<fraction>[0-9]{1,4}))?\s*h)?\s*(?:(?<minutes>[0-9]{1,5})\s*m)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnitsPattern();
}
