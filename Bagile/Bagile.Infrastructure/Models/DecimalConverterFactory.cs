using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace Bagile.Infrastructure.Models
{
    // Fully replaces the built-in converters for decimal and decimal?
    public class DecimalConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == typeof(decimal) || typeToConvert == typeof(decimal?);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(decimal))
                return new FlexibleDecimalConverter();
            return new FlexibleNullableDecimalConverter();
        }

        private class FlexibleDecimalConverter : JsonConverter<decimal>
        {
            public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.TokenType switch
                {
                    JsonTokenType.Number => reader.GetDecimal(),
                    JsonTokenType.String => TryParse(reader.GetString()),
                    _ => 0m
                };

                static decimal TryParse(string? s)
                {
                    if (string.IsNullOrWhiteSpace(s)) return 0m;
                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                        return val;
                    return 0m;
                }
            }

            public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
                => writer.WriteNumberValue(value);
        }

        private class FlexibleNullableDecimalConverter : JsonConverter<decimal?>
        {
            public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.TokenType switch
                {
                    JsonTokenType.Number => reader.GetDecimal(),
                    JsonTokenType.String => TryParse(reader.GetString()),
                    JsonTokenType.Null => null,
                    _ => null
                };

                static decimal? TryParse(string? s)
                {
                    if (string.IsNullOrWhiteSpace(s)) return null;
                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                        return val;
                    return null;
                }
            }

            public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
            {
                if (value.HasValue)
                    writer.WriteNumberValue(value.Value);
                else
                    writer.WriteNullValue();
            }
        }
    }
}
