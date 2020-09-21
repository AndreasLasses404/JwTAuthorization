using Microsoft.EntityFrameworkCore.Migrations;

namespace IndividuellUppgift.Migrations
{
    public partial class TokenEnt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LatestTokenTokenId",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Token",
                columns: table => new
                {
                    TokenId = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Token", x => x.TokenId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LatestTokenTokenId",
                table: "AspNetUsers",
                column: "LatestTokenTokenId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Token_LatestTokenTokenId",
                table: "AspNetUsers",
                column: "LatestTokenTokenId",
                principalTable: "Token",
                principalColumn: "TokenId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Token_LatestTokenTokenId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Token");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LatestTokenTokenId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LatestTokenTokenId",
                table: "AspNetUsers");
        }
    }
}
