using System;
using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Nakijin.Data.Models;
using HappyTravel.Nakijin.Data.Models.Accommodations;
using HappyTravel.Nakijin.Data.Models.Mappers;
using HappyTravel.SuppliersCatalog;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HappyTravel.Nakijin.Data
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
                a.Property(p => p.CountryNames)
                    .HasColumnType("jsonb")
                    .IsRequired();
                a.Property(p => p.Accommodation)
                    .HasColumnType("jsonb")
                    .IsRequired();

                a.Property(p => p.SupplierLocalityCode);
                a.Property(p => p.LocalityNames)
                    .HasColumnType("jsonb");

                a.Property(p => p.SupplierLocalityZoneCode);
                a.Property(p => p.LocalityZoneNames)
                    .HasColumnType("jsonb");

                a.Property(p => p.Supplier)
                    .IsRequired();
                a.Property(p => p.SupplierAccommodationId)
                    .IsRequired();

                a.Property(p => p.Created)
                    .IsRequired()
                    .HasDefaultValueSql("now() at time zone 'utc'");

                a.Property(p => p.Modified)
                    .IsRequired()
                    .HasDefaultValueSql("now() at time zone 'utc'");
            });

            builder.Entity<RichAccommodationDetails>(a =>
            {
                a.HasKey(p => p.Id);
                a.Property(p => p.CountryId).IsRequired();
                a.Property(p => p.LocalityId);
                a.Property(p => p.LocalityZoneId);
                a.Property(p => p.KeyData)
                    .HasColumnType("jsonb")
                    .HasConversion(c => JsonConvert.SerializeObject(c),
                        c => JsonConvert.DeserializeObject<AccommodationKeyData>(c))
                    .IsRequired();
                a.Property(p => p.CalculatedAccommodation).IsRequired()
                    .HasColumnType("jsonb")
                    .HasConversion(c => JsonConvert.SerializeObject(c),
                        c => JsonConvert.DeserializeObject<MultilingualAccommodation>(c));
                a.Property(p => p.HasDirectContract).IsRequired();
                a.Property(p => p.AccommodationWithManualCorrections)
                    .HasColumnType("jsonb")
                    .HasConversion(c => JsonConvert.SerializeObject(c),
                        c => JsonConvert.DeserializeObject<MultilingualAccommodation>(c));
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
                a.Property(p => p.IsActive).IsRequired();
                a.Property(p => p.DeactivationReason).IsRequired();
                a.Property(p => p.Created)
                    .IsRequired()
                    .HasDefaultValueSql("now() at time zone 'utc'");

                a.Property(p => p.Modified)
                    .IsRequired()
                    .HasDefaultValueSql("now() at time zone 'utc'");

                a.HasOne(rad => rad.Country).WithMany(c => c.Accommodations)
                    .HasForeignKey(rad => rad.CountryId).OnDelete(DeleteBehavior.Restrict);
                a.HasOne(rad => rad.Locality).WithMany(l => l.Accommodations)
                    .HasForeignKey(rad => rad.LocalityId).OnDelete(DeleteBehavior.Restrict);
                a.HasOne(rad => rad.LocalityZone).WithMany(lz => lz.Accommodations)
                    .HasForeignKey(rad => rad.LocalityZoneId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AccommodationUncertainMatches>(m =>
            {
                m.HasKey(p => p.Id);
                m.Property(p => p.SourceHtId).IsRequired();
                m.Property(p => p.HtIdToMatch).IsRequired();
                m.Property(p => p.IsActive).HasDefaultValue(true).IsRequired();
                m.Property(p => p.Created)
                    .IsRequired()
                    .HasDefaultValueSql("now() at time zone 'utc'");

                m.Property(p => p.Modified)
                    .IsRequired()
                    .HasDefaultValueSql("now() at time zone 'utc'");

                m.HasOne(p => p.SourceAccommodation).WithMany(ac => ac.SourceAccommodationUncertainMatches)
                    .HasForeignKey(um => um.SourceHtId).OnDelete(DeleteBehavior.Restrict);
                m.HasOne(p => p.AccommodationToMatch).WithMany(ac => ac.AccommodationToMatchUncertainMatches)
                    .HasForeignKey(um => um.HtIdToMatch).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StaticData>(m =>
            {
                m.HasKey(p => p.Id);
                m.Property(p => p.Type).IsRequired();
                m.Property(p => p.Value)
                    .HasColumnType("jsonb");
            });

            builder.Entity<Country>(c =>
            {
                c.HasKey(p => p.Id);
                c.Property(p => p.Code).IsRequired();
                c.Property(p => p.Names).HasColumnType("jsonb").IsRequired();
                c.Property(p => p.SupplierCountryCodes).HasColumnType("jsonb")
                    .HasConversion(p => JsonConvert.SerializeObject(p),
                        p => JsonConvert.DeserializeObject<Dictionary<Suppliers, string>>(p))
                    .IsRequired();
                c.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);
                c.Property(p => p.Created)
                    .IsRequired()
                    .HasDefaultValueSql("now() at time zone 'utc'");

                c.Property(p => p.Modified)
                    .IsRequired()
                    .HasDefaultValueSql("now() at time zone 'utc'");
            });

            builder.Entity<Locality>(l =>
            {
                l.HasKey(p => p.Id);
                l.Property(p => p.CountryId).IsRequired();
                l.Property(p => p.Names).HasColumnType("jsonb").IsRequired();
                l.Property(p => p.SupplierLocalityCodes).HasColumnType("jsonb")
                    .HasConversion(p => JsonConvert.SerializeObject(p),
                        p => JsonConvert.DeserializeObject<Dictionary<Suppliers, string>>(p));
                l.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);
                l.Property(p => p.Created)
                    .IsRequired()
                    .HasDefaultValueSql("now() at time zone 'utc'");

                l.Property(p => p.Modified)
                    .IsRequired()
                    .HasDefaultValueSql("now() at time zone 'utc'");

                l.HasOne(loc => loc.Country).WithMany(c => c.Localities)
                    .HasForeignKey(loc => loc.CountryId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<LocalityZone>(lz =>
            {
                lz.HasKey(p => p.Id);
                lz.Property(p => p.LocalityId).IsRequired();
                lz.Property(p => p.Names).HasColumnType("jsonb").IsRequired();
                lz.Property(p => p.SupplierLocalityZoneCodes).HasColumnType("jsonb")
                    .HasConversion(p => JsonConvert.SerializeObject(p),
                        p => JsonConvert.DeserializeObject<Dictionary<Suppliers, string>>(p));
                lz.Property(p => p.IsActive).IsRequired();
                lz.Property(p => p.Created)
                    .IsRequired()
                    .HasDefaultValueSql("now() at time zone 'utc'");

                lz.Property(p => p.Modified)
                    .IsRequired()
                    .HasDefaultValueSql("now() at time zone 'utc'");

                lz.HasOne(z => z.Locality).WithMany(l => l.LocalityZones)
                    .HasForeignKey(z => z.LocalityId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<HtAccommodationMapping>(ha =>
            {
                ha.HasKey(p => p.Id);
                ha.Property(p => p.HtId).IsRequired();
                ha.Property(p => p.Modified).IsRequired();
                ha.Property(p => p.Created).IsRequired();
                ha.Property(p => p.MappedHtIds).HasColumnType("jsonb").IsRequired();

                ha.HasOne(p => p.Accommodation).WithMany(ac => ac.HtAccommodationMappings)
                    .HasForeignKey(um => um.HtId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<DataUpdateHistory>(uh =>
            {
                uh.HasKey(p => p.Id);
                uh.Property(p => p.Type).IsRequired();
                uh.Property(p => p.Supplier).IsRequired();
                uh.Property(p => p.UpdateTime).IsRequired();
            });

            //TODO: Remove (AA-372)
            builder.Entity<ProjectionForGroupedAccommodations>()
                .HasNoKey()
                .ToView(null);
        }


        public virtual DbSet<Country> Countries { get; set; }
        public virtual DbSet<Locality> Localities { get; set; }
        public virtual DbSet<LocalityZone> LocalityZones { get; set; }
        public virtual DbSet<RichAccommodationDetails> Accommodations { get; set; }
        public virtual DbSet<RawAccommodation> RawAccommodations { get; set; }
        public virtual DbSet<AccommodationUncertainMatches> AccommodationUncertainMatches { get; set; }
        public virtual DbSet<StaticData> StaticData { get; set; }
        public virtual DbSet<HtAccommodationMapping> HtAccommodationMappings { get; set; }
        public virtual DbSet<DataUpdateHistory> DataUpdateHistories { get; set; }

        //TODO: Remove (AA-372)
        public DbSet<ProjectionForGroupedAccommodations> ProjectionsForGroupedAccommodations { get; set; }
    }
}