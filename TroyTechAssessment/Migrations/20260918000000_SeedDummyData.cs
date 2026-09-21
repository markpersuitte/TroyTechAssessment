using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TroyTechAssessment.Data;

#nullable disable

namespace TroyTechAssessment.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260918000000_SeedDummyData")]
    public partial class SeedDummyData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // default passwords are P@ssw0rd!
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = '10000000-0000-0000-0000-000000000002')
BEGIN
INSERT INTO [AspNetUsers] ([Id], [FirstName], [LastName], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount])
VALUES
    ('10000000-0000-0000-0000-000000000002', 'Ava', 'Hughes', 'manager1@troytech.local', 'MANAGER1@TROYTECH.LOCAL', 'manager1@troytech.local', 'MANAGER1@TROYTECH.LOCAL', 0, 'AQAAAAIAAYagAAAAEIK/kzjtE3fN/N8M5sEI59xQycXxVp2U/7iayV9dBsinM5sbkxGVz9KVRafmFj2ZfA==', 'manager-1-stamp', 'manager-1-concurrency', '555-100-1001', 0, 0, NULL, 0, 0),
    ('10000000-0000-0000-0000-000000000003', 'Noah', 'Patel', 'manager2@troytech.local', 'MANAGER2@TROYTECH.LOCAL', 'manager2@troytech.local', 'MANAGER2@TROYTECH.LOCAL', 0, 'AQAAAAIAAYagAAAAEIK/kzjtE3fN/N8M5sEI59xQycXxVp2U/7iayV9dBsinM5sbkxGVz9KVRafmFj2ZfA==', 'manager-2-stamp', 'manager-2-concurrency', '555-100-1002', 0, 0, NULL, 0, 0),
    ('10000000-0000-0000-0000-000000000011', 'Ella', 'Brown', 'applicant1@troytech.local', 'APPLICANT1@TROYTECH.LOCAL', 'applicant1@troytech.local', 'APPLICANT1@TROYTECH.LOCAL', 0, 'AQAAAAIAAYagAAAAEIK/kzjtE3fN/N8M5sEI59xQycXxVp2U/7iayV9dBsinM5sbkxGVz9KVRafmFj2ZfA==', 'applicant-1-stamp', 'applicant-1-concurrency', '555-200-2001', 0, 0, NULL, 0, 0),
    ('10000000-0000-0000-0000-000000000012', 'Lucas', 'Chen', 'applicant2@troytech.local', 'APPLICANT2@TROYTECH.LOCAL', 'applicant2@troytech.local', 'APPLICANT2@TROYTECH.LOCAL', 0, 'AQAAAAIAAYagAAAAEIK/kzjtE3fN/N8M5sEI59xQycXxVp2U/7iayV9dBsinM5sbkxGVz9KVRafmFj2ZfA==', 'applicant-2-stamp', 'applicant-2-concurrency', '555-200-2002', 0, 0, NULL, 0, 0),
    ('10000000-0000-0000-0000-000000000013', 'Mia', 'Rivera', 'applicant3@troytech.local', 'APPLICANT3@TROYTECH.LOCAL', 'applicant3@troytech.local', 'APPLICANT3@TROYTECH.LOCAL', 0, 'AQAAAAIAAYagAAAAEIK/kzjtE3fN/N8M5sEI59xQycXxVp2U/7iayV9dBsinM5sbkxGVz9KVRafmFj2ZfA==', 'applicant-3-stamp', 'applicant-3-concurrency', '555-200-2003', 0, 0, NULL, 0, 0),
    ('10000000-0000-0000-0000-000000000014', 'Owen', 'Martin', 'applicant4@troytech.local', 'APPLICANT4@TROYTECH.LOCAL', 'applicant4@troytech.local', 'APPLICANT4@TROYTECH.LOCAL', 0, 'AQAAAAIAAYagAAAAEIK/kzjtE3fN/N8M5sEI59xQycXxVp2U/7iayV9dBsinM5sbkxGVz9KVRafmFj2ZfA==', 'applicant-4-stamp', 'applicant-4-concurrency', '555-200-2004', 0, 0, NULL, 0, 0);
