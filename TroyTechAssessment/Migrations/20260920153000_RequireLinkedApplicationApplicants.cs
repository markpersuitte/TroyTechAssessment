using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TroyTechAssessment.Migrations
{
    [Migration("20260920153000_RequireLinkedApplicationApplicants")]
    public partial class RequireLinkedApplicationApplicants : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE history
                FROM [ApplicationResidenceHistory] history
                INNER JOIN [ApplicationApplicants] applicant
                    ON applicant.[Id] = history.[ApplicantId]
                WHERE applicant.[UserId] IS NULL;

                DELETE FROM [ApplicationApplicants]
                WHERE [UserId] IS NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_ApplicationApplicants_ApplicationId_UserId",
                table: "ApplicationApplicants");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "ApplicationApplicants");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "ApplicationApplicants",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationApplicants_ApplicationId_UserId",
                table: "ApplicationApplicants",
                columns: new[] { "ApplicationId", "UserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ApplicationApplicants",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE applicant
                SET [Email] = [user].[Email]
                FROM [ApplicationApplicants] applicant
                INNER JOIN [AspNetUsers] [user] ON [user].[Id] = applicant.[UserId];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationApplicants_ApplicationId_UserId",
                table: "ApplicationApplicants",
                columns: new[] { "ApplicationId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }
    }
}
