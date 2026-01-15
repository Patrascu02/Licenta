using Licenta.Data;
using Licenta.Models.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Licenta
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // --- LOGICA PENTRU SEEDING (Admin, Roluri ȘI Echipe) ---
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    var context = services.GetRequiredService<ApplicationDbContext>();

                    await context.Database.MigrateAsync();

                    // 1. Creare Roluri
                    string[] roleNames = { "Admin", "GeneralManager", "Coach", "Player", "Medic", "Scout" };
                    foreach (var roleName in roleNames)
                    {
                        if (!await roleManager.RoleExistsAsync(roleName))
                        {
                            await roleManager.CreateAsync(new IdentityRole(roleName));
                        }
                    }

                    // 2. Creare Admin Predefinit
                    string adminEmail = "admin@clubbaschet.ro";
                    string adminPassword = "Password123!";

                    var adminUser = await userManager.FindByEmailAsync(adminEmail);

                    if (adminUser == null)
                    {
                        var user = new IdentityUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            EmailConfirmed = true
                        };

                        var createPowerUser = await userManager.CreateAsync(user, adminPassword);
                        if (createPowerUser.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, "Admin");

                            // --- CREARE PROFIL STAFF PENTRU ADMIN ---
                            var adminStaff = new Staff
                            {
                                UserId = user.Id,
                                FirstName = "Admin",
                                LastName = "Sistem",
                                HireDate = DateTime.Now,
                                DateOfBirth = new DateTime(1990, 1, 1),
                                ExperienceYears = 5
                            };
                            context.Staff.Add(adminStaff);
                            await context.SaveChangesAsync();
                        }
                    }

                    // ====================================================================
                    // 3. APELĂM DBINITIALIZER PENTRU ECHIPĂ ȘI JUCĂTORI (Linia Nouă)
                    // ====================================================================
                    Licenta.Data.DbInitializer.Seed(context);
                    // ====================================================================

                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "A apărut o eroare la popularea bazei de date (Seeding).");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            await app.RunAsync();
        }
    }
}