using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LyricSync.Migrations
{
    public partial class MakeLyricsIdNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "LyricsId",
                table: "Song",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // revert to non-nullable (if there are NULLs this will fail until you fix data)
            migrationBuilder.AlterColumn<int>(
                name: "LyricsId",
                table: "Song",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}