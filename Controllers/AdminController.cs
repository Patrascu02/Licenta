using Licenta.Data;
using Licenta.Models.Core;
using Licenta.Models.Roles;
using Licenta.Models.Security;
using Licenta.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        // Folosim IdentityUser sau ApplicationUser în funcție de configurarea din Program.cs. 
        // Dacă primești eroare la rulare, schimbă IdentityUser cu ApplicationUser.
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // <--- AICI ERA LIPSA

        public AdminController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager) // <--- INJECTARE AICI
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager; // <--- INIȚIALIZARE AICI
        }

        // --- DASHBOARD ---
        public async Task<IActionResult> AdminDashboard()
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalPlayers = await _context.Players.CountAsync();
            ViewBag.TotalCoaches = await _context.Coaches.CountAsync();
            ViewBag.TotalStaff = await _context.Staff.CountAsync();

            var recentLogs = await _context.AuditLogs
                .Include(a => a.Staff) // Includem numele celui care a făcut acțiunea
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

                // 2. Creare IdentityUser (Login)
                var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);

                    // 3. Creare Staff (Profil Bază)
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
                    await _context.SaveChangesAsync(); // Salvăm pentru a genera StaffId

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
                            Specialty = model.Specialization,
                            MedicalLicense = "În așteptare"
                        });
                    }
                    else if (model.Role == "Scout")
                    {
                        _context.Scouts.Add(new Scout { StaffId = staff.StaffId });
                    }

                    // 5. Audit Log
                    await LogAuditAction($"Creat utilizator nou: {model.Role} - {model.FirstName} {model.LastName}");

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(AdminDashboard));
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // --- VERIFICARE EMAIL (AJAX) ---
        [HttpGet]
        public async Task<JsonResult> CheckEmailExists(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return Json(new { exists = (user != null) });
        }

        // --- GESTIUNE UTILIZATORI (LISTĂ) ---
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

                // EXCLUDEM ADMINUL din listă pentru siguranță
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

        // --- EDITARE UTILIZATOR (GET) ---
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var staff = await _context.Staff
                .Include(s => s.Player)
                .Include(s => s.Coach)
                .Include(s => s.Medic)
                .FirstOrDefaultAsync(s => s.StaffId == id);

            if (staff == null) return NotFound();

            var identityUser = await _userManager.FindByIdAsync(staff.UserId);
            var roles = await _userManager.GetRolesAsync(identityUser);
            var roleName = roles.FirstOrDefault() ?? "Nespecificat";

            var model = new EditUserViewModel
            {
                StaffId = staff.StaffId,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Email = identityUser?.Email,
                DateOfBirth = staff.DateOfBirth,
                HireDate = staff.HireDate,
                RoleName = roleName,

                // Mapare dinamică
                Position = staff.Player?.Position,
                JerseyNumber = staff.Player?.JerseyNumber,
                Height = staff.Player?.Height,
                LicenseNumber = staff.Coach?.LicenseNumber,
                Specialization = staff.Medic?.Specialty
            };

            return View(model);
        }

        // --- EDITARE UTILIZATOR (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var staff = await _context.Staff
                .Include(s => s.Player)
                .Include(s => s.Coach)
                .Include(s => s.Medic)
                .FirstOrDefaultAsync(s => s.StaffId == model.StaffId);

            if (staff == null) return NotFound();

            // Actualizare date generale
            staff.FirstName = model.FirstName;
            staff.LastName = model.LastName;
            staff.DateOfBirth = model.DateOfBirth;
            staff.HireDate = model.HireDate;

            // Actualizare date specifice
            if (model.RoleName == "Player" && staff.Player != null)
            {
                staff.Player.Position = model.Position;
                staff.Player.JerseyNumber = model.JerseyNumber ?? 0;
                staff.Player.Height = model.Height ?? 0;
                _context.Update(staff.Player);
            }
            else if (model.RoleName == "Coach" && staff.Coach != null)
            {
                staff.Coach.LicenseNumber = model.LicenseNumber;
                _context.Update(staff.Coach);
            }
            else if (model.RoleName == "Medic" && staff.Medic != null)
            {
                staff.Medic.Specialty = model.Specialization;
                _context.Update(staff.Medic);
            }

            _context.Update(staff);
            await LogAuditAction($"Editat utilizator: {staff.FirstName} {staff.LastName}");
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManageUsers));
        }

        // --- ȘTERGERE UTILIZATOR (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var staffMember = await _context.Staff.FindAsync(id);
            if (staffMember == null) return NotFound();

            // 1. Audit înainte de ștergere
            await LogAuditAction($"ȘTERS utilizator: {staffMember.FirstName} {staffMember.LastName} (ID: {id})");

            // 2. Ștergere cont Identity
            var user = await _userManager.FindByIdAsync(staffMember.UserId);

            // 3. Ștergere Staff (Cascade Delete în DB se ocupă de restul)
            _context.Staff.Remove(staffMember);

            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageUsers));
        }

        // --- HELPER: LOG AUDIT ---
        private async Task LogAuditAction(string message)
        {
            var currentUserId = _userManager.GetUserId(User);
            // Găsim adminul curent
            var adminStaff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == currentUserId);

            if (adminStaff != null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = message,
                    Timestamp = DateTime.Now,
                    StaffId = adminStaff.StaffId,
                    EntityName = "UserManagement"
                });
                // Notă: Nu dăm SaveChanges aici dacă metoda este apelată dintr-o altă tranzacție (ex: CreateUser),
                // dar e safe dacă e apelată independent sau dacă EF Core gestionează tranzacția.
                // Pentru DeleteUser, trebuie să fim atenți, așa că e mai bine să salvăm în metoda părinte
                // sau să folosim un context separat, dar pentru simplitate lăsăm SaveChanges-ul în metodele principale.
            }
        }

        // ==========================================
        //         MODUL PERMISIUNI (ACL)
        // ==========================================

        // --- LISTA ROLURILOR (Exclus Admin) ---
        [HttpGet]
        public async Task<IActionResult> ManageRoles()
        {
            // Aducem toate rolurile, dar filtrăm Admin-ul
            var roles = await _roleManager.Roles
                .Where(r => r.Name != "Admin") // <--- Această linie face excluderea
                .ToListAsync();

            return View(roles);
        }

        // 2. CONFIGURARE PERMISIUNI (GET)
        [HttpGet]
        public async Task<IActionResult> ManagePermissions(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound();

            // Luăm catalogul de permisiuni
            var allPermissions = await _context.Permissions.ToListAsync();

            // Luăm ce are deja rolul (din tabela de legătură)
            var existingLinkIds = await _context.RolePermissions
                                          .Where(rp => rp.RoleId == roleId)
                                          .Select(rp => rp.PermissionId)
                                          .ToListAsync();

            var model = new ManageRolePermissionsViewModel
            {
                RoleId = roleId,
                RoleName = role.Name,
                PermissionList = allPermissions.Select(p => new PermissionCheckbox
                {
                    PermissionId = p.PermissionId,
                    Name = p.Name,
                    Description = p.Description,
                    IsSelected = existingLinkIds.Contains(p.PermissionId)
                }).ToList()
            };

            return View(model);
        }

        // 3. SALVARE PERMISIUNI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePermissions(ManageRolePermissionsViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null) return NotFound();

            // A. Curățăm permisiunile vechi (DB Custom)
            var oldLinks = await _context.RolePermissions.Where(rp => rp.RoleId == model.RoleId).ToListAsync();
            _context.RolePermissions.RemoveRange(oldLinks);

            // B. Curățăm Claims vechi (Identity)
            var oldClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in oldClaims.Where(c => c.Type == "Permission"))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            // C. Adăugăm permisiunile noi
            var selectedPermissions = model.PermissionList.Where(p => p.IsSelected).ToList();
            foreach (var item in selectedPermissions)
            {
                // DB Custom
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = model.RoleId,
                    PermissionId = item.PermissionId
                });

                // Identity Claims (pentru [Authorize])
                await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", item.Name));
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageRoles));
        }
    }
}