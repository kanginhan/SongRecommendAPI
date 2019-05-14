using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SongRecommendAPI.Migrations
{
    public partial class datestring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReleaseDate",
                table: "ProposeSong",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ReleaseDate",
                table: "ProposeSong",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
