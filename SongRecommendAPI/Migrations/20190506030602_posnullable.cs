using Microsoft.EntityFrameworkCore.Migrations;

namespace SongRecommendAPI.Migrations
{
    public partial class posnullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsPositive",
                table: "BaseWordCollectingSong",
                nullable: true,
                oldClrType: typeof(bool));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsPositive",
                table: "BaseWordCollectingSong",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);
        }
    }
}
