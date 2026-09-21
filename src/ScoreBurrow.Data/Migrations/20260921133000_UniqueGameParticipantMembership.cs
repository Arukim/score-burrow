using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScoreBurrow.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueGameParticipantMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GameParticipants_GameId_LeagueMembershipId",
                table: "GameParticipants",
                columns: new[] { "GameId", "LeagueMembershipId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameParticipants_GameId_LeagueMembershipId",
                table: "GameParticipants");
        }
    }
}
