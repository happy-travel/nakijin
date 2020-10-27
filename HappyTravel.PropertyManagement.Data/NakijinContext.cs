using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.PropertyManagement.Data.Models;
using HappyTravel.PropertyManagement.Data.Models.Accommodations;
using HappyTravel.PropertyManagement.Data.Models.Mappers;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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

            builder.Entity<RichAccommodationDetails>(a =>
            {
                a.HasKey(p => p.Id);
                a.Property(p => p.CalculatedAccommodation).IsRequired()
                    .HasColumnType("jsonb")
                    .HasConversion(c => JsonConvert.SerializeObject(c),
                        c => JsonConvert.DeserializeObject<Accommodation>(c));
                a.Property(p => p.AccommodationWithManualCorrections)
                    .HasColumnType("jsonb")
                    .HasConversion(c => JsonConvert.SerializeObject(c),
                        c => JsonConvert.DeserializeObject<Accommodation>(c));
                a.Property(p => p.SupplierAccommodationCodes)
                    .HasColumnType("jsonb")
                    .HasConversion(c => JsonConvert.SerializeObject(c),
                        c => JsonConvert.DeserializeObject<Dictionary<Suppliers, string>>(c))
                    .IsRequired();

                a.Property(p => p.SuppliersPriority)
                    .HasColumnType("jsonb")
                    .HasConversion(c => JsonConvert.SerializeObject(c),
                        c => JsonConvert.DeserializeObject<Dictionary<AccommodationDataTypes, List<Suppliers>>>(c))
                    .IsRequired();
                a.Property(p => p.IsCalculated).IsRequired().HasDefaultValue(true);
            });

            builder.Entity<AccommodationUncertainMatches>(m =>
            {
                m.HasKey(p => p.Id);
                m.Property(p => p.ExistingHtId).IsRequired();
                m.Property(p => p.NewHtId).IsRequired();
                m.Property(p => p.IsActive).HasDefaultValue(true).IsRequired();
            });

            builder.Entity<StaticData>(m =>
            {
                m.HasKey(p => p.Id);
                m.Property(p => p.Type).IsRequired();
                m.Property(p => p.Value)
                    .HasColumnType("jsonb");
            });
        }


        public virtual DbSet<RichAccommodationDetails> Accommodations { get; set; }
        public virtual DbSet<RawAccommodation> RawAccommodations { get; set; }
        public virtual DbSet<AccommodationUncertainMatches> AccommodationUncertainMatches { get; set; }

        public virtual DbSet<StaticData> StaticDatas { get; set; }
    }
}