using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;

namespace HappyTravel.Nakijin.Api.Models.StaticDataPublications
{
    public readonly struct LocationPublicationEntry
    {
        public LocationPublicationEntry(UpdateEventTypes type, Location location)
        {
            Type = type;
            Location = location;
        }

        public UpdateEventTypes Type { get; }
        public Location Location { get; }
    }
}