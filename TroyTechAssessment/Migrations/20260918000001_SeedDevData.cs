using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TroyTechAssessment.Data;

#nullable disable

namespace TroyTechAssessment.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260918000001_SeedDevData")]
    public partial class SeedDevData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // default passwords are P@ssw0rd!
            migrationBuilder.Sql(@"
INSERT INTO [AspNetUsers] ([Id], [FirstName], [LastName], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount])
VALUES
    ('10000000-0000-0000-0000-000000000001', 'Mark', 'Persuitte', 'markdp73@gmail.com', 'MARKDP73@GMAIL.COM', 'markdp73@gmail.com', 'MARKDP73@GMAIL.COM', 0, 'AQAAAAIAAYagAAAAEFI8VV+QYgH3k5Y0JNaIfJNcD2JPSisnxx5juWvFCOFEuq1dpt4rE3HUiYjGJANesQ==', 'markdp73-stamp', 'markdp73-concurrency', '555-100-1000', 0, 0, NULL, 0, 0);
");

            migrationBuilder.Sql(@"
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
VALUES
    ('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001');
");
            
            migrationBuilder.Sql(@"
INSERT INTO [ManagerProperties] ([ManagerId], [PropertyId])
VALUES
    ('10000000-0000-0000-0000-000000000001', 1),
    ('10000000-0000-0000-0000-000000000001', 2),
    ('10000000-0000-0000-0000-000000000001', 3),
    ('10000000-0000-0000-0000-000000000001', 4),
    ('10000000-0000-0000-0000-000000000001', 5);");

        }
    }
}
