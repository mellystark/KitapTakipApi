using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapTakipApi.Migrations
{
    /// <inheritdoc />
    public partial class AddIsReadingToBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReading",
                table: "Books",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReading",
                table: "Books");
        }
    }
}
