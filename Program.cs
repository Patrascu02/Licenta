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

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // =======================================================
            // NOU: CONFIGURARE POLITICI DE ACCES (ACL)
            // =======================================================
            builder.Services.AddAuthorization(options =>
            {
                // Politica pentru Contracte: Permis dacă e Admin SAU dacă are bifa "ManageContracts"
                options.AddPolicy("CanManageContracts", policy =>
                    policy.RequireAssertion(context =>
                        context.User.IsInRole("Admin") || context.User.HasClaim("Permission", "ManageContracts")));

                // Aici poți adăuga pe viitor și alte politici, ex:
                // options.AddPolicy("CanDeletePlayers", policy =>
                //    policy.RequireAssertion(context => context.User.IsInRole("Admin") || context.User.HasClaim("Permission", "DeletePlayers")));
            });
            // =======================================================

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    var context = services.GetRequiredService<ApplicationDbContext>();

                    await context.Database.MigrateAsync();

                    string[] roleNames = { "Admin", "GeneralManager", "Coach", "Player", "Medic", "Scout" };
                    foreach (var roleName in roleNames)
                    {
                        if (!await roleManager.RoleExistsAsync(roleName))
                        {
                            await roleManager.CreateAsync(new IdentityRole(roleName));
                        }
                    }

                    string adminEmail = "admin@clubbaschet.ro";
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);

                    if (adminUser == null)
                    {
                        var user = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                        var result = await userManager.CreateAsync(user, "Password123!");

                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, "Admin");

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

                    Licenta.Data.DbInitializer.Seed(context);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Eroare la inițializarea minimală a bazei de date.");
                }
            }

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