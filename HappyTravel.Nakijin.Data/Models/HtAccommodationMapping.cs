using System;
using System.Collections.Generic;

namespace HappyTravel.Nakijin.Data.Models
{
    public class HtAccommodationMapping
    {
        public int Id { get; set; }
        public int HtId { get; set; }
        public HashSet<int> MappedHtIds { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}