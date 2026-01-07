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


        // --- GESTIUNE UTILIZATORI (Excluzând Admin) ---
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageUsers()
        {
            var staffList = await _context.Staff
                .Include(s => s.Player)
                .Include(s => s.Coach)
                .Include(s => s.Medic)
                .Include(s => s.Scout)
                .ToListAsync();

            var viewModel = new List<UserManagementViewModel>();

            foreach (var staff in staffList)
            {
                var identityUser = await _userManager.FindByIdAsync(staff.UserId);
                if (identityUser == null) continue;

                var roles = await _userManager.GetRolesAsync(identityUser);
                var roleName = roles.FirstOrDefault() ?? "Nespecificat";

                // REZOLVARE CERINȚA 2: Dacă este Admin, nu îl afișăm în listă
                if (roleName == "Admin") continue;

                viewModel.Add(new UserManagementViewModel
                {
                    StaffMember = staff,
                    RoleName = roleName,
                    Email = identityUser.Email
                });
            }

            return View(viewModel);
        }

        // GET: Admin/EditUser/5
        public async Task<IActionResult> EditUser(int id)
        {
            var staff = await _context.Staff
                .Include(s => s.Player)
                .Include(s => s.Coach)
                .Include(s => s.Medic)
                .Include(s => s.Scout)
                .FirstOrDefaultAsync(m => m.StaffId == id);

            if (staff == null) return NotFound();

            var identityUser = await _userManager.FindByIdAsync(staff.UserId);

            // Mapăm datele către un ViewModel de Editare (trebuie creat sau folosit cel de Create adaptat)
            var model = new EditUserViewModel
            {
                StaffId = staff.StaffId,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Email = identityUser?.Email,
                DateOfBirth = staff.DateOfBirth,
                HireDate = staff.HireDate
                // Adaugă aici câmpuri specifice dacă vrei să editezi și poziția/numărul (Player)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var staff = await _context.Staff.FindAsync(model.StaffId);
                if (staff == null) return NotFound();

                staff.FirstName = model.FirstName;
                staff.LastName = model.LastName;
                staff.DateOfBirth = model.DateOfBirth;
                staff.HireDate = model.HireDate;

                _context.Update(staff);

                // Logare Audit pentru Editare
                await LogAuditAction($"Editat utilizator: {staff.FirstName} {staff.LastName}");

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageUsers));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var staffMember = await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffId == id);

            if (staffMember == null) return NotFound();

            // 1. Logăm acțiunea ÎNAINTE de ștergere
            await LogAuditAction($"ȘTERS utilizator: {staffMember.FirstName} {staffMember.LastName} (ID: {id})");

            // 2. Ștergem datele din Identity (Contul de login)
            var user = await _userManager.FindByIdAsync(staffMember.UserId);

            // 3. Ștergem Staff (va șterge și Player/Coach prin Cascade Delete în DB)
            _context.Staff.Remove(staffMember);
            await _context.SaveChangesAsync();

            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            return RedirectToAction(nameof(ManageUsers));
        }

        // METODĂ HELPER pentru Audit (pentru a nu repeta codul)
        private async Task LogAuditAction(string actionDescription)
        {
            var currentUserId = _userManager.GetUserId(User);
            var currentAdmin = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == currentUserId);

            if (currentAdmin != null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = actionDescription,
                    Timestamp = DateTime.Now,
                    StaffId = currentAdmin.StaffId,
                    EntityName = "Staff"
                });
            }
        }
    }
}