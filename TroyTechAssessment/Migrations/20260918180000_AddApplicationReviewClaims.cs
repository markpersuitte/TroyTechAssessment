using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TroyTechAssessment.Data;

#nullable disable

namespace TroyTechAssessment.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260918180000_AddApplicationReviewClaims")]
public partial class AddApplicationReviewClaims : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ClaimedByManagerId",
            table: "Applications",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ClaimedAtUtc",
            table: "Applications",
            type: "datetime2",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Applications_ClaimedByManagerId",
            table: "Applications",
            column: "ClaimedByManagerId");

        migrationBuilder.AddForeignKey(
            name: "FK_Applications_AspNetUsers_ClaimedByManagerId",
            table: "Applications",
            column: "ClaimedByManagerId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Applications_AspNetUsers_ClaimedByManagerId",
            table: "Applications");
        migrationBuilder.DropIndex(
            name: "IX_Applications_ClaimedByManagerId",
            table: "Applications");
        migrationBuilder.DropColumn(name: "ClaimedByManagerId", table: "Applications");
        migrationBuilder.DropColumn(name: "ClaimedAtUtc", table: "Applications");
    }
}
