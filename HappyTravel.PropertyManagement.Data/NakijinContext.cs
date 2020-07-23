using HappyTravel.PropertyManagement.Data.Models.Mappers;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.PropertyManagement.Data
{
    public class NakijinContext : DbContext
    {
        public NakijinContext(DbContextOptions<NakijinContext> options) : base(options)
        { }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasPostgresExtension("postgis")
                .HasPostgresExtension("uuid-ossp");

            builder.Entity<RawAccommodation>(a =>
            {
                a.HasKey(a => a.Id);
                a.Property(a => a.Accommodation)
                    .IsRequired();
                a.Property(a => a.Supplier)
                    .IsRequired();
                a.Property(a => a.SupplierId)
                    .IsRequired();
            });
        }


        public virtual DbSet<RawAccommodation> RawAccommodations { get; set; }
    }
}
