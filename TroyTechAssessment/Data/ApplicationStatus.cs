using System.ComponentModel.DataAnnotations;

namespace TroyTechAssessment.Data;

public class ApplicationStatus
{
    [Key] public int Id { get; set; }
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = string.Empty;
}