END;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [AspNetUserRoles] WHERE [UserId] = '10000000-0000-0000-0000-000000000002' AND [RoleId] = '00000000-0000-0000-0000-000000000001')
BEGIN
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
VALUES
    ('10000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001'),
    ('10000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001'),
    ('10000000-0000-0000-0000-000000000011', '00000000-0000-0000-0000-000000000002'),
    ('10000000-0000-0000-0000-000000000012', '00000000-0000-0000-0000-000000000002'),
    ('10000000-0000-0000-0000-000000000013', '00000000-0000-0000-0000-000000000002'),
    ('10000000-0000-0000-0000-000000000014', '00000000-0000-0000-0000-000000000002');
END;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Id] = '10000000-0000-0000-0000-000000000074')
BEGIN
DECLARE @applicantNumber int = 19;
DECLARE @userId uniqueidentifier;
WHILE @applicantNumber <= 74
BEGIN
    SET @userId = CONVERT(uniqueidentifier, CONCAT(
        '10000000-0000-0000-0000-',
        RIGHT(CONCAT('000000000000', @applicantNumber), 12)));

    INSERT INTO [AspNetUsers]
        ([Id], [FirstName], [LastName], [UserName], [NormalizedUserName],
         [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash],
         [SecurityStamp], [ConcurrencyStamp], [PhoneNumber],
         [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd],
         [LockoutEnabled], [AccessFailedCount])
    VALUES
        (@userId, CONCAT('Applicant', @applicantNumber - 14), 'Generated',
         CONCAT('applicant', @applicantNumber - 14, '@troytech.local'),
         UPPER(CONCAT('applicant', @applicantNumber - 14, '@troytech.local')),
         CONCAT('applicant', @applicantNumber - 14, '@troytech.local'),
         UPPER(CONCAT('applicant', @applicantNumber - 14, '@troytech.local')),
         0,
         'AQAAAAIAAYagAAAAEIK/kzjtE3fN/N8M5sEI59xQycXxVp2U/7iayV9dBsinM5sbkxGVz9KVRafmFj2ZfA==',
         CONCAT('applicant-', @applicantNumber - 14, '-stamp'),
         CONCAT('applicant-', @applicantNumber - 14, '-concurrency'),
         CONCAT('555-300-', RIGHT(CONCAT('0000', @applicantNumber), 4)),
         0, 0, NULL, 0, 0);

    INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
    VALUES (@userId, '00000000-0000-0000-0000-000000000002');

    SET @applicantNumber += 1;
END;
END;
");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM [Properties] WHERE [Id] = 1)
BEGIN
SET IDENTITY_INSERT [Properties] ON;
INSERT INTO [Properties] ([Id], [Name], [StreetAddress], [City], [State], [ZipCode])
VALUES
    (1, 'Oak Grove Apartments', '101 Oak Grove Drive', 'Seattle', 'WA', '98101'),
    (2, 'Maple Terrace', '220 Maple Avenue', 'Tacoma', 'WA', '98402'),
    (3, 'Pine Crest Homes', '88 Pine Crest Lane', 'Bellevue', 'WA', '98004'),
    (4, 'Cedar Valley Residences', '410 Cedar Valley Road', 'Redmond', 'WA', '98052'),
    (5, 'Riverstone Commons', '725 Riverstone Boulevard', 'Kirkland', 'WA', '98033');
SET IDENTITY_INSERT [Properties] OFF;
END;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [ManagerProperties] WHERE [ManagerId] = '10000000-0000-0000-0000-000000000002' AND [PropertyId] = 5)
BEGIN
INSERT INTO [ManagerProperties] ([ManagerId], [PropertyId])
VALUES
    ('10000000-0000-0000-0000-000000000002', 1),
    ('10000000-0000-0000-0000-000000000002', 2),
    ('10000000-0000-0000-0000-000000000002', 4),
    ('10000000-0000-0000-0000-000000000003', 2),
    ('10000000-0000-0000-0000-000000000003', 3),
    ('10000000-0000-0000-0000-000000000003', 5);
