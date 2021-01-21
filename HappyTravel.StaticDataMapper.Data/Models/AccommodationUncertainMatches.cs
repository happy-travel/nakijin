using System;

namespace HappyTravel.StaticDataMapper.Data.Models
{
    public class AccommodationUncertainMatches
    {
        public int Id { get; set; }
        public int ExistingHtId { get; set; }
        public int NewHtId { get; set; }
        public float Score { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public  DateTime Modified { get; set; }
    }
}