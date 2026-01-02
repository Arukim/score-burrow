using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ScoreBurrow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBulwarkTownAndHeroes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Towns",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { 12, "The home of Chieftains and Elders", "Bulwark" });

            migrationBuilder.InsertData(
                table: "Heroes",
                columns: new[] { "Id", "HeroClass", "Name", "TownId" },
                values: new object[,]
                {
                    { 187, "Chieftain", "Dhuin", 12 },
                    { 188, "Chieftain", "Oidana", 12 },
                    { 189, "Chieftain", "Neia", 12 },
                    { 190, "Chieftain", "Eikthurn", 12 },
                    { 191, "Chieftain", "Creyle", 12 },
                    { 192, "Chieftain", "Spadum", 12 },
                    { 193, "Chieftain", "Kynr", 12 },
                    { 194, "Chieftain", "Ergon", 12 },
                    { 195, "Elder", "Kriv", 12 },
                    { 196, "Elder", "Glacius", 12 },
                    { 197, "Elder", "Sial", 12 },
                    { 198, "Elder", "Dalton", 12 },
                    { 199, "Elder", "Biarma", 12 },
                    { 200, "Elder", "Akka", 12 },
                    { 201, "Elder", "Vehr", 12 },
                    { 202, "Elder", "Allora", 12 },
                    { 203, "Elder", "Haugir", 12 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 187);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 188);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 189);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 190);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 191);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 192);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 193);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 194);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 195);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 196);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 197);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 198);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 199);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 200);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 201);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 202);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 203);

            migrationBuilder.DeleteData(
                table: "Towns",
                keyColumn: "Id",
                keyValue: 12);
        }
    }
}
