﻿using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;

using System.Text.Json;

namespace MetaCity.IO.GeoJSON.Converters
{
    internal partial class GeometryConverter
    {
        private void WriteCoordinateSequence(Utf8JsonWriter writer, CoordinateSequence sequence, JsonSerializerOptions options, bool multiple = true, OrientationIndex orientation = OrientationIndex.None)
        {
            //writer.WritePropertyName("coordinates");
            if (sequence == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (multiple)
            {
                writer.WriteStartArray();
                if (orientation == OrientationIndex.Clockwise && Orientation.IsCCW(sequence) ||
                    orientation == OrientationIndex.CounterClockwise && !Orientation.IsCCW(sequence))
                {
                    CoordinateSequences.Reverse(sequence);
                }
            }

            bool hasZ = sequence.HasZ;
            for (int i = 0; i < sequence.Count; i++)
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(sequence.GetX(i));
                writer.WriteNumberValue(sequence.GetY(i));

                if (hasZ)
                {
                    double z = sequence.GetZ(i);
                    if (!double.IsNaN(z))
                        writer.WriteNumberValue(sequence.GetZ(i));
                }
                writer.WriteEndArray();

                if (!multiple) break;
            }

            if (multiple)
                writer.WriteEndArray();
        }
    }
}
