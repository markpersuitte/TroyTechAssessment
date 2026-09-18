using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TroyTechAssessment.Data;

public class ApplicationStatusHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    [ForeignKey(nameof(ApplicationId))]
    public Application Application { get; set; } = null!;

    [Required]
    public int ApplicationStatusId { get; set; }

    [ForeignKey(nameof(ApplicationStatusId))]
    public ApplicationStatus ApplicationStatus { get; set; } = null!;

    [Required]
    public Guid ChangedByUserId { get; set; }

    [ForeignKey(nameof(ChangedByUserId))]
    public ApplicationUser ChangedByUser { get; set; } = null!;

    [Required]
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    [StringLength(1000)]
    public string? Comment { get; set; }
}
