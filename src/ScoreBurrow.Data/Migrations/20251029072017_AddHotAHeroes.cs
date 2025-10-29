using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ScoreBurrow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHotAHeroes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Heroes",
                columns: new[] { "Id", "HeroClass", "Name", "TownId" },
                values: new object[,]
                {
                    { 145, "Captain", "Corkes", 10 },
                    { 146, "Captain", "Jeremy", 10 },
                    { 147, "Captain", "Illor", 10 },
                    { 148, "Captain", "Derek", 10 },
                    { 149, "Captain", "Elmore", 10 },
                    { 150, "Captain", "Leena", 10 },
                    { 151, "Captain", "Anabel", 10 },
                    { 152, "Captain", "Cassiopeia", 10 },
                    { 153, "Captain", "Miriam", 10 },
                    { 154, "Captain", "Tark", 10 },
                    { 155, "Navigator", "Manfred", 10 },
                    { 156, "Navigator", "Zilare", 10 },
                    { 157, "Navigator", "Astra", 10 },
                    { 158, "Navigator", "Casmetra", 10 },
                    { 159, "Navigator", "Dargem", 10 },
                    { 160, "Navigator", "Andal", 10 },
                    { 161, "Navigator", "Eovacius", 10 },
                    { 162, "Navigator", "Spint", 10 },
                    { 163, "Mercenary", "Henrietta", 11 },
                    { 164, "Mercenary", "Sam", 11 },
                    { 165, "Mercenary", "Tancred", 11 },
                    { 166, "Mercenary", "Dury", 11 },
                    { 167, "Mercenary", "Morton", 11 },
                    { 168, "Mercenary", "Tavin", 11 },
                    { 169, "Mercenary", "Murdoch", 11 },
                    { 170, "Artificer", "Melchior", 11 },
                    { 171, "Artificer", "Floribert", 11 },
                    { 172, "Artificer", "Wynona", 11 },
                    { 173, "Artificer", "Todd", 11 },
                    { 174, "Artificer", "Agar", 11 },
                    { 175, "Artificer", "Bertram", 11 },
                    { 176, "Artificer", "Wrathmont", 11 },
                    { 177, "Artificer", "Ziph", 11 },
                    { 178, "Artificer", "Victoria", 11 },
                    { 179, "Artificer", "Eanswythe", 11 },
                    { 180, "Artificer", "Frederick", 11 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 145);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 146);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 147);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 148);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 149);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 150);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 151);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 152);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 153);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 154);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 155);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 156);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 157);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 158);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 159);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 160);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 161);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 162);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 163);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 164);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 165);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 166);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 167);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 168);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 169);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 170);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 171);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 172);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 173);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 174);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 175);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 176);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 177);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 178);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 179);

            migrationBuilder.DeleteData(
                table: "Heroes",
                keyColumn: "Id",
                keyValue: 180);
        }
    }
}
