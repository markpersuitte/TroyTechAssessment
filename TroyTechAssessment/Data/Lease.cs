using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TroyTechAssessment.Data;

public class Lease
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UnitId { get; set; }

    [ForeignKey(nameof(UnitId))]
    public Unit Unit { get; set; } = null!;

    [Required]
    public int ApplicationId { get; set; }

    [ForeignKey(nameof(ApplicationId))]
    public Application Application { get; set; } = null!;

    [Required]
    [Column(TypeName = "Date")]
    public DateTime StartDate { get; set; }

    [Required]
    [Column(TypeName = "Date")]
    public DateTime EndDate { get; set; }

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool CoversDate(DateTime date)
    {
        return date >= StartDate && date <= EndDate;
    }

    public bool Overlaps(DateTime startDate, DateTime endDate)
    {
        return StartDate <= endDate && EndDate >= startDate;
    }

    public static DateTime CalculateEndDate(DateTime startDate)
    {
        return startDate.Date.AddYears(1).AddDays(-1);
    }
}
