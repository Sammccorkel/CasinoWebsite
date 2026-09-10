using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chuds2Chads.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomClosureRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CloseRequested",
                table: "GameRooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloseRequested",
                table: "GameRooms");
        }
    }
}
