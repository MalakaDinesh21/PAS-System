using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PAS.ViewModels;

namespace PAS.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private static readonly string[] AllowedRoles = { "Student", "Supervisor", "Admin" };

    private readonly UserManager<IdentityUser> _userManager;

    public AdminController(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Users()
    {
        var users = _userManager.Users.ToList();
        var items = new List<AdminUserListItemViewModel>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new AdminUserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? user.Id,
                Roles = roles.OrderBy(r => r).ToArray(),
            });
        }

        return View(items.OrderBy(u => u.Email));
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Roles = AllowedRoles;
        return View(new AdminCreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCreateUserViewModel model)
    {
        if (!AllowedRoles.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Invalid role.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Roles = AllowedRoles;
            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.Roles = AllowedRoles;
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.Role);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        if (string.IsNullOrWhiteSpace(id) || !AllowedRoles.Contains(role))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        await _userManager.AddToRoleAsync(user, role);
        return RedirectToAction(nameof(Users));
    }
}
