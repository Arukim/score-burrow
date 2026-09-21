using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScoreBurrow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTownPool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TownPoolTownIds",
                table: "Games",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TownPoolTownIds",
                table: "Games");
        }
    }
}
