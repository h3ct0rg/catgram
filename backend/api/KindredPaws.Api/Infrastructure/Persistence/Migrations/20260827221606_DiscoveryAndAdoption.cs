using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KindredPaws.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DiscoveryAndAdoption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Shelters",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Shelters",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccessStory",
                table: "Posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AdoptionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnimalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AnswersJson = table.Column<string>(type: "text", nullable: false),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdoptionRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shelters_Name",
                table: "Shelters",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_IsSuccessStory",
                table: "Posts",
                column: "IsSuccessStory");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_Name",
                table: "Animals",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_Sex",
                table: "Animals",
                column: "Sex");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_Size",
                table: "Animals",
                column: "Size");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_Species",
                table: "Animals",
                column: "Species");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionRequests_AnimalId_Status",
                table: "AdoptionRequests",
                columns: new[] { "AnimalId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionRequests_ApplicantUserId",
                table: "AdoptionRequests",
                column: "ApplicantUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdoptionRequests");

            migrationBuilder.DropIndex(
                name: "IX_Shelters_Name",
                table: "Shelters");

            migrationBuilder.DropIndex(
                name: "IX_Posts_IsSuccessStory",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Animals_Name",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_Sex",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_Size",
                table: "Animals");

            migrationBuilder.DropIndex(
                name: "IX_Animals_Species",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Shelters");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Shelters");

            migrationBuilder.DropColumn(
                name: "IsSuccessStory",
                table: "Posts");
        }
    }
}
