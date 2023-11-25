using NetTopologySuite.Geometries;

namespace UrbanX.Planning.Utility
{
    public class PrecisionSetting
    {

        public static readonly PrecisionModel _precision = new PrecisionModel(1E+6);


        /// <summary>
        /// Change current precision to fixed precision.
        /// </summary>
        /// <param name="gf"></param>
        public static void ChangePrecision(ref GeometryFactory gf)
        {
            ChangePrecision(ref gf, _precision);
        }


        public static void ChangePrecision(ref GeometryFactory gf, PrecisionModel pm)
        {
            gf = new GeometryFactory(pm, gf.SRID);
        }


        public static GeometryFactory GetGeometryFactory( Geometry geom)
        {
            var gf = geom.Factory;

            if (gf.PrecisionModel.IsFloating)
            {
                ChangePrecision(ref gf);
            }

            return gf;
        }

    }
}
