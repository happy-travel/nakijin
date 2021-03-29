using System;
using HappyTravel.Geography;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public static class GeoPointExtension
    {
        public static bool IsEmpty(this Geography.GeoPoint geoPoint)
            => geoPoint.Equals(NanGeoPoint) || geoPoint.Equals(OriginGeoPoint);


        private static readonly Geography.GeoPoint NanGeoPoint = new Geography.GeoPoint(double.NaN, Double.NaN);
        private static readonly Geography.GeoPoint OriginGeoPoint = new GeoPoint(0, 0);
    }
}