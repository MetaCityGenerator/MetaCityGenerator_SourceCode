using NetTopologySuite.Features;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UrbanX.IO.GeoJSON.Converters
{
    /// <summary>
    /// Converts IAttributesTable object to its JSON representation.
    /// </summary>
    internal class AttributesTableConverter : JsonConverter<IAttributesTable>
    {
        private static readonly AttributesTable _emptyTable = new AttributesTable();

        //private readonly string _idPropertyName;

        public AttributesTableConverter()
        {
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The calling serializer.</param>
        public override void Write(Utf8JsonWriter writer, IAttributesTable value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            foreach (string propertyName in value.GetNames())
            {

                writer.WritePropertyName(propertyName);
                JsonSerializer.Serialize(writer, value[propertyName], value.GetType(propertyName), options);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="options">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override IAttributesTable Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            using (var doc = JsonDocument.ParseValue(ref reader))
            {
                switch (doc.RootElement.ValueKind)
                {
                    case JsonValueKind.Null:
                        return _emptyTable;

                    case JsonValueKind.Object:
                        return CreateAttributesTable(doc.RootElement);

                    default:
                        throw new JsonException();
                }
            }
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        ///   <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(IAttributesTable).IsAssignableFrom(objectType);
        }



        public AttributesTable CreateAttributesTable(JsonElement rootElement)
        {
            var names = GetNames(rootElement);
            var values = GetValues(rootElement);

            KeyValuePair<string, object>[] attributes = new KeyValuePair<string, object>[names.Length];
            for (int i = 0; i < attributes.Length; i++)
            {
                attributes[i] = new KeyValuePair<string, object>(names[i], values[i]);
            }

            return new AttributesTable(attributes);
        }



        private object GetOptionalValue(string attributeName, JsonElement rootElement)
        {
            return rootElement.TryGetProperty(attributeName, out var prop)
                ? ConvertValue(prop)
                : null;
        }

        public Type GetType(string attributeName, JsonElement rootElement)
        {
            if (!rootElement.TryGetProperty(attributeName, out var prop))
            {
                throw new ArgumentException($"Attribute {attributeName} does not exist!", nameof(attributeName));
            }

            return ConvertValue(prop)?.GetType() ?? typeof(object);
        }

        private string[] GetNames(JsonElement rootElement)
        {
            return rootElement.EnumerateObject()
                              .Select(prop => prop.Name)
                              .ToArray();
        }

        private object[] GetValues(JsonElement rootElement)
        {
            return rootElement.EnumerateObject()
                              .Select(prop => GetOptionalValue(prop.Name, rootElement))
                              .ToArray();
        }



        private object ConvertValue(JsonElement prop)
        {
            switch (prop.ValueKind)
            {
                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                    return null;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.String:
                    return prop.GetString();

                case JsonValueKind.Object:
                    return CreateAttributesTable(prop);

                case JsonValueKind.Array:
                    return prop.EnumerateArray()
                               .Select(ConvertValue)
                               .ToArray();

                case JsonValueKind.Number when prop.TryGetInt32(out int d):
                    return d;

                case JsonValueKind.Number when prop.TryGetDouble(out double d):
                    return d;

                //case JsonValueKind.Number when prop.TryGetDecimal(out decimal d):
                //    return d;

                case JsonValueKind.Number:
                    throw new NotSupportedException("Number value cannot be boxed as a decimal: " + prop.GetRawText());

                default:
                    throw new NotSupportedException("Unrecognized JsonValueKind: " + prop.ValueKind);
            }
        }
    }
}
