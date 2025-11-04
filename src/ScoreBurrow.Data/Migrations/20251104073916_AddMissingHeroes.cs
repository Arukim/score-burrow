using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ScoreBurrow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingHeroes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Heroes",
                columns: new[] { "Id", "HeroClass", "Name", "TownId" },
                values: new object[,]
                {
                    { 181, "Knight", "Beatrice", 1 },
                    { 182, "Knight", "Sir Mullich", 1 },
                    { 183, "Ranger", "Giselle", 2 },
                    { 184, "Death Knight", "Ranloo", 5 },
                    { 185, "Witch", "Kinkeria", 8 },
                    { 186, "Mercenary", "Celestine", 11 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 181);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 182);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 183);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 184);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 185);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 186);
        }
    }
}
