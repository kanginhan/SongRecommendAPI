using Microsoft.EntityFrameworkCore.Migrations;

namespace SongRecommendAPI.Migrations
{
    public partial class initials : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaseWord",
                columns: table => new
                {
                    Word = table.Column<string>(type: "NVARCHAR(100)", nullable: false),
                    PositivePoint = table.Column<double>(nullable: false),
                    NegativePoint = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseWord", x => x.Word);
                });

            migrationBuilder.CreateTable(
                name: "BaseWordCollectingSong",
                columns: table => new
                {
                    SongId = table.Column<int>(nullable: false),
                    PlayListSeq = table.Column<int>(nullable: false),
                    Title = table.Column<string>(type: "NVARCHAR(1000)", nullable: true),
                    Singer = table.Column<string>(type: "NVARCHAR(1000)", nullable: true),
                    IsPositive = table.Column<bool>(nullable: false),
                    Lyric = table.Column<string>(type: "NTEXT", nullable: true),
                    Status = table.Column<string>(maxLength: 20, nullable: true),
                    Message = table.Column<string>(type: "NVARCHAR(1000)", nullable: true),
                    Rate = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseWordCollectingSong", x => x.SongId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaseWord");

            migrationBuilder.DropTable(
                name: "BaseWordCollectingSong");
        }
    }
}
