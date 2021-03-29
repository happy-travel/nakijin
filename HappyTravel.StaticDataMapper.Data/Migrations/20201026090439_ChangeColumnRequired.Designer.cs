﻿// <auto-generated />
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
    [Migration("20201026090439_ChangeColumnRequired")]
    partial class ChangeColumnRequired
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:PostgresExtension:postgis", ",,")
                .HasAnnotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("HappyTravel.StaticDataMapper.Data.Models.AccommodationUncertainMatches", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

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

            modelBuilder.Entity("HappyTravel.StaticDataMapper.Data.Models.Accommodations.RichAccommodationDetails", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("AccommodationWithManualCorrections")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("CalculatedAccommodation")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("CountryCode")
                        .HasColumnType("text");

                    b.Property<bool>("IsCalculated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<string>("SupplierAccommodationCodes")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("SuppliersPriority")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.HasKey("Id");

                    b.ToTable("Accommodations");
                });

            modelBuilder.Entity("HappyTravel.StaticDataMapper.Data.Models.Mappers.RawAccommodation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

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

            modelBuilder.Entity("HappyTravel.StaticDataMapper.Data.Models.StaticData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

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
