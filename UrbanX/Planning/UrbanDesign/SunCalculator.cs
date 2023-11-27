using Rhino.Geometry;

using System;

namespace MetaCity.Planning.UrbanDesign
{
    public class SunCalculator
    {
        private Point3d _selfCenpt;
        private readonly double _selfScale;
        private readonly double _selfTimeZone;
        private readonly double _selfTime;
        private readonly double _selfNorthAngle;

        /// <summary>
        /// The 3d vector of current sun light.
        /// </summary>
        public Vector3d SunVector { get; }

        /// <summary>
        /// The altitude of current sun.
        /// </summary>
        public double SolAlt { get; }
        public double SolAz { get; }


        /// <summary>
        /// The minimum distance between two residential buildings.
        /// </summary>
        public double SunDistance { get; }


        /// <summary>s
        /// Constructor of class.
        /// </summary>
        /// <param name="latitude"> The latitude of current site. </param>
        /// <param name="height"> The height of residential building. </param>
        /// <param name="longtitude"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="northAngle"></param>
        /// <param name="timeZone"></param>
        public SunCalculator(double latitude, double height, double longtitude = 0d, int year = 2018, int month = 12, int day = 23, int hour = 12, double northAngle = 0d, double timeZone = 0d)
        {
            _selfCenpt = new Point3d(0, 0, 0);
            _selfScale = 100d;
            _selfTimeZone = timeZone;
            _selfTime = hour;
            _selfNorthAngle = northAngle;

            SunVector = GetSunVector(latitude, longtitude, year, month, day);
            SolAlt = GetSolAlt(latitude, longtitude, year, month, day);
            SolAz = GetSolAz(latitude, longtitude, year, month, day);

            SunDistance = GetDistanceBasedOnSunVec(height, SunVector);
        }

        private Vector3d GetSunVector(double latitude, double longtitude, int year, int month, int day)
        {
            double solAlt = GetSolAlt(latitude, longtitude, year, month, day);
            double solAz = GetSolAz(latitude, longtitude, year, month, day);
            double angle2North = GetAngle2North(_selfNorthAngle);

            var basePt = Point3d.Add(_selfCenpt, new Vector3d(0, _selfScale, 0));
            var rotateVec = new Vector3d(basePt);
            rotateVec.Rotate(solAlt, Vector3d.XAxis);
            rotateVec.Rotate(-(solAz - angle2North), Vector3d.ZAxis);
            basePt = new Point3d(rotateVec);

            Vector3d sunVector = new Vector3d(_selfCenpt - basePt);
            sunVector.Unitize();
            return sunVector;
        }

        #region GetSunVector->solAlt
        private double GetSolAlt(double latitude, double longtitude, int year, int month, int day)
        {
            double zenith = GetZenith(latitude, longtitude, year, month, day);
            var solAlt = (Math.PI / 2) - zenith;
            return solAlt;
        }


        #region solAlt->zenith
        private double GetZenith(double latitude, double longtitude, int year, int month, int day)
        {
            double solLat = GetSolLat(latitude);
            double solDec = GetSolDec(year, month, day);
            double hourAngle = GetHourAngle(longtitude, year, month, day);
            var zenith = Math.Acos(Math.Sin(solLat) * Math.Sin(solDec) + Math.Cos(solLat) * Math.Cos(solDec) * Math.Cos(ConvertDegreesToRadians(hourAngle)));
            return zenith;
        }

        #region calculate zenith->solLat
        private double GetSolLat(double _lattitude)
        {
            var solLat = ConvertDegreesToRadians(_lattitude);
            return solLat;
        }
        #endregion

        #region calculate zenith->solDec
        private double GetSolDec(int year, int month, int day)
        {
            double julianCentury = GetJulianCentury(year, month, day);
            double obliqueCorr = GetObliqueCorr(year, month, day);
            double sunAppLong = GetSunAppLong(julianCentury);

            var solDec = Math.Asin(Math.Sin(ConvertDegreesToRadians(obliqueCorr)) * Math.Sin(ConvertDegreesToRadians(sunAppLong)));
            return solDec;
        }
        #endregion
        #region calculate zenith->solDec->ObliqueCorr

        /// <summary>
        /// checked
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        private double GetObliqueCorr(int year, int month, int day)
        {
            double julianCentury = GetJulianCentury(year, month, day);
            double meanObliqEcliptic = 23 + (26 + ((21.448 - julianCentury * (46.815 + julianCentury * (0.00059 - julianCentury * 0.001813)))) / 60) / 60;

            var obliqueCorr = meanObliqEcliptic + 0.00256 * Math.Cos(ConvertDegreesToRadians(125.04 - 1934.136 * julianCentury));
            return obliqueCorr;
        }
        #endregion

        #region calculate zenith->solDec->sunAppLong
        private double GetSunAppLong(double julianCentury)
        {
            var geomMeanAnomSun = 357.52911 + julianCentury * (35999.05029 - 0.0001537 * julianCentury);
            var sunEqOfCtr = Math.Sin(ConvertDegreesToRadians(geomMeanAnomSun)) * (1.914602 - julianCentury * (0.004817 + 0.000014 * julianCentury)) + Math.Sin(ConvertDegreesToRadians(2 * geomMeanAnomSun)) * (0.019993 - 0.000101 * julianCentury) + Math.Sin(ConvertDegreesToRadians(3 * geomMeanAnomSun)) * 0.000289;
            var geomMeanLongSun = (280.46646 + julianCentury * (36000.76983 + julianCentury * 0.0003032)) % 360;
            var sunTrueLong = geomMeanLongSun + sunEqOfCtr;

            var sunAppLong = sunTrueLong - 0.00569 - 0.00478 * Math.Sin(ConvertDegreesToRadians(125.04 - 1934.136 * julianCentury));
            return sunAppLong;
        }
        #endregion


