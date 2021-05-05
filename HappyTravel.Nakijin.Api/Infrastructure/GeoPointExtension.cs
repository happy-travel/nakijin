using System;
using HappyTravel.Geography;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public static class GeoPointExtension
    {
        public static bool IsEmpty(this Geography.GeoPoint geoPoint) => geoPoint.Equals(NanGeoPoint) || geoPoint.Equals(OriginGeoPoint);


        public static bool IsValid(this Geography.GeoPoint geoPoint)
            => geoPoint.Longitude is >= -180 and <= 180
                && geoPoint.Latitude is >= -90 and <= 90
                && !geoPoint.Latitude.Equals(double.NaN) && !geoPoint.Longitude.Equals(double.NaN);


        public static readonly Geography.GeoPoint OriginGeoPoint = new GeoPoint(0, 0);

        private static readonly Geography.GeoPoint NanGeoPoint = new Geography.GeoPoint(double.NaN, Double.NaN);
    }
}