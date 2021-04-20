using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;

namespace HappyTravel.Nakijin.Api.Models.PredictionsUpdate
{
    public readonly struct LocationEntry
    {
        public LocationEntry(UpdateEventTypes type, Location location)
        {
            Type = type;
            Location = location;
        }

        public UpdateEventTypes Type { get; }
        public Location Location { get; }
    }
}