        #region calculate zenith->hourAngle

        private double GetEqOfTime(int year, int month, int day)
        {
            var obliqueCorr = GetObliqueCorr(year, month, day);
            double julianCentury = GetJulianCentury(year, month, day);

            var geomMeanAnomSun = 357.52911 + julianCentury * (35999.05029 - 0.0001537 * julianCentury);
            var geomMeanLongSun = (280.46646 + julianCentury * (36000.76983 + julianCentury * 0.0003032)) % 360;
            var eccentOrbit = 0.016708634 - julianCentury * (0.000042037 + 0.0000001267 * julianCentury);
            var varY = Math.Tan(ConvertDegreesToRadians(obliqueCorr / 2)) * Math.Tan(ConvertDegreesToRadians(obliqueCorr / 2));

            var eqOfTime = 4 * ConvertRadiansToDegrees(varY * Math.Sin(2 * ConvertDegreesToRadians(geomMeanLongSun))
            - 2 * eccentOrbit * Math.Sin(ConvertDegreesToRadians(geomMeanAnomSun))
            + 4 * eccentOrbit * varY * Math.Sin(ConvertDegreesToRadians(geomMeanAnomSun)) * Math.Cos(2 * ConvertDegreesToRadians(geomMeanLongSun))
            - 0.5 * (Math.Pow(varY, 2)) * Math.Sin(4 * ConvertDegreesToRadians(geomMeanLongSun))
            - 1.25 * (Math.Pow(eccentOrbit, 2) * Math.Sin(2 * ConvertDegreesToRadians(geomMeanAnomSun))));

            return eqOfTime;
        }


        private double GetHourAngle(double longtitude, int year, int month, int day, bool solarTime = false)
        {
            var _solTime = _selfTime;
            var eqOfTime = GetEqOfTime(year, month, day);

            if (solarTime == false)
                _solTime = ((_selfTime * 60 + eqOfTime + 4 * longtitude - 60 * _selfTimeZone) % 1440) / 60;
            else
                _solTime = _selfTime;

            double hourAngle = 0;
            if (_solTime * 15 < 0)
                hourAngle = _solTime * 15 + 180;
            else
                hourAngle = _solTime * 15 - 180;

            return hourAngle;
        }

        #endregion
        #endregion
        #endregion
        #region GetSunVector->solAz
        private double GetSolAz(double latitude, double longtitude, int year, int month, int day)
        {
            var solLat = GetSolLat(latitude);
            var zenith = GetZenith(latitude, longtitude, year, month, day);
            var solDec = GetSolDec(year, month, day);
            var hourAngle = GetHourAngle(longtitude, year, month, day);

            double solAz;

            if (hourAngle > 0) { solAz = ((Math.Acos(((Math.Sin(solLat) * Math.Cos(zenith)) - Math.Sin(solDec)) / (Math.Cos(solLat) * Math.Sin(zenith))) + Math.PI) % (2 * Math.PI)); }
            else { solAz = ((3 * Math.PI - Math.Acos(((Math.Sin(solLat) * Math.Cos(zenith)) - Math.Sin(solDec)) / (Math.Cos(solLat) * Math.Sin(zenith)))) % (2 * Math.PI)); }

            return solAz;
        }
        #endregion

        #region GetSunVector->angle2North
        private double GetAngle2North(double northAngle)
        {
            var angle2North = northAngle;
            return angle2North;
        }
        #endregion

        private double GetJulianCentury(int year, int month, int day)
        {
            int a;
            if (month < 3) { a = 1; } else { a = 0; }

            var y = year + 4800 - a;
            var m = month + 12 * a - 3;

            double julianDay = day + Math.Floor((153d * m + 2) / 5) + 59;
            julianDay += (_selfTime - _selfTimeZone) / 24.0 + 365 * y + Math.Floor(y / 4d) - Math.Floor(y / 100d) + Math.Floor(y / 400d) - 32045.5 - 59;
            var julianCentury = (julianDay - 2451545) / 36525;
            return julianCentury;
        }

        /// <summary>
        /// 角度转弧度
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        private double ConvertDegreesToRadians(double degrees)
        {
            var radians = degrees * (Math.PI / 180);
            return radians;

        }

        /// <summary>
        /// 弧度转角度
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        private double ConvertRadiansToDegrees(double radians)
        {
            var degrees = radians * (180 / Math.PI);
            return degrees;
        }

        /// <summary>
        /// 计算楼层间距
        /// </summary>
        /// <param name="height"></param>
        /// <param name="sunVector"></param>
        /// <returns></returns>
        private double GetDistanceBasedOnSunVec(double height, Vector3d sunVector)
        {
            Point3d topPt = new Point3d(0, 0, height);
            Vector3d newSunVector = new Vector3d(0d, sunVector.Y, sunVector.Z);

            var lightLine = new Line(topPt, newSunVector);
            Rhino.Geometry.Intersect.Intersection.LinePlane(lightLine, Plane.WorldXY, out double lineValue);
            var distance = lightLine.PointAt(lineValue).DistanceTo(Point3d.Origin);

            return distance;
        }
    }
}
