using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFoundApp.Migrations
{
    /// <inheritdoc />
    public partial class AddModifiedAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "LostFoundItems",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDateTime",
                table: "LostFoundItems",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "LostFoundItems");

            migrationBuilder.DropColumn(
                name: "ModifiedDateTime",
                table: "LostFoundItems");
        }
    }
}
