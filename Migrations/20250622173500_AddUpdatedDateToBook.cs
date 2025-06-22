using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapTakipApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedDateToBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Books",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Books");
        }
    }
}
