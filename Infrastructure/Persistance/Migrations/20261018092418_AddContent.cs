using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlogPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SummaryFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SummaryEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FooterLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColumnFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColumnEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LabelFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LabelEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Href = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FooterLinks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogPosts");

            migrationBuilder.DropTable(
                name: "FooterLinks");
        }
    }
}
