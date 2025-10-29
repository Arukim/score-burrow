using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScoreBurrow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RatingAtGameTime",
                table: "GameParticipants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RatingDeviationAtGameTime",
                table: "GameParticipants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VolatilityAtGameTime",
                table: "GameParticipants",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "RatingHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeagueMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousRating = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousRatingDeviation = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousVolatility = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    NewRating = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewRatingDeviation = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewVolatility = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatingHistory_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RatingHistory_LeagueMemberships_LeagueMembershipId",
                        column: x => x.LeagueMembershipId,
                        principalTable: "LeagueMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RatingHistory_CalculatedAt",
                table: "RatingHistory",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RatingHistory_GameId",
                table: "RatingHistory",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingHistory_LeagueMembershipId",
                table: "RatingHistory",
                column: "LeagueMembershipId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RatingHistory");

            migrationBuilder.DropColumn(
                name: "RatingAtGameTime",
                table: "GameParticipants");

            migrationBuilder.DropColumn(
                name: "RatingDeviationAtGameTime",
                table: "GameParticipants");

            migrationBuilder.DropColumn(
                name: "VolatilityAtGameTime",
                table: "GameParticipants");
        }
    }
}
