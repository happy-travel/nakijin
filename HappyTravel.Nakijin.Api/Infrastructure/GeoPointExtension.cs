using System;
using HappyTravel.Geography;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public static class GeoPointExtension
    {
        public static bool IsEmpty(this Geography.GeoPoint geoPoint)
            => geoPoint.Equals(NanGeoPoint) || geoPoint.Equals(OriginGeoPoint);

        public static bool IsValid(this Geography.GeoPoint geoPoint)
            => !geoPoint.Latitude.Equals(double.NaN) && !geoPoint.Longitude.Equals(Double.NaN);

        public static readonly Geography.GeoPoint OriginGeoPoint = new GeoPoint(0, 0);
        
        private static readonly Geography.GeoPoint NanGeoPoint = new Geography.GeoPoint(double.NaN, Double.NaN);
    }
}