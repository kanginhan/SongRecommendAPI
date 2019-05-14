using Microsoft.EntityFrameworkCore.Migrations;

namespace SongRecommendAPI.Migrations
{
    public partial class addWordRate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Rate",
                table: "BaseWord",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rate",
                table: "BaseWord");
        }
    }
}
