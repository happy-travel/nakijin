using System;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.Nakijin.Data.Models.Mappers;

namespace HappyTravel.Nakijin.Data.Models
{
    public class AccommodationUncertainMatches
    {
        public int Id { get; set; }
        public int SourceHtId { get; set; }
        public int HtIdToMatch { get; set; }
        public float Score { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public  DateTime Modified { get; set; }
        
        public virtual RichAccommodationDetails SourceAccommodation { get; set; }
        public virtual RichAccommodationDetails AccommodationToMatch { get; set; }
    }
}