﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TradingAssistant;

#nullable disable

namespace TradingAssistant.Migrations
{
    [DbContext(typeof(TradingContext))]
    partial class TradingContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.1");

            modelBuilder.Entity("TradingAssistant.OpenPosition", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<decimal>("BreakEvenPrice")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("EntryPrice")
                        .HasColumnType("TEXT");

                    b.Property<bool>("HasStopLossInBreakEven")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Leverage")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PositionSide")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Quantity")
                        .HasColumnType("TEXT");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("UpdateTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Symbol");

                    b.ToTable("OpenPositions");
                });
#pragma warning restore 612, 618
        }
    }
}
