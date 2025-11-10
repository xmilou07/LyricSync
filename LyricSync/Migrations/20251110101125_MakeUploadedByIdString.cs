using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LyricSync.Migrations
{
    public partial class MakeUploadedByIdString : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert column to string and allow NULLs so we can fix existing data first
            migrationBuilder.AlterColumn<string>(
                name: "UploadedById",
                table: "Song",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Clear any values that cannot reference AspNetUsers.Id to avoid FK errors.
            // If you do have a mapping from old integer ids to new user Id strings,
            // replace this UPDATE with a mapping update.
            migrationBuilder.Sql("UPDATE [Song] SET [UploadedById] = NULL WHERE ISNUMERIC([UploadedById]) = 1 OR [UploadedById] IS NOT NULL");

            // Create index only for non-null UploadedById values
            migrationBuilder.CreateIndex(
                name: "IX_Song_UploadedById",
                table: "Song",
                column: "UploadedById",
                unique: false,
                filter: "[UploadedById] IS NOT NULL");

            // Add foreign key; use SetNull so deleting a user will null out the FK instead of cascading deletes
            migrationBuilder.AddForeignKey(
                name: "FK_Song_AspNetUsers_UploadedById",
                table: "Song",
                column: "UploadedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Song_AspNetUsers_UploadedById",
                table: "Song");

            migrationBuilder.DropIndex(
                name: "IX_Song_UploadedById",
                table: "Song");

            // revert to int; if you had nulls, this will fail — handle carefully when rolling back
            migrationBuilder.AlterColumn<int>(
                name: "UploadedById",
                table: "Song",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
