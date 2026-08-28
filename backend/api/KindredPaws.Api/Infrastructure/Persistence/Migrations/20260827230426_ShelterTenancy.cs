using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KindredPaws.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShelterTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Postgres has no implicit text->uuid cast; this column was never actually populated by the
            // app (dead field until this migration), so an explicit USING cast is safe here — NULLIF
            // guards against an empty string being cast instead of NULL.
            migrationBuilder.Sql(
                "ALTER TABLE \"Invitations\" ALTER COLUMN \"ShelterId\" TYPE uuid USING NULLIF(\"ShelterId\", '')::uuid;");

            migrationBuilder.AddColumn<string>(
                name: "NewShelterName",
                table: "Invitations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShelterId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_ShelterId",
                table: "Invitations",
                column: "ShelterId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ShelterId",
                table: "AspNetUsers",
                column: "ShelterId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Shelters_ShelterId",
                table: "AspNetUsers",
                column: "ShelterId",
                principalTable: "Shelters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Shelters_ShelterId",
                table: "Invitations",
                column: "ShelterId",
                principalTable: "Shelters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Shelters_ShelterId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Shelters_ShelterId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_ShelterId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ShelterId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NewShelterName",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "ShelterId",
                table: "AspNetUsers");

            migrationBuilder.Sql(
                "ALTER TABLE \"Invitations\" ALTER COLUMN \"ShelterId\" TYPE text USING \"ShelterId\"::text;");
        }
    }
}
