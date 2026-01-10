using Licenta.Data;
using Licenta.Models.Core;
using Licenta.Models.Roles;
using Licenta.Models.Security;
using Licenta.Models.Sports;
using Licenta.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // IMPORTANT pentru Claim
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // --- DASHBOARD ---
        public async Task<IActionResult> AdminDashboard()
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalPlayers = await _context.Players.CountAsync();
            ViewBag.TotalCoaches = await _context.Coaches.CountAsync();
            ViewBag.TotalStaff = await _context.Staff.CountAsync();

            var recentLogs = await _context.AuditLogs
                .Include(a => a.Staff)
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
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Această adresă de email este deja utilizată.");
                    return View(model);
                }

                var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);

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
                    await _context.SaveChangesAsync();

                    if (model.Role == "Player")
                    {
                        // 1. Găsim echipa unică a clubului (presupunem că e prima sau singura din tabelă)
                        var mainTeam = await _context.Teams.FirstOrDefaultAsync();

                        // Siguranță: Dacă nu există nicio echipă, o creăm pe loc
                        if (mainTeam == null)
                        {
                            mainTeam = new Team { Name = "Echipa Seniori", City = "Club", Category = "Seniori", MaxAge = 99 };
                            _context.Teams.Add(mainTeam);
                            await _context.SaveChangesAsync();
                        }

                        // 2. Creăm jucătorul și îl legăm direct de această echipă
                        var player = new Player
                        {
                            StaffId = staff.StaffId,
                            Position = model.Position ?? "Nespecificat",
                            JerseyNumber = model.JerseyNumber ?? 0,
                            Height = model.Height ?? 0,

                            // AICI SE FACE ASOCIEREA:
                            CurrentTeamId = mainTeam.TeamId
                        };
                        _context.Players.Add(player);
                        await _context.SaveChangesAsync(); // Salvăm pentru a genera PlayerId

                        // 3. (Opțional dar recomandat) Adăugăm intrarea în istoricul echipei
                        var history = new PlayerTeamHistory
                        {
                            PlayerId = player.PlayerId,
                            TeamId = mainTeam.TeamId,
                            StartDate = DateTime.Now
                        };
                        _context.PlayerTeamHistories.Add(history);
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

            staff.FirstName = model.FirstName;
            staff.LastName = model.LastName;
            staff.DateOfBirth = model.DateOfBirth;
            staff.HireDate = model.HireDate;

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

            await LogAuditAction($"ȘTERS utilizator: {staffMember.FirstName} {staffMember.LastName} (ID: {id})");

            var user = await _userManager.FindByIdAsync(staffMember.UserId);
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
            }
        }

        // ==========================================
        //         MODUL PERMISIUNI (ACL)
        // ==========================================

        // --- 1. GESTIUNE PERMISIUNI ROLURI ---
        [HttpGet]
        public async Task<IActionResult> ManageRoles()
        {
            var roles = await _roleManager.Roles.Where(r => r.Name != "Admin").ToListAsync();
            return View(roles);
        }

        [HttpGet]
        public async Task<IActionResult> ManagePermissions(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound();

            var allPermissions = await _context.Permissions.ToListAsync();
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePermissions(ManageRolePermissionsViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null) return NotFound();

            var oldLinks = await _context.RolePermissions.Where(rp => rp.RoleId == model.RoleId).ToListAsync();
            _context.RolePermissions.RemoveRange(oldLinks);

            var oldClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in oldClaims.Where(c => c.Type == "Permission"))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            var selectedPermissions = model.PermissionList.Where(p => p.IsSelected).ToList();
            foreach (var item in selectedPermissions)
            {
                _context.RolePermissions.Add(new RolePermission { RoleId = model.RoleId, PermissionId = item.PermissionId });
                await _roleManager.AddClaimAsync(role, new Claim("Permission", item.Name));
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageRoles));
        }




        // --- 2. GESTIUNE PERMISIUNI INDIVIDUALE (USER) ---
        [HttpGet]
        public async Task<IActionResult> ManageUserPermissions(int staffId)
        {
            var staff = await _context.Staff.FindAsync(staffId);
            if (staff == null) return NotFound();

            var userId = staff.UserId;
            var user = await _userManager.FindByIdAsync(userId); // Găsim user-ul Identity

            // 1. Toate permisiunile posibile
            var allPermissions = await _context.Permissions.ToListAsync();

            // 2. Permisiuni EXPLICITE (UserPermission - Extra)
            var userDirectPermissions = await _context.UserPermissions
                .Where(up => up.UserId == userId)
                .Select(up => up.PermissionId)
                .ToListAsync();

            // 3. Permisiuni MOȘTENITE (RolePermission - Standard) - LOGICA NOUĂ
            var userRoles = await _userManager.GetRolesAsync(user); // Numele rolurilor (ex: "Coach")
            var roleIds = await _roleManager.Roles
                .Where(r => userRoles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var inheritedPermissions = await _context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var model = new ManageUserPermissionsViewModel
            {
                StaffId = staff.StaffId,
                UserName = $"{staff.FirstName} {staff.LastName}",
                UserId = userId,
                PermissionList = allPermissions.Select(p => new PermissionCheckbox
                {
                    PermissionId = p.PermissionId,
                    Name = p.Name,
                    Description = p.Description,

                    // Este bifat manual DOAR dacă e în UserPermissions
                    IsSelected = userDirectPermissions.Contains(p.PermissionId),

                    // Este moștenit dacă e în RolePermissions
                    IsInherited = inheritedPermissions.Contains(p.PermissionId)
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserPermissions(ManageUserPermissionsViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var oldPermissions = await _context.UserPermissions.Where(up => up.UserId == model.UserId).ToListAsync();
            _context.UserPermissions.RemoveRange(oldPermissions);

            var oldClaims = await _userManager.GetClaimsAsync(user);
            foreach (var claim in oldClaims.Where(c => c.Type == "Permission"))
            {
                await _userManager.RemoveClaimAsync(user, claim);
            }

            var selected = model.PermissionList.Where(p => p.IsSelected).ToList();
            foreach (var item in selected)
            {
                _context.UserPermissions.Add(new UserPermission
                {
                    UserId = model.UserId,
                    PermissionId = item.PermissionId
                });

                // Aici era posibila eroare: new Claim("Permission", item.Name)
                await _userManager.AddClaimAsync(user, new Claim("Permission", item.Name));
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ManageUsers");
        }


        // --- AUDIT LOGS (ISTORIC COMPLET) ---
        [HttpGet]
        public async Task<IActionResult> AuditLogs()
        {
            var logs = await _context.AuditLogs
                .Include(a => a.Staff) // Încărcăm datele despre cine a făcut acțiunea
                .OrderByDescending(l => l.Timestamp) // Cele mai recente primele
                .ToListAsync();

            return View(logs);
        }
    }

}