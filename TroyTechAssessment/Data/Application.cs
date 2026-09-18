using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TroyTechAssessment.Data;

public class Application{
    [Key] public int Id  { get; set; }
    [Required] public string FirstName { get; set; } = string.Empty;
    [Required] public string LastName { get; set; } = string.Empty;
    [Required] public string Email { get; set; } = string.Empty;
    [Required, StringLength(25)] public string PhoneNumber { get; set; } = string.Empty;
    [Required] public string CurrentStreetAddress { get; set; } = string.Empty;
    [Required] public string CurrentCity { get; set; } = string.Empty;
    [Required] public string CurrentState { get; set; } = string.Empty;
    [Required] public string CurrentZipCode { get; set; } = string.Empty;
    [Required] public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))] public ApplicationUser User { get; set; } = null!;
    [Required] public int UnitId { get; set; }
    [ForeignKey(nameof(UnitId))] public Unit Unit { get; set; } = null!;
    [Required] public int ApplicationStatusId { get; set; }
    [ForeignKey(nameof(ApplicationStatusId))]
    public ApplicationStatus ApplicationStatus { get; set; } = null!;
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
    public Guid? ClaimedByManagerId { get; set; }
    [ForeignKey(nameof(ClaimedByManagerId))]
    public ApplicationUser? ClaimedByManager { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    public string? DraftResidenceHistoryJson { get; set; }
    public DateTime LastModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ApplicationResidenceHistory> ApplicationResidenceHistory { get; set; } = [];
    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<ManagerNote> ManagerNotes { get; set; } = [];
    public Lease? Lease { get; set; }
}