END;");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM [Units] WHERE [Id] = 1)
BEGIN
SET IDENTITY_INSERT [Units] ON;
INSERT INTO [Units] ([Id], [Number], [Bedrooms], [MonthlyRent], [PropertyId], [UnitTypeId])
VALUES
    (1, '101', 1, 1650.00, 1, 1),
    (2, '102', 2, 2100.00, 1, 1),
    (3, '201', 1, 1750.00, 2, 1),
    (4, '202', 2, 2250.00, 2, 1),
    (5, '301', 3, 2800.00, 3, 1),
    (6, '302', 2, 2450.00, 3, 1),
    (7, '103', 1, 1700.00, 1, 2),
    (8, '203', 2, 2190.00, 2, 2),
    (9, '201', 0, 1500.00, 4, 1),
    (10, '202', 1, 1625.00, 5, 2),
    (11, '203', 2, 1750.00, 1, 3),
    (12, '204', 3, 1875.00, 2, 4),
    (13, '205', 0, 2000.00, 3, 1),
    (14, '301', 1, 2125.00, 4, 2),
    (15, '302', 2, 2250.00, 5, 3),
    (16, '303', 3, 2375.00, 1, 4),
    (17, '304', 0, 2500.00, 2, 1),
    (18, '305', 1, 2625.00, 3, 2),
    (19, '401', 2, 2750.00, 4, 3),
    (20, '402', 3, 2875.00, 5, 4),
    (21, '403', 0, 3000.00, 1, 1),
    (22, '404', 1, 3125.00, 2, 2),
    (23, '405', 2, 3250.00, 3, 3),
    (24, '501', 3, 3375.00, 4, 4),
    (25, '502', 0, 1500.00, 5, 1),
    (26, '503', 1, 1625.00, 1, 2),
    (27, '504', 2, 1750.00, 2, 3),
    (28, '505', 3, 1875.00, 3, 4),
    (29, '601', 0, 2000.00, 4, 1),
    (30, '602', 1, 2125.00, 5, 2),
    (31, '603', 2, 2250.00, 1, 3),
    (32, '604', 3, 2375.00, 2, 4),
    (33, '605', 0, 2500.00, 3, 1),
    (34, '701', 1, 2625.00, 4, 2),
    (35, '702', 2, 2750.00, 5, 3),
    (36, '703', 3, 2875.00, 1, 4),
    (37, '704', 0, 3000.00, 2, 1),
    (38, '705', 1, 3125.00, 3, 2),
    (39, '801', 2, 3250.00, 4, 3),
    (40, '802', 3, 3375.00, 5, 4),
    (41, '803', 0, 1500.00, 1, 1),
    (42, '804', 1, 1625.00, 2, 2),
    (43, '805', 2, 1750.00, 3, 3),
    (44, '901', 3, 1875.00, 4, 4),
    (45, '902', 0, 2000.00, 5, 1),
    (46, '903', 1, 2125.00, 1, 2),
    (47, '904', 2, 2250.00, 2, 3),
    (48, '905', 3, 2375.00, 3, 4),
    (49, '1001', 0, 2500.00, 4, 1),
    (50, '1002', 1, 2625.00, 5, 2),
    (51, '1003', 2, 2750.00, 1, 3),
    (52, '1004', 3, 2875.00, 2, 4),
    (53, '1005', 0, 3000.00, 3, 1),
    (54, '1101', 1, 3125.00, 4, 2),
    (55, '1102', 2, 3250.00, 5, 3),
    (56, '1103', 3, 3375.00, 1, 4),
    (57, '1104', 0, 1500.00, 2, 1),
    (58, '1105', 1, 1625.00, 3, 2),
    (59, '1201', 2, 1750.00, 4, 3),
    (60, '1202', 3, 1875.00, 5, 4),
    (61, '1203', 0, 2000.00, 1, 1),
    (62, '1204', 1, 2125.00, 2, 2),
    (63, '1205', 2, 2250.00, 3, 3),
    (64, '1301', 3, 2375.00, 4, 4),
    (65, '1302', 0, 2500.00, 5, 1),
    (66, '1303', 1, 2625.00, 1, 2),
    (67, '1304', 2, 2750.00, 2, 3),
    (68, '1305', 3, 2875.00, 3, 4),
    (69, '1401', 0, 3000.00, 4, 1),
    (70, '1402', 1, 3125.00, 5, 2),
    (71, '1403', 2, 3250.00, 1, 3),
    (72, '1404', 3, 3375.00, 2, 4),
    (73, '1405', 0, 1500.00, 3, 1),
    (74, '1501', 1, 1625.00, 4, 2),
    (75, '1502', 2, 1750.00, 5, 3),
    (76, '1503', 3, 1875.00, 1, 4),
    (77, '1504', 0, 2000.00, 2, 1),
    (78, '1505', 1, 2125.00, 3, 2),
    (79, '1601', 2, 2250.00, 4, 3),
    (80, '1602', 3, 2375.00, 5, 4);
