using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chuds2Chads.Migrations
{
    /// <inheritdoc />
    public partial class AddBodySlotToAvatarLoadout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BodyObjectId",
                table: "UserAvatarLoadouts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyObjectId",
                table: "UserAvatarLoadouts");
        }
    }
}
