using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TroyTechAssessment.Data;

#nullable disable

namespace TroyTechAssessment.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260918170000_AddManagerNotesTable")]
public partial class ReplaceManagerNotesWithManagerNotesTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ManagerNotes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ApplicationId = table.Column<int>(type: "int", nullable: false),
                ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ManagerNotes", x => x.Id);
                table.ForeignKey(
                    name: "FK_ManagerNotes_Applications_ApplicationId",
                    column: x => x.ApplicationId,
                    principalTable: "Applications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ManagerNotes_AspNetUsers_ManagerId",
                    column: x => x.ManagerId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ManagerNotes_ApplicationId",
            table: "ManagerNotes",
            column: "ApplicationId");

        migrationBuilder.CreateIndex(
            name: "IX_ManagerNotes_ManagerId",
            table: "ManagerNotes",
            column: "ManagerId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ManagerNotes");

        migrationBuilder.AddColumn<string>(
            name: "ManagerNotes",
            table: "Applications",
            type: "nvarchar(4000)",
            maxLength: 4000,
            nullable: true);
    }
}
