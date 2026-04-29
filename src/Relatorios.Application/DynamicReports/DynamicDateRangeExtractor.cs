using System.Globalization;
using System.Text.RegularExpressions;

namespace Relatorios.Application.DynamicReports;

public sealed class DynamicDateRangeExtractor
{
    public DynamicDateRange? Extract(string prompt)
    {     
        
        var text = prompt.Trim().ToLowerInvariant();

        var weekdayRange = ExtractLastWeekday(text);

        if (weekdayRange is not null)
        {
            return weekdayRange;
        }

        if (text.Contains("hoje"))
        {
            var today = DateTime.Today;

            return new DynamicDateRange
            {
                Start = today,
                EndExclusive = today.AddDays(1)
            };
        }

        if (text.Contains("essa semana") || text.Contains("esta semana") || text.Contains("semana atual"))
        {
            var today = DateTime.Today;
            var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var start = today.AddDays(-diff);

            return new DynamicDateRange
            {
                Start = start,
                EndExclusive = start.AddDays(7)
            };
        }

        if (text.Contains("esse mês") || text.Contains("este mês") || text.Contains("mes atual") || text.Contains("mês atual"))
        {
            var today = DateTime.Today;
            var start = new DateTime(today.Year, today.Month, 1);

            return new DynamicDateRange
            {
                Start = start,
                EndExclusive = start.AddMonths(1)
            };
        }

        var relativeRange = ExtractRelativeRange(text);
        if (relativeRange is not null)
        {
            return relativeRange;
        }

        var explicitRange = ExtractExplicitRange(text);
        if (explicitRange is not null)
        {
            return explicitRange;
        }

        var monthRange = ExtractMonth(text);
        if (monthRange is not null)
        {
            return monthRange;
        }

        return null;
    }

    private static DynamicDateRange? ExtractRelativeRange(string text)
    {
        var patterns = new[]
        {
        @"(?:faz|há|ha)\s+(\d+)\s+dias?",
        @"(?:últimos|ultimos|últimas|ultimas)\s+(\d+)\s+dias?",
        @"(?:faz|há|ha)\s+(\d+)\s+meses?",
        @"(?:últimos|ultimos|últimas|ultimas)\s+(\d+)\s+meses?"
    };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern);

            if (!match.Success)
            {
                continue;
            }

            var quantity = int.Parse(match.Groups[1].Value);
            var today = DateTime.Today;

            if (pattern.Contains("dias?"))
            {
                return new DynamicDateRange
                {
                    Start = today.AddDays(-quantity),
                    EndExclusive = today.AddDays(1)
                };
            }

            if (pattern.Contains("meses?"))
            {
                return new DynamicDateRange
                {
                    Start = today.AddMonths(-quantity),
                    EndExclusive = today.AddDays(1)
                };
            }
        }

        return null;
    }

    private static DynamicDateRange? ExtractExplicitRange(string text)
    {
        var pattern = @"(\d{1,2})\/(\d{1,2})(?:\/(\d{4}))?\s*(?:até|a|ate|-)\s*(\d{1,2})\/(\d{1,2})(?:\/(\d{4}))?";
        var match = Regex.Match(text, pattern);

        if (!match.Success)
        {
            return null;
        }

        var currentYear = DateTime.Today.Year;

        var startDay = int.Parse(match.Groups[1].Value);
        var startMonth = int.Parse(match.Groups[2].Value);
        var startYear = match.Groups[3].Success
            ? int.Parse(match.Groups[3].Value)
            : currentYear;

        var endDay = int.Parse(match.Groups[4].Value);
        var endMonth = int.Parse(match.Groups[5].Value);
        var endYear = match.Groups[6].Success
            ? int.Parse(match.Groups[6].Value)
            : currentYear;

        var start = new DateTime(startYear, startMonth, startDay);
        var endExclusive = new DateTime(endYear, endMonth, endDay).AddDays(1);

        return new DynamicDateRange
        {
            Start = start,
            EndExclusive = endExclusive
        };
    }

    private static DynamicDateRange? ExtractMonth(string text)
    {
        var months = new Dictionary<string, int>
        {
            ["janeiro"] = 1,
            ["fevereiro"] = 2,
            ["março"] = 3,
            ["marco"] = 3,
            ["abril"] = 4,
            ["maio"] = 5,
            ["junho"] = 6,
            ["julho"] = 7,
            ["agosto"] = 8,
            ["setembro"] = 9,
            ["outubro"] = 10,
            ["novembro"] = 11,
            ["dezembro"] = 12
        };

        foreach (var month in months)
        {
            if (!Regex.IsMatch(text, $@"\b{Regex.Escape(month.Key)}\b"))
            {
                continue;
            }

            var year = ExtractYear(text) ?? DateTime.Today.Year;
            var start = new DateTime(year, month.Value, 1);

            return new DynamicDateRange
            {
                Start = start,
                EndExclusive = start.AddMonths(1)
            };
        }

        return null;
    }

    private static DynamicDateRange? ExtractLastWeekday(string text)
    {
        var weekdays = new Dictionary<string, DayOfWeek>
        {
            ["segunda"] = DayOfWeek.Monday,
            ["segunda-feira"] = DayOfWeek.Monday,
            ["terça"] = DayOfWeek.Tuesday,
            ["terca"] = DayOfWeek.Tuesday,
            ["terça-feira"] = DayOfWeek.Tuesday,
            ["terca-feira"] = DayOfWeek.Tuesday,
            ["quarta"] = DayOfWeek.Wednesday,
            ["quarta-feira"] = DayOfWeek.Wednesday,
            ["quinta"] = DayOfWeek.Thursday,
            ["quinta-feira"] = DayOfWeek.Thursday,
            ["sexta"] = DayOfWeek.Friday,
            ["sexta-feira"] = DayOfWeek.Friday,
            ["sábado"] = DayOfWeek.Saturday,
            ["sabado"] = DayOfWeek.Saturday,
            ["domingo"] = DayOfWeek.Sunday
        };

        foreach (var weekday in weekdays)
        {
            if (!Regex.IsMatch(text, $@"\b{Regex.Escape(weekday.Key)}\b"))
            {
                continue;
            }

            var today = DateTime.Today;
            var diff = (7 + (today.DayOfWeek - weekday.Value)) % 7;

            var start = today.AddDays(-diff);

            return new DynamicDateRange
            {
                Start = start,
                EndExclusive = start.AddDays(1)
            };
        }

        return null;
    }

    private static int? ExtractYear(string text)
    {
        var match = Regex.Match(text, @"\b(20\d{2})\b");

        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var year)
            ? year
            : null;
    }
}

public sealed class DynamicDateRange
{
    public DateTime Start { get; set; }
    public DateTime EndExclusive { get; set; }
}