using Licenta.Data;
using Licenta.Models.Medical;
using Licenta.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Licenta.Controllers
{
    [Authorize(Roles = "Medic,Admin")]
    public class MedicController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicController(ApplicationDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var roster = await _context.Players
                .Include(p => p.Staff)
                .ToListAsync();

            var activeInjuries = await _context.Injuries
                .Where(i => i.Status != "Recuperat")
                .ToListAsync();

            var model = new MedicDashboardViewModel();

            foreach (var player in roster)
            {
                var pStatus = new PlayerMedicalStatus
                {
                    PlayerId = player.PlayerId,
                    FullName = $"{player.Staff.FirstName} {player.Staff.LastName}",
                    JerseyNumber = player.JerseyNumber.ToString(),
                    LastClearance = player.LastMedicalClearance,
                    ActiveInjury = activeInjuries.FirstOrDefault(i => i.PlayerId == player.PlayerId)
                };

                if (!pStatus.IsClearanceValid) model.ExpiredClearances++;
                if (pStatus.ActiveInjury != null) model.TotalInjured++;

                model.Players.Add(pStatus);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GrantClearance(int playerId)
        {
            var player = await _context.Players.Include(p => p.Staff).FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (player != null)
            {
                player.LastMedicalClearance = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> ReportInjury(int playerId, string description, string recoveryText)
        {
            string finalDescription = string.IsNullOrWhiteSpace(recoveryText)
                ? description
                : $"{description} (Timp estimat recuperare: {recoveryText})";

            var injury = new Injury
            {
                PlayerId = playerId,
                Description = finalDescription,
                StartDate = DateTime.Now,
                EstimatedRecoveryDate = DateTime.Now,
                Status = "Accidentat"
            };

            _context.Injuries.Add(injury);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> MarkRecovered(int injuryId)
        {
            var injury = await _context.Injuries.FindAsync(injuryId);
            if (injury != null)
            {
                injury.Status = "Recuperat";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Dashboard));
        }

        // ==========================================
        //  GENERARE AUTOMATA DE PDF-URI (QuestPDF)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> DownloadClearancePdf(int playerId)
        {
            var player = await _context.Players.Include(p => p.Staff).FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (player == null) return NotFound();

            var pdfData = GenerateClearancePdfDocument(player);
            return File(pdfData, "application/pdf", $"VizaMedicala_{player.Staff.LastName}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadInjuryPdf(int injuryId)
        {
            var injury = await _context.Injuries.Include(i => i.Player).ThenInclude(p => p.Staff).FirstOrDefaultAsync(i => i.InjuryId == injuryId);
            if (injury == null) return NotFound();

            var pdfData = GenerateInjuryPdfDocument(injury);
            return File(pdfData, "application/pdf", $"FisaAccidentare_{injury.Player.Staff.LastName}.pdf");
        }

        
        private byte[] GenerateClearancePdfDocument(Licenta.Models.Roles.Player player)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Text("DEPARTAMENTUL MEDICAL").SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                    {
                        x.Spacing(20);
                        x.Item().Text("CERTIFICAT MEDICAL - APT DE JOC").FontSize(16).Bold().AlignCenter();
                        x.Item().Text($"Subsemnatul, Medic Sportiv al clubului, atest faptul că jucătorul:");
                        x.Item().Text($"{player.Staff.FirstName} {player.Staff.LastName}").FontSize(18).Bold().FontColor(Colors.Green.Darken2).AlignCenter();
                        x.Item().Text($"Număr Tricou: #{player.JerseyNumber} | Poziție: {player.Position}");

                        x.Item().Text($"A fost examinat medical astăzi, {DateTime.Now:dd MMMM yyyy}, și este declarat CLINIC SĂNĂTOS și APT pentru antrenamente și meciuri oficiale în sezonul competițional curent.");

                        // --- AICI ESTE MODIFICAREA ---
                        x.Item().Text("Viza medicală este valabilă până la finalul sezonului regulat!").Bold();

                        x.Item().PaddingTop(50).Row(row =>
                        {
                            row.RelativeItem().Text("Data emiterii:\n" + DateTime.Now.ToString("dd.MM.yyyy"));
                            row.RelativeItem().AlignRight().Text("Semnătura Medicului:\n....................................");
                        });
                    });
                });
            });
            return document.GeneratePdf();
        }

        private byte[] GenerateInjuryPdfDocument(Injury injury)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Text("DEPARTAMENTUL MEDICAL").SemiBold().FontSize(20).FontColor(Colors.Red.Darken2);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                    {
                        x.Spacing(20);
                        x.Item().Text("FIȘĂ DE ACCIDENTARE SPORTIVĂ").FontSize(16).Bold().AlignCenter();
                        x.Item().Text($"Nume Jucător: {injury.Player.Staff.FirstName} {injury.Player.Staff.LastName}").FontSize(14).Bold();

                        x.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        x.Item().Text($"Dată constatare: {injury.StartDate:dd MMMM yyyy}");
                        x.Item().Text("Diagnostic / Descriere accidentare:").Bold();
                        x.Item().Text(injury.Description).FontColor(Colors.Red.Medium);

                        x.Item().Text($"Status actual: {injury.Status}").Bold();

                        x.Item().Text("\n\nPrin prezenta se suspendă dreptul de joc și antrenament fizic al sportivului până la recuperarea completă și eliberarea unei noi vize medicale.");

                        x.Item().PaddingTop(50).Row(row =>
                        {
                            row.RelativeItem().Text("Data raportului:\n" + DateTime.Now.ToString("dd.MM.yyyy"));
                            row.RelativeItem().AlignRight().Text("Semnătura Medicului:\n....................................");
                        });
                    });
                });
            });
            return document.GeneratePdf();
        }
    }
}