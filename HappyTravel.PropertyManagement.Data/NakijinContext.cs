using HappyTravel.PropertyManagement.Data.Models.Mappers;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.PropertyManagement.Data
{
    public class NakijinContext : DbContext
    {
        public NakijinContext(DbContextOptions<NakijinContext> options) : base(options)
        { }


        public virtual DbSet<RawAccommodation> RawAccommodations { get; set; }
    }
}
