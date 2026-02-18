using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Licenta.Models.Core;
using Licenta.Models.Roles;
using Licenta.Models.Sports;
using Licenta.Models.Contracts;
using Licenta.Models.Medical;
using Licenta.Models.Finance;
using Licenta.Models.Security;
using Licenta.Models.Communication;
using Licenta.Models.Calendar;
using Licenta.Models.HR;
using Licenta.Models.Files;
using Licenta.Models.Scouting;
using Licenta.Models.Identity; // <--- ESENȚIAL: Pentru ApplicationUser

namespace Licenta.Data
{
    // SCHIMBARE AICI: Moștenim IdentityDbContext<ApplicationUser> în loc de simplu IdentityDbContext
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- DEFINIREA TABELELOR (DbSets) ---
        public DbSet<Staff> Staff { get; set; }
        public DbSet<StaffRole> StaffRoles { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Coach> Coaches { get; set; }
        public DbSet<Medic> Medics { get; set; }
        public DbSet<Scout> Scouts { get; set; }
        public DbSet<GeneralManager> GeneralManagers { get; set; }
        public DbSet<ScoutPlayer> ScoutPlayers { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<PlayerTeamHistory> PlayerTeamHistories { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Season> Seasons { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<PlayerGameStats> PlayerGameStats { get; set; }
        public DbSet<Injury> Injuries { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<TerminationNotice> TerminationNotices { get; set; }
        public DbSet<FileStorage> FileStorages { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // --- DEFINIREA CHEILOR PRIMARE ---
            builder.Entity<FileStorage>().HasKey(f => f.FileId);
            builder.Entity<Contract>().HasKey(c => c.ContractId);
            builder.Entity<Staff>().HasKey(s => s.StaffId);
            builder.Entity<Player>().HasKey(p => p.PlayerId);
            builder.Entity<Injury>().HasKey(i => i.InjuryId);
            builder.Entity<UserPermission>().HasKey(up => up.UserPermissionId);

            // --- REZOLVARE WARNINGS PENTRU DECIMAL ---
            builder.Entity<Contract>()
                .Property(c => c.Salary)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Expense>()
                .Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Season>()
                .Property(s => s.Budget)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Team>()
                .Property(t => t.BudgetLimit)
                .HasColumnType("decimal(18,2)");

            // --- CONFIGURĂRI CASCADE DELETE ---

            // 1. Relații Jucător & Contracte
            builder.Entity<Contract>()
                .HasOne(c => c.Staff)
                .WithMany(s => s.Contracts)
                .HasForeignKey(c => c.StaffId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Injury>()
                .HasOne(i => i.Player)
                .WithMany(p => p.Injuries)
                .HasForeignKey(i => i.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PlayerTeamHistory>()
                .HasOne(pth => pth.Player)
                .WithMany(p => p.TeamHistories)
                .HasForeignKey(pth => pth.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            // 2. Performanță și Sezoane
            builder.Entity<Expense>()
                .HasOne(e => e.Season)
                .WithMany(s => s.Expenses)
                .HasForeignKey(e => e.SeasonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Game>()
                .HasOne(g => g.Season)
                .WithMany(s => s.Games)
                .HasForeignKey(g => g.SeasonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PlayerGameStats>()
                .HasOne(pgs => pgs.Game)
                .WithMany(g => g.PlayerStats)
                .HasForeignKey(pgs => pgs.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PlayerGameStats>()
                .HasOne(pgs => pgs.Player)
                .WithMany(p => p.GameStats)
                .HasForeignKey(pgs => pgs.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. Comunicare și Mesaje
            builder.Entity<Message>()
                .HasOne(m => m.FromStaff)
                .WithMany(s => s.MessagesSent)
                .HasForeignKey(m => m.FromStaffId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Message>()
                .HasOne(m => m.ToStaff)
                .WithMany(s => s.MessagesReceived)
                .HasForeignKey(m => m.ToStaffId)
                .OnDelete(DeleteBehavior.NoAction);

            // 4. HR și Notificări
            builder.Entity<TerminationNotice>()
                .HasOne(tn => tn.IssuedByStaff)
                .WithMany(s => s.NoticesIssued)
                .HasForeignKey(tn => tn.IssuedByStaffId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TerminationNotice>()
                .HasOne(tn => tn.TargetStaff)
                .WithMany(s => s.NoticesReceived)
                .HasForeignKey(tn => tn.TargetStaffId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Notification>()
                .HasOne(n => n.Staff)
                .WithMany(s => s.Notifications)
                .HasForeignKey(n => n.StaffId)
                .OnDelete(DeleteBehavior.Cascade);

            // 5. Fișiere și Securitate
            builder.Entity<FileStorage>()
                .HasOne(f => f.Staff)
                .WithMany(s => s.Files)
                .HasForeignKey(f => f.StaffId)
                .OnDelete(DeleteBehavior.Cascade);

            // 6. Permisiuni
            builder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany()
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany()
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}