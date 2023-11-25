using System;
using System.IO;
using System.Text;
using System.Text.Json;



using UrbanX.IO.GeoJSON.Converters;

namespace UrbanX.IO.GeoJSON
{
    public class GeoJsonWriter
    {
        public GeoJsonConverterFactory ConverterFactory { get; }

        public JsonSerializerOptions SerializerOptions { get; }


        /// <summary>
        /// Creates an instance of this class using the defaults.
        /// </summary>
        public GeoJsonWriter():this(new GeoJsonConverterFactory())
        {
        }


        /// <summary>
        /// Creates an instance of this class using the provided <see cref="GeoJsonConverterFactory"/> and
        /// <see cref="JsonSerializerOptions"/>.
        /// </summary>
        /// <param name="factory">The factory to use when creating geometries.</param>
        public GeoJsonWriter(GeoJsonConverterFactory factory)
        {
            ConverterFactory = factory;

            //var encoderSettings = new TextEncoderSettings();
            //encoderSettings.AllowRanges(UnicodeRanges.CjkCompatibility, UnicodeRanges.CjkCompatibilityForms, UnicodeRanges.CjkCompatibilityIdeographs,
            //    UnicodeRanges.CjkRadicalsSupplement, UnicodeRanges.CjkStrokes, UnicodeRanges.CjkSymbolsandPunctuation, UnicodeRanges.CjkUnifiedIdeographs,
            //    UnicodeRanges.CjkUnifiedIdeographsExtensionA);

            SerializerOptions = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                //Encoder = JavaScriptEncoder.Create(encoderSettings)
            };
            SerializerOptions.Converters.Add(ConverterFactory);
        }


        /// <summary>
        /// Writes the specified object. The precison model in GeoJsonConverterFactory won't affect the output result.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="value">The object.</param>
        /// <returns></returns>
        public string Write<T>(T value)
            where T : class
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            using (var ms = new MemoryStream())
            {
                Serialize(ms, value, SerializerOptions);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }


        /// <summary>
        /// Helper method for writing specified memory stream.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="stream">The memory stream.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">Serializer option.</param>
        private void Serialize<T>(Stream stream, T value, JsonSerializerOptions options)
            where T : class
        {
            using (var writer = new Utf8JsonWriter(stream))
            {
                 JsonSerializer.Serialize(writer, value, options);
            }
        }
    }
}
