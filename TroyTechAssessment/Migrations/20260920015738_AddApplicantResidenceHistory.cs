using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TroyTechAssessment.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicantResidenceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApplicantId",
                table: "ApplicationResidenceHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE residence
                SET ApplicantId = applicant.Id
                FROM ApplicationResidenceHistory residence
                INNER JOIN ApplicationApplicants applicant
                    ON applicant.ApplicationId = residence.ApplicationId
                   AND applicant.IsPrimary = 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationResidenceHistory_ApplicantId",
                table: "ApplicationResidenceHistory",
                column: "ApplicantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationResidenceHistory_ApplicationApplicants_ApplicantId",
                table: "ApplicationResidenceHistory",
                column: "ApplicantId",
                principalTable: "ApplicationApplicants",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationResidenceHistory_ApplicationApplicants_ApplicantId",
                table: "ApplicationResidenceHistory");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationResidenceHistory_ApplicantId",
                table: "ApplicationResidenceHistory");

            migrationBuilder.DropColumn(
                name: "ApplicantId",
                table: "ApplicationResidenceHistory");
        }
    }
}
