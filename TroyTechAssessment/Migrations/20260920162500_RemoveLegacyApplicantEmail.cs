using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TroyTechAssessment.Migrations
{
    [Migration("20260920162500_RemoveLegacyApplicantEmail")]
    public partial class RemoveLegacyApplicantEmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.ApplicationApplicants', 'Email') IS NOT NULL
                BEGIN
                    ALTER TABLE [ApplicationApplicants]
                        ALTER COLUMN [Email] nvarchar(256) NULL;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                INNER JOIN [AspNetUsers] [user]
                    ON [user].[Id] = applicant.[UserId];
                """);
        }
    }
}
