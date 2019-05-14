using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SongRecommendAPI.Migrations
{
    public partial class proposesong : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProposeSong",
                columns: table => new
                {
                    SongId = table.Column<int>(nullable: false),
                    PlayListSeq = table.Column<int>(nullable: false),
                    Title = table.Column<string>(type: "NVARCHAR(1000)", nullable: true),
                    Singer = table.Column<string>(type: "NVARCHAR(1000)", nullable: true),
                    Genre = table.Column<string>(type: "NVARCHAR(1000)", nullable: true),
                    ReleaseDate = table.Column<DateTime>(nullable: false),
                    Rate = table.Column<double>(nullable: false),
                    Like = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposeSong", x => x.SongId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProposeSong");
        }
    }
}
