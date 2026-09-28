using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdviceChatThread : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AuthorIsCustomer",
                table: "SubmissionNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SeenByAdviser",
                table: "SubmissionNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SeenByAdviserAt",
                table: "SubmissionNotes",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorIsCustomer",
                table: "SubmissionNotes");

            migrationBuilder.DropColumn(
                name: "SeenByAdviser",
                table: "SubmissionNotes");

            migrationBuilder.DropColumn(
                name: "SeenByAdviserAt",
                table: "SubmissionNotes");
        }
    }
}
