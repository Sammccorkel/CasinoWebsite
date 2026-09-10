using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chuds2Chads.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiplayerSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DisconnectedUntilUtc",
                table: "RoomPlayers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "RoomPlayers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "InitialStack",
                table: "RoomPlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsConnected",
                table: "RoomPlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHost",
                table: "RoomPlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastHeartbeatUtc",
                table: "RoomPlayers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "Stack",
                table: "RoomPlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedUtc",
                table: "GameRooms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPlayers",
                table: "GameRooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "MinBet",
                table: "GameRooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "GameRooms",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "GameRooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveRoomId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FriendCode",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenUtc",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "PresenceStatus",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "AspNetUsers"
                SET "FriendCode" = 'C2C-' ||
                    UPPER(SUBSTR(REPLACE("Id", '-', ''), 1, 4)) || '-' ||
                    UPPER(SUBSTR(REPLACE("Id", '-', ''), 5, 4))
                WHERE "FriendCode" = '';
                """);

            migrationBuilder.CreateTable(
                name: "FriendRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequesterUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequesteeUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RespondedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FriendUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameRoomInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoomId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InviteeUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRoomInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameRoomInvites_GameRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "GameRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FriendCode",
                table: "AspNetUsers",
                column: "FriendCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_RequesterUserId_RequesteeUserId_Status",
                table: "FriendRequests",
                columns: new[] { "RequesterUserId", "RequesteeUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_UserId_FriendUserId",
                table: "Friendships",
                columns: new[] { "UserId", "FriendUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameRoomInvites_RoomId_InviteeUserId",
                table: "GameRoomInvites",
                columns: new[] { "RoomId", "InviteeUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FriendRequests");

            migrationBuilder.DropTable(
                name: "Friendships");

            migrationBuilder.DropTable(
                name: "GameRoomInvites");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_FriendCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DisconnectedUntilUtc",
                table: "RoomPlayers");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "RoomPlayers");

            migrationBuilder.DropColumn(
                name: "InitialStack",
                table: "RoomPlayers");

            migrationBuilder.DropColumn(
                name: "IsConnected",
                table: "RoomPlayers");

            migrationBuilder.DropColumn(
                name: "IsHost",
                table: "RoomPlayers");

            migrationBuilder.DropColumn(
                name: "LastHeartbeatUtc",
                table: "RoomPlayers");

            migrationBuilder.DropColumn(
                name: "Stack",
                table: "RoomPlayers");

            migrationBuilder.DropColumn(
                name: "ClosedUtc",
                table: "GameRooms");

            migrationBuilder.DropColumn(
                name: "MaxPlayers",
                table: "GameRooms");

            migrationBuilder.DropColumn(
                name: "MinBet",
                table: "GameRooms");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "GameRooms");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "GameRooms");

            migrationBuilder.DropColumn(
                name: "ActiveRoomId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FriendCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastSeenUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PresenceStatus",
                table: "AspNetUsers");
        }
    }
}
