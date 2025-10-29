using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LyricSync.Migrations
{
    /// <inheritdoc />
    public partial class MakeLyricsIdNullalble : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "LyricsId",
                table: "Song",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.DropIndex(
                name: "IX_Song_LyricsId",
                table: "Song");

            migrationBuilder.CreateIndex(
                name: "IX_Song_LyricsId",
                table: "Song",
                column: "LyricsId",
                unique: true,
                filter: "[LyricsId] IS NOT NULL");

            migrationBuilder.DropForeignKey(
                name: "FK_Song_Lyric_LyricsId",
                table: "Song");

            migrationBuilder.AddForeignKey(
                name: "FK_Song_Lyric_LyricsId",
                table: "Song",
                column: "LyricsId",
                principalTable: "Lyric",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Song_Lyric_LyricsId",
                table: "Song");

            migrationBuilder.DropIndex(
                name: "IX_Song_LyricsId",
                table: "Song");

            migrationBuilder.DropColumn(
                name: "LyricsId",
                table: "Song");

            migrationBuilder.AddColumn<string>(
                name: "Lyrics",
                table: "Song",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
