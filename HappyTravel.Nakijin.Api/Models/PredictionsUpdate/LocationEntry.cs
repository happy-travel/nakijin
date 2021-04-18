using HappyTravel.Nakijin.Api.Models.LocationServiceInfo;

namespace HappyTravel.Nakijin.Api.Models.PredictionsUpdate
{
    public readonly struct LocationEntry
    {
        public LocationEntry(EntryTypes type, Location location)
        {
            Type = type;
            Location = location;
        }

        public EntryTypes Type { get; }
        public Location Location { get; }
    }
}