using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KemonoDownloaderDataModels.Migrations
{
    public partial class AddedPathOnDiskForArtist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PathOnDisk",
                table: "Artists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PathOnDisk",
                table: "Artists");
        }
    }
}
