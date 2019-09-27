﻿// <auto-generated />
using System;
using EthereumXplorer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EthereumXplorer.Data.Migrations
{
    [DbContext(typeof(EthereumClientApplicationDbContext))]
    partial class EthereumClientApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.11-servicing-32099");

            modelBuilder.Entity("EthereumXplorer.Models.EthereumClientTransactionData", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<decimal>("Amount");

                    b.Property<string>("BlockHash");

                    b.Property<string>("BlockNumber");

                    b.Property<DateTime>("CreatedDateTime");

                    b.Property<string>("From");

                    b.Property<string>("Gas");

                    b.Property<string>("GasPrice");

                    b.Property<string>("Input");

                    b.Property<string>("Nonce");

                    b.Property<string>("To");

                    b.Property<string>("TransactionHash");

                    b.Property<string>("TransactionIndex");

                    b.HasKey("Id");

                    b.ToTable("EthereumClientTransactions");
                });
#pragma warning restore 612, 618
        }
    }
}
