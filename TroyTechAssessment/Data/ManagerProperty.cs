using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TroyTechAssessment.Data;

public class ManagerProperty
{
    [Required]
    public Guid ManagerId { get; set; }

    [ForeignKey(nameof(ManagerId))]
    public ApplicationUser Manager { get; set; } = null!;

    [Required]
    public int PropertyId { get; set; }

    [ForeignKey(nameof(PropertyId))]
    public Property Property { get; set; } = null!;
}
