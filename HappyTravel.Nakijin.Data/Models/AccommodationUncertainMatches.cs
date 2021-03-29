using System;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.Nakijin.Data.Models.Mappers;

namespace HappyTravel.Nakijin.Data.Models
{
    public class AccommodationUncertainMatches
    {
        public int Id { get; set; }
        public int FirstHtId { get; set; }
        public int SecondHtId { get; set; }
        public float Score { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public  DateTime Modified { get; set; }
        
        public virtual RichAccommodationDetails FirstAccommodation { get; set; }
        public virtual RichAccommodationDetails SecondAccommodation { get; set; }
    }
}