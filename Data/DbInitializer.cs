using Licenta.Models.Sports;
using System;
using System.Linq;

namespace Licenta.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (!context.Teams.Any())
            {
                context.Teams.Add(new Team
                {
                    Name = "CSA STEAUA", 
                    City = "București",
                    Category = "Seniori",
                    MaxAge = 46,
                    BudgetLimit = 500000 
                });
                context.SaveChanges();
            }

            if (!context.Seasons.Any())
            {
                context.Seasons.Add(new Season
                {
                    Name = $"Sezon {DateTime.Now.Year}-{DateTime.Now.Year + 1}",
                    StartDate = new DateTime(DateTime.Now.Year, 8, 1),           
                    EndDate = new DateTime(DateTime.Now.Year + 1, 6, 30),
                    Budget = 150000
                });
                context.SaveChanges();
            }
        }
    }
}