using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureMailBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailedScanFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "dmarc_status",
                table: "scans",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "domain_country",
                table: "scans",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "malicious_urls_json",
                table: "scans",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "phishing_keywords_json",
                table: "scans",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "plain_text_body",
                table: "scans",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "provider",
                table: "scans",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "spf_status",
                table: "scans",
                type: "tinyint(1)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dmarc_status",
                table: "scans");

            migrationBuilder.DropColumn(
                name: "domain_country",
                table: "scans");

            migrationBuilder.DropColumn(
                name: "malicious_urls_json",
                table: "scans");

            migrationBuilder.DropColumn(
                name: "phishing_keywords_json",
                table: "scans");

            migrationBuilder.DropColumn(
                name: "plain_text_body",
                table: "scans");

            migrationBuilder.DropColumn(
                name: "provider",
                table: "scans");

            migrationBuilder.DropColumn(
                name: "spf_status",
                table: "scans");
        }
    }
}