SET IDENTITY_INSERT [Units] OFF;
END;");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM [Applications] WHERE [Id] = 1)
BEGIN
SET IDENTITY_INSERT [Applications] ON;
INSERT INTO [Applications] ([Id], [FirstName], [LastName], [Email], [PhoneNumber], [CurrentStreetAddress], [CurrentCity], [CurrentState], [CurrentZipCode], [UserId], [UnitId], [ApplicationStatusId])
VALUES
    (1, 'Ella', 'Brown', 'applicant1@troytech.local', '555-200-2001', '1400 Pine Street', 'Seattle', 'WA', '98101', '10000000-0000-0000-0000-000000000011', 1, 1),
    (2, 'Lucas', 'Chen', 'applicant2@troytech.local', '555-200-2002', '3600 Alder Way', 'Seattle', 'WA', '98104', '10000000-0000-0000-0000-000000000012', 2, 2),
    (3, 'Mia', 'Rivera', 'applicant3@troytech.local', '555-200-2003', '200 Lakeview Blvd', 'Tacoma', 'WA', '98405', '10000000-0000-0000-0000-000000000013', 3, 3),
    (4, 'Owen', 'Martin', 'applicant4@troytech.local', '555-200-2004', '780 Harbor Rd', 'Bellevue', 'WA', '98007', '10000000-0000-0000-0000-000000000014', 4, 4),
    (5, 'Ella', 'Brown', 'applicant1@troytech.local', '555-200-2001', '1400 Pine Street', 'Seattle', 'WA', '98101', '10000000-0000-0000-0000-000000000011', 5, 5),
    (6, 'Lucas', 'Chen', 'applicant2@troytech.local', '555-200-2002', '3600 Alder Way', 'Seattle', 'WA', '98104', '10000000-0000-0000-0000-000000000012', 6, 6);
