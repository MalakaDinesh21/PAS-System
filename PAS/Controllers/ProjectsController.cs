using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAS.Data;
using PAS.Models;
using PAS.ViewModels;
using System.Security.Claims;

namespace PAS.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Projects (Student)
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var projects = await _context.Projects
                .AsNoTracking()
                .Where(p => p.StudentId == userId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(projects);
        }

        // GET: Projects/Review (Supervisor)
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> Review()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(supervisorId))
            {
                return Challenge();
            }

            var interestedProjectIds = await _context.ProjectInterests
                .AsNoTracking()
                .Where(i => i.SupervisorId == supervisorId)
                .Select(i => i.ProjectId)
                .ToListAsync();

            var projects = await _context.Projects
                .AsNoTracking()
                .Where(p => p.Status != "Matched")
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            var model = projects.Select(p => new SupervisorReviewItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                ResearchArea = p.ResearchArea,
                TechStack = p.TechStack,
                Status = p.Status,
                HasExpressedInterest = interestedProjectIds.Contains(p.Id),
            }).ToList();

            return View(model);
        }

        // POST: Projects/ExpressInterest/5 (Supervisor)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> ExpressInterest(int id)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(supervisorId))
            {
                return Challenge();
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project is null)
            {
                return NotFound();
            }

            if (string.Equals(project.Status, "Matched", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            var alreadyInterested = await _context.ProjectInterests
                .AnyAsync(i => i.ProjectId == id && i.SupervisorId == supervisorId);

            if (!alreadyInterested)
            {
                _context.ProjectInterests.Add(new ProjectInterest
                {
                    ProjectId = id,
                    SupervisorId = supervisorId,
                    CreatedAt = DateTimeOffset.UtcNow,
                });

                project.Status = "Under Review";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Review));
        }

        // POST: Projects/ConfirmMatch/5 (Supervisor)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> ConfirmMatch(int id)
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(supervisorId))
            {
                return Challenge();
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project is null)
            {
                return NotFound();
            }

            if (string.Equals(project.Status, "Matched", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            var hasInterest = await _context.ProjectInterests
                .AnyAsync(i => i.ProjectId == id && i.SupervisorId == supervisorId);

            if (!hasInterest)
            {
                return Forbid();
            }

            project.SupervisorId = supervisorId;
            project.Status = "Matched";
            project.MatchedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Matches));
        }

        // GET: Projects/Matches (Supervisor)
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> Matches()
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(supervisorId))
            {
                return Challenge();
            }

            var matchedProjects = await _context.Projects
                .AsNoTracking()
                .Where(p => p.Status == "Matched" && p.SupervisorId == supervisorId)
                .OrderByDescending(p => p.MatchedAt)
                .ToListAsync();

            var studentIds = matchedProjects
                .Select(p => p.StudentId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var studentEmails = await _context.Users
                .AsNoTracking()
                .Where(u => studentIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email, u.UserName })
                .ToListAsync();

            var emailById = studentEmails.ToDictionary(x => x.Id, x => x.Email ?? x.UserName ?? x.Id);

            var model = matchedProjects.Select(p => new SupervisorMatchItemViewModel
            {
                ProjectId = p.Id,
                Title = p.Title,
                ResearchArea = p.ResearchArea,
                TechStack = p.TechStack,
                Status = p.Status,
                StudentEmail = (p.StudentId is not null && emailById.TryGetValue(p.StudentId, out var email)) ? email : "(unknown)",
            }).ToList();

            return View(model);
        }

        // GET: Projects/Details/5 (Student)
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id && m.StudentId == userId);

            if (project is null)
            {
                return NotFound();
            }

            string? supervisorEmail = null;
            if (string.Equals(project.Status, "Matched", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(project.SupervisorId))
            {
                supervisorEmail = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == project.SupervisorId)
                    .Select(u => u.Email ?? u.UserName)
                    .FirstOrDefaultAsync();
            }

            return View(new ProjectDetailsViewModel
            {
                Project = project,
                SupervisorEmail = supervisorEmail,
            });
        }

        // GET: Projects/Create (Student)
        [Authorize(Roles = "Student")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create (Student)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create([Bind("Title,Description,TechStack,ResearchArea")] Project project)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                project.StudentId = userId;
                project.Status = "Pending";

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }

        // GET: Projects/Edit/5 (Student)
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.StudentId == userId);

            if (project is null)
            {
                return NotFound();
            }

            if (string.Equals(project.Status, "Matched", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            return View(project);
        }

        // POST: Projects/Edit/5 (Student)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,TechStack,ResearchArea")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var existing = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.StudentId == userId);
            if (existing is null)
            {
                return NotFound();
            }

            if (string.Equals(existing.Status, "Matched", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(project);
            }

            existing.Title = project.Title;
            existing.Description = project.Description;
            existing.TechStack = project.TechStack;
            existing.ResearchArea = project.ResearchArea;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Projects/Delete/5 (Student)
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id && m.StudentId == userId);

            if (project is null)
            {
                return NotFound();
            }

            if (string.Equals(project.Status, "Matched", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            return View(project);
        }

        // POST: Projects/Delete/5 (Student)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.StudentId == userId);
            if (project is null)
            {
                return NotFound();
            }

            if (string.Equals(project.Status, "Matched", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}