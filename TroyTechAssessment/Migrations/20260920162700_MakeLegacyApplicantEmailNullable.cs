using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TroyTechAssessment.Migrations
{
    [Migration("20260920162700_MakeLegacyApplicantEmailNullable")]
    public partial class MakeLegacyApplicantEmailNullable : Migration
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
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.ApplicationApplicants', 'Email') IS NOT NULL
                BEGIN
                    UPDATE [ApplicationApplicants]
                    SET [Email] = [AspNetUsers].[Email]
                    FROM [ApplicationApplicants]
                    INNER JOIN [AspNetUsers]
                        ON [AspNetUsers].[Id] = [ApplicationApplicants].[UserId]
                    WHERE [ApplicationApplicants].[Email] IS NULL;

                    ALTER TABLE [ApplicationApplicants]
                        ALTER COLUMN [Email] nvarchar(256) NOT NULL;
                END;
                """);
        }
    }
}
