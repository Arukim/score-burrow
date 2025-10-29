using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ScoreBurrow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Towns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Towns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeagueMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PlayerNickname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlayerDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Glicko2Rating = table.Column<double>(type: "float", nullable: false),
                    Glicko2RatingDeviation = table.Column<double>(type: "float", nullable: false),
                    Glicko2Volatility = table.Column<double>(type: "float", nullable: false),
                    LastRatingUpdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueMemberships_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Heroes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TownId = table.Column<int>(type: "int", nullable: false),
                    HeroClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Heroes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Heroes_Towns_TownId",
                        column: x => x.TownId,
                        principalTable: "Towns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MapName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WinnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_LeagueMemberships_WinnerId",
                        column: x => x.WinnerId,
                        principalTable: "LeagueMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Games_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeagueMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GamesPlayed = table.Column<int>(type: "int", nullable: false),
                    GamesWon = table.Column<int>(type: "int", nullable: false),
                    TechnicalLosses = table.Column<int>(type: "int", nullable: false),
                    WinRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AveragePosition = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FavoriteTownId = table.Column<int>(type: "int", nullable: true),
                    FavoriteHeroId = table.Column<int>(type: "int", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerStatistics_Heroes_FavoriteHeroId",
                        column: x => x.FavoriteHeroId,
                        principalTable: "Heroes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlayerStatistics_LeagueMemberships_LeagueMembershipId",
                        column: x => x.LeagueMembershipId,
                        principalTable: "LeagueMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerStatistics_Towns_FavoriteTownId",
                        column: x => x.FavoriteTownId,
                        principalTable: "Towns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GameParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeagueMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TownId = table.Column<int>(type: "int", nullable: false),
                    HeroId = table.Column<int>(type: "int", nullable: false),
                    PlayerColor = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    IsWinner = table.Column<bool>(type: "bit", nullable: false),
                    IsTechnicalLoss = table.Column<bool>(type: "bit", nullable: false),
                    GoldTrade = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameParticipants_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameParticipants_Heroes_HeroId",
                        column: x => x.HeroId,
                        principalTable: "Heroes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameParticipants_LeagueMemberships_LeagueMembershipId",
                        column: x => x.LeagueMembershipId,
                        principalTable: "LeagueMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameParticipants_Towns_TownId",
                        column: x => x.TownId,
                        principalTable: "Towns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Towns",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "The home of Knights and Clerics", "Castle" },
                    { 2, "The home of Rangers and Druids", "Rampart" },
                    { 3, "The home of Alchemists and Wizards", "Tower" },
                    { 4, "The home of Demoniac and Heretics", "Inferno" },
                    { 5, "The home of Death Knights and Necromancers", "Necropolis" },
                    { 6, "The home of Overlords and Warlocks", "Dungeon" },
                    { 7, "The home of Barbarians and Battle Mages", "Stronghold" },
                    { 8, "The home of Beastmasters and Witches", "Fortress" },
                    { 9, "The home of Planeswalkers and Elementalists", "Conflux" },
                    { 10, "The home of Captains and Navigators", "Cove" },
                    { 11, "The home of Artificiers and Mercenaries", "Factory" }
                });

            migrationBuilder.InsertData(
                table: "Heroes",
                columns: new[] { "Id", "HeroClass", "Name", "TownId" },
                values: new object[,]
                {
                    { 1, "Knight", "Orrin", 1 },
                    { 2, "Knight", "Valeska", 1 },
                    { 3, "Knight", "Edric", 1 },
                    { 4, "Knight", "Sylvia", 1 },
                    { 5, "Knight", "Lord Haart", 1 },
                    { 6, "Knight", "Sorsha", 1 },
                    { 7, "Knight", "Christian", 1 },
                    { 8, "Knight", "Tyris", 1 },
                    { 9, "Cleric", "Rion", 1 },
                    { 10, "Cleric", "Adela", 1 },
                    { 11, "Cleric", "Cuthbert", 1 },
                    { 12, "Cleric", "Adelaide", 1 },
                    { 13, "Cleric", "Ingham", 1 },
                    { 14, "Cleric", "Sanya", 1 },
                    { 15, "Cleric", "Loynis", 1 },
                    { 16, "Cleric", "Caitlin", 1 },
                    { 17, "Ranger", "Mephala", 2 },
                    { 18, "Ranger", "Ufretin", 2 },
                    { 19, "Ranger", "Jenova", 2 },
                    { 20, "Ranger", "Ryland", 2 },
                    { 21, "Ranger", "Thorgrim", 2 },
                    { 22, "Ranger", "Ivor", 2 },
                    { 23, "Ranger", "Clancy", 2 },
                    { 24, "Ranger", "Kyrre", 2 },
                    { 25, "Druid", "Coronius", 2 },
                    { 26, "Druid", "Uland", 2 },
                    { 27, "Druid", "Elleshar", 2 },
                    { 28, "Druid", "Gem", 2 },
                    { 29, "Druid", "Malcom", 2 },
                    { 30, "Druid", "Melodia", 2 },
                    { 31, "Druid", "Alagar", 2 },
                    { 32, "Druid", "Aeris", 2 },
                    { 33, "Alchemist", "Piquedram", 3 },
                    { 34, "Alchemist", "Thane", 3 },
                    { 35, "Alchemist", "Josephine", 3 },
                    { 36, "Alchemist", "Neela", 3 },
                    { 37, "Alchemist", "Torosar", 3 },
                    { 38, "Alchemist", "Fafner", 3 },
                    { 39, "Alchemist", "Rissa", 3 },
                    { 40, "Alchemist", "Iona", 3 },
                    { 41, "Wizard", "Astral", 3 },
                    { 42, "Wizard", "Halon", 3 },
                    { 43, "Wizard", "Serena", 3 },
                    { 44, "Wizard", "Daremyth", 3 },
                    { 45, "Wizard", "Theodorus", 3 },
                    { 46, "Wizard", "Solmyr", 3 },
                    { 47, "Wizard", "Cyra", 3 },
                    { 48, "Wizard", "Aine", 3 },
                    { 49, "Demoniac", "Fiona", 4 },
                    { 50, "Demoniac", "Rashka", 4 },
                    { 51, "Demoniac", "Marius", 4 },
                    { 52, "Demoniac", "Ignatius", 4 },
                    { 53, "Demoniac", "Octavia", 4 },
                    { 54, "Demoniac", "Calh", 4 },
                    { 55, "Demoniac", "Pyre", 4 },
                    { 56, "Demoniac", "Nymus", 4 },
                    { 57, "Heretic", "Ayden", 4 },
                    { 58, "Heretic", "Xyron", 4 },
                    { 59, "Heretic", "Axsis", 4 },
                    { 60, "Heretic", "Olema", 4 },
                    { 61, "Heretic", "Calid", 4 },
                    { 62, "Heretic", "Ash", 4 },
                    { 63, "Heretic", "Zydar", 4 },
                    { 64, "Heretic", "Xarfax", 4 },
                    { 65, "Death Knight", "Straker", 5 },
                    { 66, "Death Knight", "Vokial", 5 },
                    { 67, "Death Knight", "Moandor", 5 },
                    { 68, "Death Knight", "Charna", 5 },
                    { 69, "Death Knight", "Tamika", 5 },
                    { 70, "Death Knight", "Isra", 5 },
                    { 71, "Death Knight", "Clavius", 5 },
                    { 72, "Death Knight", "Galthran", 5 },
                    { 73, "Necromancer", "Septienna", 5 },
                    { 74, "Necromancer", "Aislinn", 5 },
                    { 75, "Necromancer", "Sandro", 5 },
                    { 76, "Necromancer", "Nimbus", 5 },
                    { 77, "Necromancer", "Thant", 5 },
                    { 78, "Necromancer", "Xsi", 5 },
                    { 79, "Necromancer", "Vidomina", 5 },
                    { 80, "Necromancer", "Nagash", 5 },
                    { 81, "Overlord", "Lorelei", 6 },
                    { 82, "Overlord", "Arlach", 6 },
                    { 83, "Overlord", "Dace", 6 },
                    { 84, "Overlord", "Ajit", 6 },
                    { 85, "Overlord", "Damacon", 6 },
                    { 86, "Overlord", "Gunnar", 6 },
                    { 87, "Overlord", "Synca", 6 },
                    { 88, "Overlord", "Shakti", 6 },
                    { 89, "Warlock", "Alamar", 6 },
                    { 90, "Warlock", "Jaegar", 6 },
                    { 91, "Warlock", "Malekith", 6 },
                    { 92, "Warlock", "Jeddite", 6 },
                    { 93, "Warlock", "Geon", 6 },
                    { 94, "Warlock", "Deemer", 6 },
                    { 95, "Warlock", "Sephinroth", 6 },
                    { 96, "Warlock", "Darkstorn", 6 },
                    { 97, "Barbarian", "Yog", 7 },
                    { 98, "Barbarian", "Gurnisson", 7 },
                    { 99, "Barbarian", "Jabarkas", 7 },
                    { 100, "Barbarian", "Shiva", 7 },
                    { 101, "Barbarian", "Gretchin", 7 },
                    { 102, "Barbarian", "Krellion", 7 },
                    { 103, "Barbarian", "Crag Hack", 7 },
                    { 104, "Barbarian", "Tyraxor", 7 },
                    { 105, "Battle Mage", "Gird", 7 },
                    { 106, "Battle Mage", "Vey", 7 },
                    { 107, "Battle Mage", "Dessa", 7 },
                    { 108, "Battle Mage", "Terek", 7 },
                    { 109, "Battle Mage", "Zubin", 7 },
                    { 110, "Battle Mage", "Gundula", 7 },
                    { 111, "Battle Mage", "Oris", 7 },
                    { 112, "Battle Mage", "Saurug", 7 },
                    { 113, "Beastmaster", "Bron", 8 },
                    { 114, "Beastmaster", "Drakon", 8 },
                    { 115, "Beastmaster", "Wystan", 8 },
                    { 116, "Beastmaster", "Tazar", 8 },
                    { 117, "Beastmaster", "Alkin", 8 },
                    { 118, "Beastmaster", "Korbac", 8 },
                    { 119, "Beastmaster", "Gerwulf", 8 },
                    { 120, "Beastmaster", "Broghild", 8 },
                    { 121, "Witch", "Mirlanda", 8 },
                    { 122, "Witch", "Rosic", 8 },
                    { 123, "Witch", "Voy", 8 },
                    { 124, "Witch", "Verdish", 8 },
                    { 125, "Witch", "Merist", 8 },
                    { 126, "Witch", "Styg", 8 },
                    { 127, "Witch", "Andra", 8 },
                    { 128, "Witch", "Tiva", 8 },
                    { 129, "Planeswalker", "Pasis", 9 },
                    { 130, "Planeswalker", "Thunar", 9 },
                    { 131, "Planeswalker", "Ignissa", 9 },
                    { 132, "Planeswalker", "Lacus", 9 },
                    { 133, "Planeswalker", "Monere", 9 },
                    { 134, "Planeswalker", "Erdamon", 9 },
                    { 135, "Planeswalker", "Fiur", 9 },
                    { 136, "Planeswalker", "Kalt", 9 },
                    { 137, "Elementalist", "Luna", 9 },
                    { 138, "Elementalist", "Brissa", 9 },
                    { 139, "Elementalist", "Ciele", 9 },
                    { 140, "Elementalist", "Labetha", 9 },
                    { 141, "Elementalist", "Inteus", 9 },
                    { 142, "Elementalist", "Aenain", 9 },
                    { 143, "Elementalist", "Gelare", 9 },
                    { 144, "Elementalist", "Grindan", 9 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameParticipants_GameId",
                table: "GameParticipants",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameParticipants_GameId_PlayerColor",
                table: "GameParticipants",
                columns: new[] { "GameId", "PlayerColor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameParticipants_HeroId",
                table: "GameParticipants",
                column: "HeroId");

            migrationBuilder.CreateIndex(
                name: "IX_GameParticipants_LeagueMembershipId",
                table: "GameParticipants",
                column: "LeagueMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_GameParticipants_TownId",
                table: "GameParticipants",
                column: "TownId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_LeagueId",
                table: "Games",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_StartTime",
                table: "Games",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Status",
                table: "Games",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Games_WinnerId",
                table: "Games",
                column: "WinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Heroes_TownId",
                table: "Heroes",
                column: "TownId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueMemberships_LeagueId",
                table: "LeagueMemberships",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueMemberships_LeagueId_PlayerNickname",
                table: "LeagueMemberships",
                columns: new[] { "LeagueId", "PlayerNickname" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeagueMemberships_UserId",
                table: "LeagueMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_IsActive",
                table: "Leagues",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_OwnerId",
                table: "Leagues",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStatistics_FavoriteHeroId",
                table: "PlayerStatistics",
                column: "FavoriteHeroId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStatistics_FavoriteTownId",
                table: "PlayerStatistics",
                column: "FavoriteTownId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStatistics_LeagueMembershipId",
                table: "PlayerStatistics",
                column: "LeagueMembershipId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Towns_Name",
                table: "Towns",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameParticipants");

            migrationBuilder.DropTable(
                name: "PlayerStatistics");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Heroes");

            migrationBuilder.DropTable(
                name: "LeagueMemberships");

            migrationBuilder.DropTable(
                name: "Towns");

            migrationBuilder.DropTable(
                name: "Leagues");
        }
    }
}
