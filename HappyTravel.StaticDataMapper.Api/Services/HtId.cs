using System;
using CSharpFunctionalExtensions;
using HappyTravel.StaticDataMapper.Api.Models.LocationServiceInfo;

namespace HappyTravel.StaticDataMapper.Api.Services
{
    public static class HtId
    {
        public static string Create(AccommodationMapperLocationTypes type, int id)
            => $"{type}{Delimiter}{id}";

        
        public static Result<(AccommodationMapperLocationTypes type, int id)> Parse(string htId)
        {
            var idParts = htId.Split(Delimiter);
            if (idParts.Length != 2)
                return Result.Failure<(AccommodationMapperLocationTypes, int)>($"Could not parse '{htId}'");

            var typeAsString = idParts[0];
            if (!Enum.TryParse<AccommodationMapperLocationTypes>(typeAsString, out var type))
                return Result.Failure<(AccommodationMapperLocationTypes, int)>($"Could not get location type from '{typeAsString}'");

            var idAsString = idParts[1];
            if (!int.TryParse(idAsString, out var id))
                return Result.Failure<(AccommodationMapperLocationTypes, int)>($"Could not get id from '{idAsString}'");

            return (type, id);
        }

        
        private const string Delimiter = "_";
    }
}