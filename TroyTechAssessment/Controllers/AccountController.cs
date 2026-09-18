using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TroyTechAssessment.Data;

namespace TroyTechAssessment.Controllers;

public class AccountController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        return View(new LoginInput());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (!ModelState.IsValid)
            return View(input);

        var result = await signInManager.PasswordSignInAsync(
            input.Email,
            input.Password,
            input.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(input.Password),
                "The email address or password is incorrect. Check both values and try again.");
            return View(input);
        }

        logger.LogInformation("User {Email} logged in.", input.Email);
        return RedirectToPage("/Index");
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        return View(await CreateRegisterViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([Bind(Prefix = "Input")] RegisterInput input)
    {
        var model = await CreateRegisterViewModelAsync(input);
        if (!ModelState.IsValid)
            return View(model);

        if (!Guid.TryParse(input.RoleId, out var roleId))
        {
            ModelState.AddModelError(nameof(input.RoleId), "Select either Applicant or Manager.");
            return View(model);
        }

        var selectedRole = await roleManager.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(role => role.Id == roleId);
        if (selectedRole?.Name is not ("Applicant" or "Manager"))
        {
            ModelState.AddModelError(nameof(input.RoleId), "Select either Applicant or Manager.");
            return View(model);
        }

        var selectedPropertyIds = input.SelectedPropertyIds.Distinct().ToArray();
        if (selectedRole.Name == "Manager")
        {
            if (selectedPropertyIds.Length == 0)
            {
                ModelState.AddModelError(nameof(input.SelectedPropertyIds),
                    "Select at least one property for this manager.");
                return View(model);
            }

            var validPropertyCount = await db.Properties
                .CountAsync(property => selectedPropertyIds.Contains(property.Id));
            if (validPropertyCount != selectedPropertyIds.Length)
            {
                ModelState.AddModelError(nameof(input.SelectedPropertyIds),
                    "One or more selected properties is no longer available. Select only listed properties.");
                return View(model);
            }
        }

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            FirstName = input.FirstName,
            LastName = input.LastName
        };
        var result = await userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            AddRegistrationErrors(result);
            return View(model);
        }

        var roleResult = await userManager.AddToRoleAsync(user, selectedRole.Name);
        if (!roleResult.Succeeded)
        {
            AddErrors(roleResult);
            await userManager.DeleteAsync(user);
            return View(model);
        }

        if (selectedRole.Name == "Manager")
        {
            db.ManagerProperties.AddRange(selectedPropertyIds.Select(propertyId => new ManagerProperty
            {
                ManagerId = user.Id,
                PropertyId = propertyId
            }));
            await db.SaveChangesAsync();
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToPage("/Index");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        return View(await CreateEditViewModelAsync(user));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind(Prefix = "Input")] EditInput input)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        var model = await CreateEditViewModelAsync(user, input);
        var changingPassword = !string.IsNullOrWhiteSpace(input.CurrentPassword)
            || !string.IsNullOrWhiteSpace(input.NewPassword)
            || !string.IsNullOrWhiteSpace(input.ConfirmNewPassword);

        if (changingPassword)
        {
            if (string.IsNullOrWhiteSpace(input.CurrentPassword))
                ModelState.AddModelError(nameof(input.CurrentPassword), "Current password is required.");
            if (string.IsNullOrWhiteSpace(input.NewPassword))
                ModelState.AddModelError(nameof(input.NewPassword), "New password is required.");
            if (input.NewPassword != input.ConfirmNewPassword)
                ModelState.AddModelError(nameof(input.ConfirmNewPassword), "The new passwords do not match.");
        }

        if (model.IsManager)
        {
            var selectedPropertyIds = input.SelectedPropertyIds.Distinct().ToArray();
            var validPropertyCount = await db.Properties
                .CountAsync(property => selectedPropertyIds.Contains(property.Id));
            if (selectedPropertyIds.Length == 0 || validPropertyCount != selectedPropertyIds.Length)
                ModelState.AddModelError(nameof(input.SelectedPropertyIds), "Select at least one valid property.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var existingUser = await userManager.FindByEmailAsync(input.Email);
        if (existingUser is not null && existingUser.Id != user.Id)
        {
            ModelState.AddModelError(nameof(input.Email), "That email address is already in use.");
            return View(model);
        }

        user.FirstName = input.FirstName;
        user.LastName = input.LastName;

        if (changingPassword)
        {
            var passwordResult = await userManager.ChangePasswordAsync(
                user, input.CurrentPassword!, input.NewPassword!);
            if (!passwordResult.Succeeded)
            {
                AddErrors(passwordResult);
                return View(model);
            }
        }

        var emailResult = await userManager.SetEmailAsync(user, input.Email);
        if (!emailResult.Succeeded)
        {
            AddErrors(emailResult);
            return View(model);
        }

        var usernameResult = await userManager.SetUserNameAsync(user, input.Email);
        if (!usernameResult.Succeeded)
        {
            AddErrors(usernameResult);
            return View(model);
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            AddErrors(updateResult);
            return View(model);
        }

        if (model.IsManager)
        {
            var assignments = await db.ManagerProperties
                .Where(item => item.ManagerId == user.Id)
                .ToListAsync();
            db.ManagerProperties.RemoveRange(assignments);
            db.ManagerProperties.AddRange(input.SelectedPropertyIds
                .Distinct()
                .Select(propertyId => new ManagerProperty
                {
                    ManagerId = user.Id,
                    PropertyId = propertyId
                }));
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Edit));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }

    private async Task<RegisterViewModel> CreateRegisterViewModelAsync(RegisterInput? input = null)
    {
        return new RegisterViewModel
        {
            Input = input ?? new RegisterInput(),
            Roles = await roleManager.Roles
                .AsNoTracking()
                .Where(role => role.Name == "Applicant" || role.Name == "Manager")
                .OrderBy(role => role.Name)
                .Select(role => new SelectListItem(role.Name!, role.Id.ToString()))
                .ToListAsync(),
            Properties = await db.Properties
                .AsNoTracking()
                .OrderBy(property => property.Name)
                .ToListAsync()
        };
    }

    private async Task<EditViewModel> CreateEditViewModelAsync(ApplicationUser user, EditInput? input = null)
    {
        var isManager = await userManager.IsInRoleAsync(user, "Manager");
        return new EditViewModel
        {
            Input = input ?? new EditInput
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                SelectedPropertyIds = await db.ManagerProperties
                    .Where(item => item.ManagerId == user.Id)
                    .Select(item => item.PropertyId)
                    .ToListAsync()
            },
            IsManager = isManager,
            Properties = isManager
                ? await db.Properties.AsNoTracking().OrderBy(property => property.Name).ToListAsync()
                : []
        };
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
    }

    private void AddRegistrationErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            if (string.Equals(error.Code, "DuplicateUserName", StringComparison.Ordinal))
                continue;

            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    public sealed class LoginInput
    {
        [Required, EmailAddress]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public sealed class RegisterInput
    {
        [Required, StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;
        [Required, StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;
        [Required, EmailAddress, StringLength(256)]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        [Required, Compare(nameof(Password)), DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
        [Required]
        public string RoleId { get; set; } = string.Empty;
        public List<int> SelectedPropertyIds { get; set; } = [];
    }

    public sealed class EditInput
    {
        [Required, StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;
        [Required, StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;
        [Required, EmailAddress, StringLength(256)]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string? CurrentPassword { get; set; }
        [DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
        [Display(Name = "New password")]
        public string? NewPassword { get; set; }
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        public string? ConfirmNewPassword { get; set; }
        public List<int> SelectedPropertyIds { get; set; } = [];
    }

    public sealed class RegisterViewModel
    {
        public RegisterInput Input { get; init; } = new();
        public IList<SelectListItem> Roles { get; init; } = [];
        public IList<Property> Properties { get; init; } = [];
    }

    public sealed class EditViewModel
    {
        public EditInput Input { get; init; } = new();
        public IList<Property> Properties { get; init; } = [];
        public bool IsManager { get; init; }
    }
}
