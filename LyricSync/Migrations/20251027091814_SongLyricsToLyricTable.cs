using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LyricSync.Migrations
{
    /// <inheritdoc />
    public partial class AddSongIdToLyric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // drop FK if it exists
            migrationBuilder.Sql(
                "IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = N'FK_Lyric_Song_SongId') " +
                "BEGIN ALTER TABLE [Lyric] DROP CONSTRAINT [FK_Lyric_Song_SongId] END");

            // drop index if it exists
            migrationBuilder.Sql(
                "IF EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_Lyric_SongId' AND object_id = OBJECT_ID(N'[dbo].[Lyric]')) " +
                "BEGIN DROP INDEX [IX_Lyric_SongId] ON [Lyric] END");

            migrationBuilder.AlterColumn<int>(
                name: "SongId",
                table: "Lyric",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Lyric_SongId",
                table: "Lyric",
                column: "SongId",
                unique: true,
                filter: "[SongId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Lyric_Song_SongId",
                table: "Lyric",
                column: "SongId",
                principalTable: "Song",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // drop FK if exists
            migrationBuilder.Sql(
                "IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = N'FK_Lyric_Song_SongId') " +
                "BEGIN ALTER TABLE [Lyric] DROP CONSTRAINT [FK_Lyric_Song_SongId] END");

            // drop index if exists
            migrationBuilder.Sql(
                "IF EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_Lyric_SongId' AND object_id = OBJECT_ID(N'[dbo].[Lyric]')) " +
                "BEGIN DROP INDEX [IX_Lyric_SongId] ON [Lyric] END");

            migrationBuilder.AlterColumn<int>(
                name: "SongId",
                table: "Lyric",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lyric_SongId",
                table: "Lyric",
                column: "SongId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lyric_Song_SongId",
                table: "Lyric",
                column: "SongId",
                principalTable: "Song",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
