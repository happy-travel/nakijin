using System.Collections.Generic;
using HappyTravel.PropertyManagement.Api.Models.Mappers.Enums;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using HappyTravel.PropertyManagement.Data.Models.Mappers;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace HappyTravel.PropertyManagement.Data
{
    public class NakijinContext : DbContext
    {
        public NakijinContext(DbContextOptions<NakijinContext> options) : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasPostgresExtension("postgis")
                .HasPostgresExtension("uuid-ossp");

            builder.Entity<RawAccommodation>(a =>
            {
                a.HasKey(p => p.Id);
                a.Property(p => p.CountryCode)
                    .IsRequired();
                a.Property(p => p.Accommodation)
                    .IsRequired();
                a.Property(p => p.Supplier)
                    .IsRequired();
                a.Property(p => p.SupplierAccommodationId)
                    .IsRequired();
            });

            builder.Entity<Accommodation>(a =>
            {
                a.HasKey(p => p.Id);
                a.Property(p => p.Name).IsRequired();
                a.Property(p => p.Address).IsRequired();
                a.Property(p => p.Coordinates).IsRequired();
                a.Property(p => p.Rating).IsRequired();
                a.Property(p => p.CountryCode).IsRequired();
                a.Property(p => p.ContactInfo).HasColumnType("jsonb")
                    .HasConversion(c => JsonSerializer.Serialize(c, default),
                        c => JsonSerializer.Deserialize<ContactInfo>(c, default))
                    .IsRequired();
                a.Property(p => p.AccommodationDetails).IsRequired();
                a.Property(p => p.SupplierAccommodationCodes)
                    .HasColumnType("jsonb")
                    .HasConversion(c => JsonConvert.SerializeObject(c),
                        c => JsonConvert.DeserializeObject<Dictionary<Suppliers, string>>(c))
                    .IsRequired();
            });

            builder.Entity<AccommodationUncertainMatches>(m =>
            {
                m.HasKey(p => p.Id);
                m.Property(p => p.ExistingHtId).IsRequired();
                m.Property(p => p.NewHtId).IsRequired();
                m.Property(p => p.IsActive).HasDefaultValue(true).IsRequired();
            });
        }


        public virtual DbSet<Accommodation> Accommodations { get; set; }
        public virtual DbSet<RawAccommodation> RawAccommodations { get; set; }
        public virtual DbSet<AccommodationUncertainMatches> AccommodationUncertainMatches { get; set; }
    }
}