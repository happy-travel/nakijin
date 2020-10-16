using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
namespace HappyTravel.PropertyManagement.Data.Models.Accommodations
{
    // TODO: remove and use contracts accommodation
    public class Accommodation
    {
        public string Name { get; set; }

        public string Category { get; set; }

        public AccommodationRatings Rating { get; set; }
        public ContactInfo ContactInfo { get; set; } = new ContactInfo();

        public SlimLocationInfo Location { get; set; }

        public List<Picture> Pictures { get; set; } = new List<Picture>();

        public ScheduleInfo ScheduleInfo { get; set; } 

        public List<TextualDescription> TextualDescriptions { get; set; } = new List<TextualDescription>();

        public PropertyTypes Type { get; set; }

        public string TypeDescription { get; set; }

        public List<string> AccommodationAmenities { get; set; } = new List<string>();

        public List<string> RoomAmenities { get; set; } = new List<string>();

        public Dictionary<string, string> AdditionalInfo { get; set; } = new Dictionary<string, string>();
    }
}