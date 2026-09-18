using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TroyTechAssessment.Data;

#nullable disable

namespace TroyTechAssessment.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260918184500_AddManagerNoteRowVersion")]
public partial class AddManagerNoteRowVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "ManagerNotes",
            rowVersion: true,
            type: "rowversion",
            nullable: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "ManagerNotes");
    }
}
