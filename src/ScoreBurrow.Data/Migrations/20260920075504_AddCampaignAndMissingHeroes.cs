using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ScoreBurrow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignAndMissingHeroes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 170,
                column: "HeroClass",
                value: "Mercenary");

            migrationBuilder.UpdateData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 171,
                column: "HeroClass",
                value: "Mercenary");

            migrationBuilder.UpdateData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 172,
                column: "HeroClass",
                value: "Mercenary");

            migrationBuilder.UpdateData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 186,
                column: "HeroClass",
                value: "Artificer");

            migrationBuilder.InsertData(
                table: "Heroes",
                columns: new[] { "Id", "HeroClass", "Name", "TownId" },
                values: new object[,]
                {
                    { 204, "Knight", "Catherine", 1 },
                    { 205, "Knight", "Roland", 1 },
                    { 206, "Ranger", "Gelu", 2 },
                    { 207, "Wizard", "Dracon", 3 },
                    { 208, "Demoniac", "Xeron", 4 },
                    { 209, "Death Knight", "Haart Lich", 5 },
                    { 210, "Overlord", "Mutare", 6 },
                    { 211, "Overlord", "Mutare Drake", 6 },
                    { 212, "Barbarian", "Boragus", 7 },
                    { 213, "Barbarian", "Kilgor", 7 },
                    { 214, "Witch", "Adrienne", 8 },
                    { 215, "Captain", "Bidley", 10 }
                });

            migrationBuilder.UpdateData(
                table: "Towns",
                keyColumn: "Id",
                keyValue: 11,
                column: "Description",
                value: "The home of Artificers and Mercenaries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 204);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 205);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 206);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 207);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 208);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 209);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 210);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 211);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 212);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 213);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 214);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 215);

            migrationBuilder.UpdateData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 170,
                column: "HeroClass",
                value: "Artificer");

            migrationBuilder.UpdateData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 171,
                column: "HeroClass",
                value: "Artificer");

            migrationBuilder.UpdateData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 172,
                column: "HeroClass",
                value: "Artificer");

            migrationBuilder.UpdateData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 186,
                column: "HeroClass",
                value: "Mercenary");

            migrationBuilder.UpdateData(
                table: "Towns",
                keyColumn: "Id",
                keyValue: 11,
                column: "Description",
                value: "The home of Artificiers and Mercenaries");
        }
    }
}
