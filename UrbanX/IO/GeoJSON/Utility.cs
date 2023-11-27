﻿using System.Runtime.CompilerServices;
using System.Text.Json;

using MetaCity.IO.Properties;

namespace MetaCity.IO.GeoJSON
{
    public static class Utility
    {
        public static void SkipComments(this ref Utf8JsonReader reader)
        {
            // Skip comments
            while (reader.TokenType == JsonTokenType.Comment)
            {
                if (!reader.Read())
                {
                    break;
                }
            }
        }


        public static bool ReadToken(this ref Utf8JsonReader reader, JsonTokenType tokenType, bool throwException = true)
        {
            if (reader.TokenType != tokenType)
            {
                if (throwException)
                    ThrowForUnexpectedToken(tokenType, ref reader);
                return false;
            }
            return reader.Read();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadOrThrow(this ref Utf8JsonReader reader)
        {
            if (!reader.Read())
            {
                ThrowForUnexpectedEndOfStream();
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertToken(this ref Utf8JsonReader reader, JsonTokenType requiredCurrentTokenType)
        {
            if (reader.TokenType != requiredCurrentTokenType)
            {
                ThrowForUnexpectedToken(requiredCurrentTokenType, ref reader);
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowForUnexpectedEndOfStream()
            => throw new JsonException(Resources.EX_UnexpectedEndOfStream);


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowForUnexpectedToken(JsonTokenType requiredNextTokenType, ref Utf8JsonReader reader)
            => throw new JsonException(string.Format(Resources.EX_UnexpectedToken, requiredNextTokenType, reader.TokenType, reader.GetString()));
    }
}