SET IDENTITY_INSERT [Applications] OFF;
END;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Applications] WHERE [Id] = 60)
BEGIN
SET IDENTITY_INSERT [Applications] ON;
DECLARE @applicationId int = 7;
DECLARE @userId uniqueidentifier;
WHILE @applicationId <= 60
BEGIN
    SET @userId = CONVERT(uniqueidentifier, CONCAT(
        '10000000-0000-0000-0000-',
        RIGHT(CONCAT('000000000000', @applicationId + 12), 12)));

    INSERT INTO [Applications]
        ([Id], [FirstName], [LastName], [Email], [PhoneNumber],
         [CurrentStreetAddress], [CurrentCity], [CurrentState],
         [CurrentZipCode], [UserId], [UnitId], [ApplicationStatusId])
    VALUES
        (@applicationId,
         CONCAT('Applicant', @applicationId - 2), 'Generated',
         CONCAT('applicant', @applicationId - 2, '@troytech.local'),
         CONCAT('555-300-', RIGHT(CONCAT('0000', @applicationId + 12), 4)),
         CONCAT(100 + @applicationId, ' Cedar Street'),
         CASE ((@applicationId - 7) % 5)
             WHEN 0 THEN 'Seattle'
             WHEN 1 THEN 'Tacoma'
             WHEN 2 THEN 'Bellevue'
             WHEN 3 THEN 'Redmond'
             ELSE 'Kirkland'
         END,
         'WA',
         CASE ((@applicationId - 7) % 5)
             WHEN 0 THEN '98101'
             WHEN 1 THEN '98402'
             WHEN 2 THEN '98004'
             WHEN 3 THEN '98052'
             ELSE '98033'
         END,
         @userId,
         @applicationId + 2,
         ((@applicationId - 7) % 6) + 1);

    SET @applicationId += 1;
END;
SET IDENTITY_INSERT [Applications] OFF;
END;
");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM [ApplicationResidenceHistory] WHERE [Id] = 1)
BEGIN
SET IDENTITY_INSERT [ApplicationResidenceHistory] ON;
INSERT INTO [ApplicationResidenceHistory] ([Id], [ApplicationId], [StreetAddress], [City], [State], [ZipCode], [LandlordFirstName], [LandlordLastName], [LandlordPhoneNumber], [MoveInDate], [MoveOutDate])
VALUES
    (1, 1, '500 Cedar Avenue', 'Spokane', 'WA', '99201', 'Harold', 'Peters', '555-111-1111', '2023-01-15', '2024-12-31'),
    (2, 2, '1200 Birch Court', 'Kent', 'WA', '98031', 'Carmen', 'Nguyen', '555-111-2222', '2022-03-01', '2024-08-15'),
    (3, 3, '812 Willow Road', 'Redmond', 'WA', '98052', 'Kevin', 'Ross', '555-111-3333', '2021-06-01', '2024-11-30'),
    (4, 4, '1800 Poplar Street', 'Renton', 'WA', '98057', 'Julia', 'Stone', '555-111-4444', '2022-08-01', '2024-10-31'),
    (5, 5, '950 Ash Lane', 'Olympia', 'WA', '98501', 'Rafael', 'Mendoza', '555-111-5555', '2020-02-10', '2023-04-01'),
    (6, 6, '1177 Spruce Place', 'Bellingham', 'WA', '98225', 'Lena', 'Foster', '555-111-6666', '2021-09-01', '2023-10-01');
SET IDENTITY_INSERT [ApplicationResidenceHistory] OFF;
END;");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM [ApplicationStatusHistory] WHERE [Id] = 1)
BEGIN
SET IDENTITY_INSERT [ApplicationStatusHistory] ON;
INSERT INTO [ApplicationStatusHistory] ([Id], [ApplicationId], [ApplicationStatusId], [ChangedByUserId], [ChangedAtUtc], [Comment])
VALUES
    (1, 1, 1, '10000000-0000-0000-0000-000000000011', '2026-01-10T12:00:00Z', 'Started application draft.'),
    (2, 2, 2, '10000000-0000-0000-0000-000000000012', '2026-01-11T09:30:00Z', 'Submitted application for review.'),
    (3, 3, 3, '10000000-0000-0000-0000-000000000002', '2026-01-12T15:10:00Z', 'Returned application for missing residence documents.'),
    (4, 4, 4, '10000000-0000-0000-0000-000000000002', '2026-01-13T11:45:00Z', 'Approved and issued lease.'),
    (5, 5, 5, '10000000-0000-0000-0000-000000000003', '2026-01-14T17:05:00Z', 'Application denied due to insufficient income verification.'),
    (6, 6, 6, '10000000-0000-0000-0000-000000000012', '2026-01-15T08:15:00Z', 'Applicant withdrew the application.');
