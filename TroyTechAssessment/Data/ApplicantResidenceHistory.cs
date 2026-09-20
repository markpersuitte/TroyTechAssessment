using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TroyTechAssessment.Data;

public class ApplicationResidenceHistory{
    [Key] public int Id  { get; set; }
    [Required] public int ApplicationId { get; set; }
    [ForeignKey(nameof(ApplicationId))] public Application Application{ get; set; } = null!;
    [Required] public int ApplicantId { get; set; }
    [ForeignKey(nameof(ApplicantId))] public ApplicationApplicant Applicant { get; set; } = null!;
    [MaxLength(100)]
    [Required] public string StreetAddress { get; set; } = string.Empty;
    [MaxLength(50)]
    [Required] public string City { get; set; } = string.Empty;
    [MaxLength(50)]
    [Required] public string State { get; set; } = string.Empty;
    [MaxLength(10)]
    [Required] public string ZipCode { get; set; } = string.Empty;
    [MaxLength(50)]
    [Required] public string LandlordFirstName { get; set; } = string.Empty;
    [MaxLength(50)]
    [Required] public string LandlordLastName { get; set; } = string.Empty;
    [MaxLength(50)]
    [Required] public string LandlordPhoneNumber { get; set; } = string.Empty;
    [Column(TypeName = "Date")]
    [Required] public DateTime MoveInDate { get; set; }
    [Column(TypeName = "Date")]
    public DateTime? MoveOutDate{ get; set; }

    public static bool HasValidDateRange(DateTime moveInDate, DateTime? moveOutDate)
    {
        return !moveOutDate.HasValue || moveOutDate.Value.Date >= moveInDate.Date;
    }

    public bool Overlaps(ApplicationResidenceHistory other)
    {
        var thisEnd = MoveOutDate ?? DateTime.MaxValue;
        var otherEnd = other.MoveOutDate ?? DateTime.MaxValue;
        return MoveInDate.Date <= otherEnd.Date && other.MoveInDate.Date <= thisEnd.Date;
    }
}