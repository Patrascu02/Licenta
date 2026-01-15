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

            // Configurare Bază de Date
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Configurare Identity
            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>() // Activăm Rolurile
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // --- SEEDING MINIMAL (Doar Structură, Fără Date de Business) ---
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    var context = services.GetRequiredService<ApplicationDbContext>();

                    // 1. Aplicăm Migrările (Update Database automat)
                    await context.Database.MigrateAsync();

                    // 2. Asigurăm că Rolurile există (dar sunt GOLE, fără permisiuni)
                    string[] roleNames = { "Admin", "GeneralManager", "Coach", "Player", "Medic", "Scout" };
                    foreach (var roleName in roleNames)
                    {
                        if (!await roleManager.RoleExistsAsync(roleName))
                        {
                            await roleManager.CreateAsync(new IdentityRole(roleName));
                        }
                    }

                    // 3. Asigurăm că există un cont de Admin pentru a putea intra în sistem
                    string adminEmail = "admin@clubbaschet.ro";
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);

                    if (adminUser == null)
                    {
                        var user = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                        var result = await userManager.CreateAsync(user, "Password123!");

                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, "Admin");

                            // Profil Staff minimal pentru Admin
                            context.Staff.Add(new Staff
                            {
                                UserId = user.Id,
                                FirstName = "Admin",
                                LastName = "Sistem",
                                HireDate = DateTime.Now,
                                DateOfBirth = new DateTime(1990, 1, 1),
                                ExperienceYears = 5
                            });
                            await context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Eroare la inițializarea minimală a bazei de date.");
                }
            }

            // Configurare Pipeline HTTP
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