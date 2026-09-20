using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TroyTechAssessment.Data;

public class ApplicationApplicant
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    [ForeignKey(nameof(ApplicationId))]
    public Application Application { get; set; } = null!;

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(25)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string CurrentStreetAddress { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string CurrentCity { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string CurrentState { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string CurrentZipCode { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}
