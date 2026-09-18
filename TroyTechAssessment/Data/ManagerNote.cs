using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TroyTechAssessment.Data;

public class ManagerNote
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    [ForeignKey(nameof(ApplicationId))]
    public Application Application { get; set; } = null!;

    [Required]
    public Guid ManagerId { get; set; }

    [ForeignKey(nameof(ManagerId))]
    public ApplicationUser Manager { get; set; } = null!;

    [Required, StringLength(4000)]
    public string Notes { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}
