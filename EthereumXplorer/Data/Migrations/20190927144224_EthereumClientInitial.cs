using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EthereumXplorer.Data.Migrations
{
    public partial class EthereumClientInitial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EthereumClientTransactions",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    TransactionHash = table.Column<string>(nullable: true),
                    BlockHash = table.Column<string>(nullable: true),
                    From = table.Column<string>(nullable: true),
                    To = table.Column<string>(nullable: true),
                    Amount = table.Column<decimal>(nullable: false),
                    Input = table.Column<string>(nullable: true),
                    Nonce = table.Column<string>(nullable: true),
                    BlockNumber = table.Column<string>(nullable: true),
                    TransactionIndex = table.Column<string>(nullable: true),
                    Gas = table.Column<string>(nullable: true),
                    GasPrice = table.Column<string>(nullable: true),
                    CreatedDateTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EthereumClientTransactions", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EthereumClientTransactions");
        }
    }
}
