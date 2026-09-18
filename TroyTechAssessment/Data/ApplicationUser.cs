using Microsoft.AspNetCore.Identity;

namespace TroyTechAssessment.Data;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public ICollection<ManagerProperty> ManagerProperties { get; set; } = [];
    public ICollection<ManagerNote> ManagerNotes { get; set; } = [];
}