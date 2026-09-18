using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TroyTechAssessment.Data
{
    public class ApplicationDbContext : IdentityDbContext<
        ApplicationUser,
        IdentityRole<Guid>,
        Guid,
        IdentityUserClaim<Guid>,
        IdentityUserRole<Guid>,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>,
        IdentityUserPasskey<Guid>>
    {
        // Define your database tables here using DbSet
        public DbSet<Property> Properties { get; set; }
        public DbSet<UnitType> UnitTypes { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Application> Applications  { get; set; }
        public DbSet<ApplicationResidenceHistory> ApplicationResidenceHistory { get; set; }
        public DbSet<ApplicationStatus> ApplicationStatuses { get; set; }
        public DbSet<ApplicationStatusHistory> ApplicationStatusHistory { get; set; }
        public DbSet<Lease> Leases { get; set; }
        public DbSet<ManagerProperty> ManagerProperties { get; set; }
        public DbSet<ManagerNote> ManagerNotes { get; set; }
        
        // The constructor passes configuration options to the base DbContext class
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options){
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdentityRole<Guid>>().HasData(
                new IdentityRole<Guid>
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Name = "Manager",
                    NormalizedName = "MANAGER",
                    ConcurrencyStamp = "manager-role-concurrency"
                },
                new IdentityRole<Guid>
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    Name = "Applicant",
                    NormalizedName = "APPLICANT",
                    ConcurrencyStamp = "applicant-role-concurrency"
                });

            modelBuilder.Entity<Application>()
                .HasOne(application => application.ApplicationStatus)
                .WithMany()
                .HasForeignKey(application => application.ApplicationStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Application>()
                .HasOne(application => application.Unit)
                .WithMany()
                .HasForeignKey(application => application.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Application>()
                .HasOne(application => application.ClaimedByManager)
                .WithMany()
                .HasForeignKey(application => application.ClaimedByManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Application>()
                .HasMany(application => application.StatusHistory)
                .WithOne(statusHistory => statusHistory.Application)
                .HasForeignKey(statusHistory => statusHistory.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Application>()
                .HasOne(application => application.Lease)
                .WithOne(lease => lease.Application)
                .HasForeignKey<Lease>(lease => lease.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationStatusHistory>()
                .HasOne(statusHistory => statusHistory.ApplicationStatus)
                .WithMany()
                .HasForeignKey(statusHistory => statusHistory.ApplicationStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ManagerNote>()
                .HasOne(note => note.Application)
                .WithMany(application => application.ManagerNotes)
                .HasForeignKey(note => note.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManagerNote>()
                .HasOne(note => note.Manager)
                .WithMany(user => user.ManagerNotes)
                .HasForeignKey(note => note.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationStatusHistory>()
                .HasOne(statusHistory => statusHistory.ChangedByUser)
                .WithMany()
                .HasForeignKey(statusHistory => statusHistory.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Unit>()
                .HasOne(unit => unit.Property)
                .WithMany(property => property.Units)
                .HasForeignKey(unit => unit.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Unit>()
                .HasOne(unit => unit.UnitType)
                .WithMany()
                .HasForeignKey(unit => unit.UnitTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Unit>()
                .HasMany(unit => unit.Leases)
                .WithOne(lease => lease.Unit)
                .HasForeignKey(lease => lease.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lease>()
                .HasOne(lease => lease.Unit)
                .WithMany(unit => unit.Leases)
                .HasForeignKey(lease => lease.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<UnitType>().HasData(
                new UnitType { Id = 1, UnitTypeName = "Type A", Active = true },
                new UnitType { Id = 2, UnitTypeName = "Type B", Active = true },
                new UnitType { Id = 3, UnitTypeName = "Type C", Active = true },
                new UnitType { Id = 4, UnitTypeName = "Type D", Active = true }
            );

            modelBuilder.Entity<ManagerProperty>()
                .HasKey(managerProperty => new { managerProperty.ManagerId, managerProperty.PropertyId });

            modelBuilder.Entity<ManagerProperty>()
                .HasOne(managerProperty => managerProperty.Manager)
                .WithMany(user => user.ManagerProperties)
                .HasForeignKey(managerProperty => managerProperty.ManagerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ManagerProperty>()
                .HasOne(managerProperty => managerProperty.Property)
                .WithMany(property => property.ManagerProperties)
                .HasForeignKey(managerProperty => managerProperty.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationStatus>().HasData(
                new ApplicationStatus { Id = 1, Status = "Draft" },
                new ApplicationStatus { Id = 2, Status = "Submitted" },
                new ApplicationStatus { Id = 3, Status = "Returned" },
                new ApplicationStatus { Id = 4, Status = "Approved" },
                new ApplicationStatus { Id = 5, Status = "Denied" },
                new ApplicationStatus { Id = 6, Status = "Withdrawn" }
            );

        }
    }
}