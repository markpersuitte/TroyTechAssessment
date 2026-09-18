using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TroyTechAssessment.Data;

#nullable disable

namespace TroyTechAssessment.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260918190000_AddDraftResidenceAndApplicationModified")]
public partial class AddDraftResidenceAndApplicationModified : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DraftResidenceHistoryJson",
            table: "Applications",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastModifiedAtUtc",
            table: "Applications",
            type: "datetime2",
            nullable: false,
            defaultValueSql: "GETUTCDATE()");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DraftResidenceHistoryJson", table: "Applications");
        migrationBuilder.DropColumn(name: "LastModifiedAtUtc", table: "Applications");
    }
}
