using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace TroyTechAssessment.Data;

public class Unit{
    public static int[] BedroomOptions{
        get{
            if (field == null){
                PropertyInfo? propInfo = typeof(Unit).GetProperty(nameof(Unit.Bedrooms));
                var allowedValuesAttr = propInfo?.GetCustomAttribute<AllowedValuesAttribute>();
                field = allowedValuesAttr?.Values.Select(v => Convert.ToInt32(v)).OrderBy(v => v).ToArray() ?? [0,1,2,3];
            }
            return field;
        }
    } = null;

    [Key] public int Id  { get; set; }
    [MaxLength(10)]
    [Required] public string Number { get; set; } = string.Empty;
    [Required]
    [AllowedValues(0,1,2,3)]
    public int Bedrooms { get; set; }
    [Required] public double MonthlyRent { get; set; }

    [Required] public int PropertyId { get; set; }
    [ForeignKey(nameof(PropertyId))] public Property Property { get; set; } = null!;

    [Required] public int UnitTypeId { get; set; }
    [ForeignKey(nameof(UnitTypeId))] public UnitType UnitType{ get; set; } = null!;

    public ICollection<Lease> Leases { get; set; } = [];

}