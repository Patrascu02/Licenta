using Licenta.Models.Sports;
using System.Linq;

namespace Licenta.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            // Dacă nu există nicio echipă, o creăm pe cea principală
            if (!context.Teams.Any())
            {
                context.Teams.Add(new Team
                {
                    Name = "CSA STEAUA", // Sau numele clubului tău
                    City = "București",
                    Category = "Seniori",
                    MaxAge = 46
                });
                context.SaveChanges();
            }
        }
    }
}