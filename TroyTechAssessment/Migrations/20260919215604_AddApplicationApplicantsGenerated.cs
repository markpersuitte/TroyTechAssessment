using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TroyTechAssessment.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationApplicantsGenerated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationApplicants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    CurrentStreetAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentCity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentZipCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationApplicants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationApplicants_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationApplicants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationApplicants_ApplicationId_UserId",
                table: "ApplicationApplicants",
                columns: new[] { "ApplicationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationApplicants_UserId",
                table: "ApplicationApplicants",
                column: "UserId");

            migrationBuilder.Sql("""
                INSERT INTO ApplicationApplicants
                    (ApplicationId, UserId, FirstName, LastName, Email, PhoneNumber,
                     CurrentStreetAddress, CurrentCity, CurrentState, CurrentZipCode, IsPrimary)
                SELECT Id, UserId, FirstName, LastName, Email, PhoneNumber,
                       CurrentStreetAddress, CurrentCity, CurrentState, CurrentZipCode, 1
                FROM Applications;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationApplicants");
        }
    }
}
