using System.Collections.Generic;

namespace HappyTravel.StaticDataMapper.Api.Models.LocationInfo
{
    public readonly struct LocationMapping
    {
        public Location Location { get; init; }
        public List<AccommodationMapping> AccommodationMappings { get; init; }
    }
}