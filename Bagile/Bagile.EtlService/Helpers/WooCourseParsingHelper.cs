using System;
using System.Globalization;
using System.Text.Json;

namespace Bagile.EtlService.Helpers
{
    public static class WooCourseParsingHelper
    {
        public static DateTime? TryParseStartDateFromName(string name, DateTime? baseDate)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var cleaned = name.Replace("™", "").Trim();

            const string marker = " - ";
            var idx = cleaned.LastIndexOf(marker, StringComparison.Ordinal);
            var segment = idx >= 0 ? cleaned[(idx + marker.Length)..].Trim() : cleaned;

            var tokens = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                return null;

            var dayToken = tokens[0];
            if (dayToken.Contains('−') || dayToken.Contains('-'))
            {
                var parts = dayToken.Split('-', '−');
                dayToken = parts[0];
            }

            if (!int.TryParse(dayToken, out var day) || day <= 0 || day > 31)
                return null;

            string? monthText = FindMonth(tokens);
            if (monthText == null)
                return null;

            if (!DateTime.TryParseExact(monthText, new[] { "MMM", "MMMM" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var monthDate))
            {
                return null;
            }

            int month = monthDate.Month;
            int? year = ExtractYear(tokens);

            var reference = baseDate ?? DateTime.UtcNow;

            if (!year.HasValue)
                year = month < reference.Month ? reference.Year + 1 : reference.Year;

            return new DateTime(year.Value, month, day);
        }

        private static string? FindMonth(string[] tokens)
        {
            foreach (var t in tokens)
            {
                var letters = new string(t.TakeWhile(char.IsLetter).ToArray());
                if (!string.IsNullOrEmpty(letters))
                    return letters;
            }
            return null;
        }

        private static int? ExtractYear(string[] tokens)
        {
            for (var i = tokens.Length - 1; i >= 0; i--)
            {
                var t = tokens[i];
                if (t.All(char.IsDigit) && (t.Length == 2 || t.Length == 4) && int.TryParse(t, out var y))
                {
                    if (t.Length == 2)
                        y += 2000;
                    return y;
                }
            }
            return null;
        }

        public static string ExtractCourseCodeFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "COURSE";

            var beforeDash = name.Split('-')[0];
            var words = beforeDash.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var chars = words.Where(w => char.IsLetter(w[0]) && char.IsUpper(w[0]))
                             .Select(w => w[0])
                             .ToArray();

            return chars.Length >= 2 ? new string(chars) : "COURSE";
        }

        public static string ExtractCourseCodeFromSku(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return "COURSE";

            var parts = sku.Split('-', StringSplitOptions.RemoveEmptyEntries);

            // Always take the bit before the dash
            return parts.Length > 0 ? parts[0] : "COURSE";
        }

        public static long? TryGetProductIdFromLineItem(JsonElement item)
        {
            if (!item.TryGetProperty("product_id", out var pidProp))
                return null;

            // Woo sometimes sends 0, number, or string
            if (pidProp.ValueKind == JsonValueKind.Number &&
                pidProp.TryGetInt64(out var pidVal))
            {
                return pidVal > 0 ? pidVal : null;
            }

            if (pidProp.ValueKind == JsonValueKind.String &&
                long.TryParse(pidProp.GetString(), out var parsed))
            {
                return parsed > 0 ? parsed : null;
            }

            return null;
        }

    }
}
