using System.ComponentModel.DataAnnotations;

namespace TroyTechAssessment.Data;

public class UnitType{
    [Key] public int Id { get; set; }
    [Required]
    [StringLength(50)]
    public string UnitTypeName{ get; set; } = string.Empty;

    [Required]
    public bool Active { get; set; }
}