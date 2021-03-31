﻿// <auto-generated />
using System;
using System.Text.Json;
using HappyTravel.MultiLanguage;
using HappyTravel.Nakijin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace HappyTravel.Nakijin.Data.Migrations
{
    [DbContext(typeof(NakijinContext))]
    [Migration("20210325141509_AddedForeignKeysToAccommodationUncertainMatches")]
    partial class AddedForeignKeysToAccommodationUncertainMatches
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasPostgresExtension("postgis")
                .HasPostgresExtension("uuid-ossp")
                .UseIdentityByDefaultColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.2");

            modelBuilder.Entity("HappyTravel.Nakijin.Data.Models.AccommodationUncertainMatches", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("now() at time zone 'utc'");

                    b.Property<int>("FirstHtId")
                        .HasColumnType("integer");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<DateTime>("Modified")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("now() at time zone 'utc'");

                    b.Property<float>("Score")
                        .HasColumnType("real");

                    b.Property<int>("SecondHtId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("FirstHtId");

                    b.HasIndex("SecondHtId");

                    b.ToTable("AccommodationUncertainMatches");
                });

            modelBuilder.Entity("HappyTravel.Nakijin.Data.Models.Accommodations.RichAccommodationDetails", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("AccommodationWithManualCorrections")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("CalculatedAccommodation")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("CountryCode")
                        .HasColumnType("text");

                    b.Property<int>("CountryId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("now() at time zone 'utc'");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsCalculated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<int?>("LocalityId")
                        .HasColumnType("integer");

                    b.Property<int?>("LocalityZoneId")
                        .HasColumnType("integer");

                    b.Property<string>("MappingData")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<DateTime>("Modified")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("now() at time zone 'utc'");

                    b.Property<string>("SupplierAccommodationCodes")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("SuppliersPriority")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.HasKey("Id");

                    b.ToTable("Accommodations");
                });

            modelBuilder.Entity("HappyTravel.Nakijin.Data.Models.Country", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("now() at time zone 'utc'");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<DateTime>("Modified")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("now() at time zone 'utc'");

                    b.Property<MultiLanguage<string>>("Names")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("SupplierCountryCodes")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.HasKey("Id");

                    b.ToTable("Countries");
                });

            modelBuilder.Entity("HappyTravel.Nakijin.Data.Models.Locality", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .UseIdentityByDefaultColumn();

                    b.Property<int>("CountryId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("now() at time zone 'utc'");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<DateTime>("Modified")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("now() at time zone 'utc'");

                    b.Property<MultiLanguage<string>>("Names")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("SupplierLocalityCodes")
                        .HasColumnType("jsonb");

                    b.HasKey("Id");

                    b.ToTable("Localities");
                });

            modelBuilder.Entity("HappyTravel.Nakijin.Data.Models.LocalityZone", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("now() at time zone 'utc'");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<int>("LocalityId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("Modified")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("now() at time zone 'utc'");

                    b.Property<MultiLanguage<string>>("Names")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("SupplierLocalityZoneCodes")
                        .HasColumnType("jsonb");

                    b.HasKey("Id");

                    b.ToTable("LocalityZones");
                });

            modelBuilder.Entity("HappyTravel.Nakijin.Data.Models.Mappers.RawAccommodation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .UseIdentityByDefaultColumn();

                    b.Property<JsonDocument>("Accommodation")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("CountryCode")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<MultiLanguage<string>>("CountryNames")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("now() at time zone 'utc'");

                    b.Property<MultiLanguage<string>>("LocalityNames")
                        .HasColumnType("jsonb");

                    b.Property<MultiLanguage<string>>("LocalityZoneNames")
                        .HasColumnType("jsonb");

                    b.Property<DateTime>("Modified")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("now() at time zone 'utc'");

                    b.Property<int>("Supplier")
                        .HasColumnType("integer");

                    b.Property<string>("SupplierAccommodationId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SupplierLocalityCode")
                        .HasColumnType("text");

                    b.Property<string>("SupplierLocalityZoneCode")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("RawAccommodations");
                });

            modelBuilder.Entity("HappyTravel.Nakijin.Data.Models.StaticData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .UseIdentityByDefaultColumn();

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<JsonDocument>("Value")
                        .HasColumnType("jsonb");

                    b.HasKey("Id");

                    b.ToTable("StaticDatas");
                });

            modelBuilder.Entity("HappyTravel.Nakijin.Data.Models.AccommodationUncertainMatches", b =>
                {
                    b.HasOne("HappyTravel.Nakijin.Data.Models.Accommodations.RichAccommodationDetails", "FirstAccommodation")
                        .WithMany("FirstUncertainMatches")
                        .HasForeignKey("FirstHtId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("HappyTravel.Nakijin.Data.Models.Accommodations.RichAccommodationDetails", "SecondAccommodation")
                        .WithMany("SecondUncertainMatches")
                        .HasForeignKey("SecondHtId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("FirstAccommodation");

                    b.Navigation("SecondAccommodation");
                });

            modelBuilder.Entity("HappyTravel.Nakijin.Data.Models.Accommodations.RichAccommodationDetails", b =>
                {
                    b.Navigation("FirstUncertainMatches");

                    b.Navigation("SecondUncertainMatches");
                });
#pragma warning restore 612, 618
        }
    }
}
