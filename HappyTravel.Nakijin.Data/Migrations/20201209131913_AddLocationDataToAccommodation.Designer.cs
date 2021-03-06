﻿// <auto-generated />
using System;
using System.Text.Json;
using HappyTravel.Nakijin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace HappyTravel.Nakijin.Data.Migrations
{
    [DbContext(typeof(NakijinContext))]
    [Migration("20201209131913_AddLocationDataToAccommodation")]
    partial class AddLocationDataToAccommodation
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasPostgresExtension("postgis")
                .HasPostgresExtension("uuid-ossp")
                .UseIdentityByDefaultColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.0");

            modelBuilder.Entity("HappyTravel.Nakijin.Data.Models.AccommodationUncertainMatches", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .UseIdentityByDefaultColumn();

                    b.Property<int>("ExistingHtId")
                        .HasColumnType("integer");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<int>("NewHtId")
                        .HasColumnType("integer");

                    b.Property<float>("Score")
                        .HasColumnType("real");

                    b.HasKey("Id");

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

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<bool>("IsCalculated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<int>("LocalityId")
                        .HasColumnType("integer");

                    b.Property<int?>("LocalityZoneId")
                        .HasColumnType("integer");

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

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<JsonDocument>("Names")
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

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<JsonDocument>("Names")
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

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<int>("LocalityId")
                        .HasColumnType("integer");

                    b.Property<JsonDocument>("Names")
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

                    b.Property<int>("Supplier")
                        .HasColumnType("integer");

                    b.Property<string>("SupplierAccommodationId")
                        .IsRequired()
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
#pragma warning restore 612, 618
        }
    }
}
