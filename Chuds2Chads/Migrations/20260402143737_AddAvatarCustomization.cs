using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chuds2Chads.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatarCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CosmeticDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    AssetKey = table.Column<string>(type: "TEXT", nullable: false),
                    Rarity = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CosmeticDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAvatarLoadouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HeadObjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FaceObjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TorsoObjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LegsObjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ShoeObjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PetObjectId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAvatarLoadouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAvatarLoadouts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCosmeticItems",
                columns: table => new
                {
                    ObjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CosmeticDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EarnedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCosmeticItems", x => x.ObjectId);
                    table.ForeignKey(
                        name: "FK_UserCosmeticItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCosmeticItems_CosmeticDefinitions_CosmeticDefinitionId",
                        column: x => x.CosmeticDefinitionId,
                        principalTable: "CosmeticDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CosmeticDefinitions_AssetKey",
                table: "CosmeticDefinitions",
                column: "AssetKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatarLoadouts_UserId",
                table: "UserAvatarLoadouts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCosmeticItems_CosmeticDefinitionId",
                table: "UserCosmeticItems",
                column: "CosmeticDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCosmeticItems_UserId",
                table: "UserCosmeticItems",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAvatarLoadouts");

            migrationBuilder.DropTable(
                name: "UserCosmeticItems");

            migrationBuilder.DropTable(
                name: "CosmeticDefinitions");
        }
    }
}
