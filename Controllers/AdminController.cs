using Licenta.Data;
using Licenta.Models.Core;
using Licenta.Models.Roles;
using Licenta.Models.Security;
using Licenta.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Licenta.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- DASHBOARD ---
        public async Task<IActionResult> AdminDashboard()
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalPlayers = await _context.Players.CountAsync();
            ViewBag.TotalCoaches = await _context.Coaches.CountAsync();
            ViewBag.TotalStaff = await _context.Staff.CountAsync();

            var recentLogs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(5)
                .ToListAsync();

            return View(recentLogs);
        }

        // --- CREARE UTILIZATOR (GET) ---
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        // --- CREARE UTILIZATOR (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Verificare Email Duplicat
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Această adresă de email este deja utilizată.");
                    return View(model);
                }

                // 2. Creare IdentityUser (Securitate)
                var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);

                    // 3. Creare Staff (Părinte)
                    var staff = new Staff
                    {
                        UserId = user.Id,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        DateOfBirth = model.DateOfBirth,
                        HireDate = model.HireDate,
                        ExperienceYears = 0
                    };
                    _context.Staff.Add(staff);

                    // Salvăm Staff-ul pentru a genera StaffId
                    await _context.SaveChangesAsync();

                    // 4. Creare Entitate Specifică (Child)
                    if (model.Role == "Player")
                    {
                        _context.Players.Add(new Player
                        {
                            StaffId = staff.StaffId,
                            Position = model.Position ?? "Nespecificat",
                            JerseyNumber = model.JerseyNumber ?? 0,
                            Height = model.Height ?? 0
                        });
                    }
                    else if (model.Role == "Coach")
                    {
                        _context.Coaches.Add(new Coach
                        {
                            StaffId = staff.StaffId,
                            LicenseNumber = model.LicenseNumber ?? "Fără Licență"
                        });
                    }
                    else if (model.Role == "Medic")
                    {
                        _context.Medics.Add(new Medic
                        {
                            StaffId = staff.StaffId,
                            Specialty = model.Specialization, // Mapare: View(Specialization) -> DB(Specialty)
                            MedicalLicense = "În așteptare"
                        });
                    }
                    else if (model.Role == "Scout")
                    {
                        _context.Scouts.Add(new Scout
                        {
                            StaffId = staff.StaffId
                        });
                    }

                    // 5. Audit Log (Cine a creat pe cine)
                    var currentUserId = _userManager.GetUserId(User);
                    var currentAdmin = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == currentUserId);
                    if (currentAdmin != null)
                    {
                        _context.AuditLogs.Add(new AuditLog
                        {
                            Action = $"Creat {model.Role}: {model.FirstName} {model.LastName} ({model.Email})",
                            Timestamp = DateTime.Now,
                            StaffId = currentAdmin.StaffId
                        });
                    }

                    // Salvare finală pentru entitatea de rol și log
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(AdminDashboard));
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // --- VERIFICARE EMAIL LIVE (AJAX) ---
        [HttpGet]
        public async Task<JsonResult> CheckEmailExists(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return Json(new { exists = (user != null) });
        }
    }
}