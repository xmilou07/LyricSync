using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LyricSync.Migrations
{
    /// <inheritdoc />
    public partial class RenameFilePathToMP3File : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
               name: "FilePath",
               table: "Song",
               newName: "MP3File");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                 name: "MP3File",
                 table: "Song",
                 newName: "FilePath");
        }
    }
}
