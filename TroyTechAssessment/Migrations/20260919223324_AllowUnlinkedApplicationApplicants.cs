using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TroyTechAssessment.Migrations
{
    /// <inheritdoc />
    public partial class AllowUnlinkedApplicationApplicants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationApplicants_ApplicationId_UserId",
                table: "ApplicationApplicants");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "ApplicationApplicants",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationApplicants_ApplicationId_UserId",
                table: "ApplicationApplicants",
                columns: new[] { "ApplicationId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationApplicants_ApplicationId_UserId",
                table: "ApplicationApplicants");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "ApplicationApplicants",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationApplicants_ApplicationId_UserId",
                table: "ApplicationApplicants",
                columns: new[] { "ApplicationId", "UserId" },
                unique: true);
        }
    }
}
