using Licenta.Data;
using Licenta.Models.Core;
using Licenta.Models.Roles;
using Licenta.Models.Security;
using Licenta.Models.Sports;
using Licenta.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _env;

        public AdminController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _env = env;
        }

        // --- DASHBOARD ---
        public async Task<IActionResult> AdminDashboard()
        {
            // 1. Statistici de Bază
            var totalUsers = await _context.Users.CountAsync();
            var totalPlayers = await _context.Players.CountAsync();
            var totalCoaches = await _context.Coaches.CountAsync();
            var totalStaff = await _context.Staff.CountAsync();

            // 2. RAM Usage
            var currentProcess = Process.GetCurrentProcess();
            double ramUsedMb = Math.Round(currentProcess.WorkingSet64 / 1024.0 / 1024.0, 2);
            double ramLimitMb = 1024;

            // 3. Storage Usage
            double storageUsedMb = 0;
            try
            {
                var webRootPath = _env.WebRootPath;
                if (Directory.Exists(webRootPath))
                {
                    long bytes = Directory.GetFiles(webRootPath, "*", SearchOption.AllDirectories)
                                          .Sum(t => (new FileInfo(t).Length));
                    storageUsedMb = Math.Round(bytes / 1024.0 / 1024.0, 2);
                }
            }
            catch { storageUsedMb = 0; }
            double storageLimitMb = 500;

            // 4. Sesiuni Active
            var activeThreshold = DateTime.Now.AddMinutes(-30);
            var activeUsersCount = await _context.AuditLogs
                .Where(l => l.Timestamp >= activeThreshold)
                .Select(l => l.StaffId)
                .Distinct()
                .CountAsync();

            if (activeUsersCount == 0) activeUsersCount = 1;

            // 5. Audit Logs Recente
            var recentLogs = await _context.AuditLogs
                .Include(a => a.Staff)
                .OrderByDescending(l => l.Timestamp)
                .Take(6)
                .ToListAsync();

            // Ultima Activitate
            var lastLog = recentLogs.FirstOrDefault();
            string lastActivityStr = lastLog != null ? lastLog.Timestamp.ToString("HH:mm") : "--:--";

            var model = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalPlayers = totalPlayers,
                TotalCoaches = totalCoaches,
                TotalStaff = totalStaff,
                RamUsageMb = ramUsedMb,
                RamTotalMb = ramLimitMb,
                StorageUsageMb = storageUsedMb,
                StorageLimitMb = storageLimitMb,
                ActiveSessions = activeUsersCount,
                LastActivityTime = lastActivityStr,
                CpuUsagePercent = new Random().Next(5, 20),
                RecentLogs = recentLogs
            };

            return View(model);
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
                        var mainTeam = await _context.Teams.FirstOrDefaultAsync();
                        var player = new Player
                        {
                            StaffId = staff.StaffId,
                            Position = model.Position,
                            JerseyNumber = model.JerseyNumber ?? 0,
                            Height = model.Height ?? 0,
                            Weight = model.Weight ?? 0,
                            CurrentTeamId = mainTeam?.TeamId
                        };
                        _context.Players.Add(player);
                        await _context.SaveChangesAsync();

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
                    else if (model.Role == "GeneralManager")
                    {
                        _context.GeneralManagers.Add(new GeneralManager
                        {
                            StaffId = staff.StaffId,
                            Office = model.Office ?? "Administrație"
                        });
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

        // --- GESTIUNE UTILIZATORI ---
        public async Task<IActionResult> ManageUsers()
        {
            var staffList = await _context.Staff
                .Include(s => s.Player)
                .Include(s => s.Coach)
                .Include(s => s.Medic)
                .Include(s => s.Scout)
                .Include(s => s.GeneralManager)
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
                .Include(s => s.GeneralManager) // AM INCLUS GM AICI
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

                // Jucător
                Position = staff.Player?.Position,
                JerseyNumber = staff.Player?.JerseyNumber,
                Height = staff.Player?.Height,
                Weight = staff.Player?.Weight,

                // Restul
                LicenseNumber = staff.Coach?.LicenseNumber,
                Specialization = staff.Medic?.Specialty,
                Office = staff.GeneralManager?.Office // AM ADĂUGAT ASTA
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
                .Include(s => s.GeneralManager) // AM INCLUS GM AICI
                .FirstOrDefaultAsync(s => s.StaffId == model.StaffId);

            if (staff == null) return NotFound();

            // Update Date Generale
            staff.FirstName = model.FirstName;
            staff.LastName = model.LastName;
            staff.DateOfBirth = model.DateOfBirth;
            staff.HireDate = model.HireDate;

            // Update Date Specifice
            if (model.RoleName == "Player" && staff.Player != null)
            {
                staff.Player.Position = model.Position;
                staff.Player.JerseyNumber = model.JerseyNumber ?? 0;
                staff.Player.Height = model.Height ?? 0;
                staff.Player.Weight = model.Weight ?? 0;
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
            else if (model.RoleName == "GeneralManager" && staff.GeneralManager != null) // AM ADĂUGAT LOGICA GM
            {
                staff.GeneralManager.Office = model.Office;
                _context.Update(staff.GeneralManager);
            }

            _context.Update(staff);
            await LogAuditAction($"Editat utilizator: {staff.FirstName} {staff.LastName}");
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManageUsers));
        }

        // --- DELETE USER ---
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

        // --- HELPER LOG ---
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
            var user = await _userManager.FindByIdAsync(userId);

            // 1. Toate permisiunile
            var allPermissions = await _context.Permissions.ToListAsync();

            // 2. Permisiuni EXPLICITE (UserPermission - Extra)
            var userDirectPermissions = await _context.UserPermissions
                .Where(up => up.UserId == userId)
                .Select(up => up.PermissionId)
                .ToListAsync();

            // 3. Permisiuni MOȘTENITE (RolePermission)
            var userRoles = await _userManager.GetRolesAsync(user);
            var roleName = userRoles.FirstOrDefault() ?? "Nespecificat"; // Luăm numele rolului

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
                RoleName = roleName, // <--- Setăm proprietatea
                PermissionList = allPermissions.Select(p => new PermissionCheckbox
                {
                    PermissionId = p.PermissionId,
                    Name = p.Name,
                    Description = p.Description,
                    IsSelected = userDirectPermissions.Contains(p.PermissionId),
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

                await _userManager.AddClaimAsync(user, new Claim("Permission", item.Name));
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ManageUsers");
        }

        [HttpGet]
        public async Task<IActionResult> AuditLogs()
        {
            var logs = await _context.AuditLogs
                .Include(a => a.Staff)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            return View(logs);
        }
    }
}