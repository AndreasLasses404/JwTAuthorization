using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace IndividuellUppgift.Migrations
{
    public partial class RefreshTokenEnt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RefreshTokenId",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    RefreshTokenId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(nullable: true),
                    Expires = table.Column<DateTime>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false),
                    CreatedByIp = table.Column<string>(nullable: true),
                    Revoked = table.Column<DateTime>(nullable: true),
                    RevokedByIp = table.Column<string>(nullable: true),
                    ReplacedByToken = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.RefreshTokenId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RefreshTokenId",
                table: "AspNetUsers",
                column: "RefreshTokenId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_RefreshToken_RefreshTokenId",
                table: "AspNetUsers",
                column: "RefreshTokenId",
                principalTable: "RefreshToken",
                principalColumn: "RefreshTokenId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_RefreshToken_RefreshTokenId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_RefreshTokenId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RefreshTokenId",
                table: "AspNetUsers");
        }
    }
}