SET IDENTITY_INSERT [ApplicationStatusHistory] OFF;
END;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [ApplicationStatusHistory] WHERE [Id] = 60)
BEGIN
SET IDENTITY_INSERT [ApplicationStatusHistory] ON;
DECLARE @historyId int = 7;
DECLARE @userId uniqueidentifier;
WHILE @historyId <= 60
BEGIN
    SET @userId = CONVERT(uniqueidentifier, CONCAT(
        '10000000-0000-0000-0000-',
        RIGHT(CONCAT('000000000000', @historyId + 12), 12)));

    INSERT INTO [ApplicationStatusHistory]
        ([Id], [ApplicationId], [ApplicationStatusId], [ChangedByUserId],
         [ChangedAtUtc], [Comment])
    VALUES
        (@historyId, @historyId, ((@historyId - 7) % 6) + 1,
         @userId,
         DATEADD(day, @historyId - 7, '2026-01-16T08:15:00Z'),
         'Seeded application status history.');

    SET @historyId += 1;
END;
SET IDENTITY_INSERT [ApplicationStatusHistory] OFF;
END;
");

            migrationBuilder.Sql(@"IF NOT EXISTS (SELECT 1 FROM [Leases] WHERE [Id] = 1)
BEGIN
SET IDENTITY_INSERT [Leases] ON;
INSERT INTO [Leases] ([Id], [UnitId], [ApplicationId], [StartDate], [EndDate], [CreatedAtUtc])
VALUES
    (1, 4, 4, '2026-02-01', '2027-01-31', '2026-01-13T11:50:00Z');
SET IDENTITY_INSERT [Leases] OFF;
END;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Leases] WHERE [Id] = 10)
BEGIN
SET IDENTITY_INSERT [Leases] ON;
DECLARE @leaseId int = 2;
WHILE @leaseId <= 10
BEGIN
    INSERT INTO [Leases]
        ([Id], [UnitId], [ApplicationId], [StartDate], [EndDate], [CreatedAtUtc])
    VALUES
        (@leaseId, @leaseId + 7, @leaseId + 5,
         DATEADD(month, @leaseId - 2, '2026-02-01'),
         DATEADD(month, @leaseId - 2, '2027-01-31'),
         DATEADD(day, @leaseId - 2, '2026-01-13T11:50:00Z'));

    SET @leaseId += 1;
END;
SET IDENTITY_INSERT [Leases] OFF;
END;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [Leases] WHERE [Id] BETWEEN 1 AND 10;");
            migrationBuilder.Sql("DELETE FROM [ApplicationStatusHistory] WHERE [Id] IN (1,2,3,4,5,6);");
            migrationBuilder.Sql("DELETE FROM [ApplicationResidenceHistory] WHERE [Id] IN (1,2,3,4,5,6);");
            migrationBuilder.Sql("DELETE FROM [Applications] WHERE [Id] BETWEEN 1 AND 60;");
            migrationBuilder.Sql("DELETE FROM [Units] WHERE [Id] BETWEEN 1 AND 80;");
            migrationBuilder.Sql("DELETE FROM [ManagerProperties] WHERE [ManagerId] IN ('10000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000003');");
            migrationBuilder.Sql("DELETE FROM [Properties] WHERE [Id] IN (1,2,3,4,5);");
            migrationBuilder.Sql(@"DELETE FROM [AspNetUserRoles]
WHERE [UserId] IN (
    '10000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000003')
    OR [UserId] IN (
        SELECT [Id] FROM [AspNetUsers]
        WHERE [UserName] LIKE 'applicant%@troytech.local');");
            migrationBuilder.Sql(@"DELETE FROM [AspNetUsers]
WHERE [Id] IN (
    '10000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000003')
    OR ([UserName] LIKE 'applicant%@troytech.local');");
        }
    }
}
