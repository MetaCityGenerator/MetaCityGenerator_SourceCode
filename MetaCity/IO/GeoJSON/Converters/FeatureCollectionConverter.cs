using NetTopologySuite.Features;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetaCity.IO.GeoJSON.Converters
{
    /// <summary>
    /// Converts FeatureCollection objects to its JSON representation.
    /// </summary>
    public class FeatureCollectionConverter : JsonConverter<FeatureCollection>
    {
        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="options">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override FeatureCollection Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            reader.AssertToken(JsonTokenType.StartObject);
            reader.ReadOrThrow();

            var fc = new FeatureCollection();
            while (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (reader.ValueTextEquals("type"))
                {
                    reader.ReadOrThrow();
                    reader.AssertToken(JsonTokenType.String);
                    if (!reader.ValueTextEquals(nameof(GeoJsonObjectType.FeatureCollection)))
                    {
                        throw new JsonException("must be FeatureCollection");
                    }

                    reader.ReadOrThrow();
                }
                else if (reader.ValueTextEquals("features"))
                {
                    reader.ReadOrThrow();
                    reader.AssertToken(JsonTokenType.StartArray);
                    reader.ReadOrThrow();
                    while (reader.TokenType != JsonTokenType.EndArray)
                    {
                        reader.AssertToken(JsonTokenType.StartObject);
                        fc.Add(JsonSerializer.Deserialize<IFeature>(ref reader, options));

                        reader.AssertToken(JsonTokenType.EndObject);
                        reader.ReadOrThrow();
                    }

                    reader.ReadOrThrow();
                }
                else
                {
                    reader.ReadOrThrow();
                    reader.Skip();
                    reader.ReadOrThrow();
                }
            }

            return fc;
        }


        public override void Write(Utf8JsonWriter writer, FeatureCollection value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            
            // "type": "FeatureCollection"
            writer.WriteString("type", nameof(GeoJsonObjectType.FeatureCollection));

            // "bbox": null
            GeometryConverter.WriteBBox(writer, value.BoundingBox, options, null);

            // "crs" .Only support ESPG authority.
            CrsValue crsValue = new CrsValue("name", new CrsValueProp(value[0].Geometry.SRID));
            writer.WritePropertyName("crs");
            JsonSerializer.Serialize(writer, crsValue, options);


            writer.WriteStartArray("features");
            foreach (var feature in value)
                JsonSerializer.Serialize(writer, feature, options);
            writer.WriteEndArray();

            writer.WriteEndObject();
        }



        private class Crs
        {
            public CrsValue CRS { get; }

            public Crs(CrsValue cv)
            {
                CRS = cv;
            }
        }

        private class CrsValue
        {
            public string Type { get; }

            public CrsValueProp Properties { get; }


            public CrsValue(string tstring, CrsValueProp prop)
            {
                Type = tstring;
                Properties = prop;
            }

        }

        private class CrsValueProp
        {
            public string Name { get; }

            public CrsValueProp(int srid)
            {
                //Name = $"urn:ogc:def:crs:EPSG::{srid}";
                Name = $"EPSG:{srid}";
            }
        }

    }
}
