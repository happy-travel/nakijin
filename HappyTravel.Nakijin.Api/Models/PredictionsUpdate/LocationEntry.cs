using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;

namespace HappyTravel.Nakijin.Api.Models.PredictionsUpdate
{
    public readonly struct LocationEntry
    {
        public LocationEntry(EventTypes type, Location location)
        {
            Type = type;
            Location = location;
        }

        public EventTypes Type { get; }
        public Location Location { get; }
    }
}