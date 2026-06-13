using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace indexer.Migrations
{
    /// <inheritdoc />
    public partial class withTotalWords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalWordsCount",
                table: "Documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalWordsCount",
                table: "Documents");
        }
    }
}
