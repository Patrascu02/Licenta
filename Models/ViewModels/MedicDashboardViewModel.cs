using Licenta.Models.Medical;
using System;
using System.Collections.Generic;

namespace Licenta.Models.ViewModels
{
    public class MedicDashboardViewModel
    {
        public List<PlayerMedicalStatus> Players { get; set; } = new List<PlayerMedicalStatus>();
        public int TotalInjured { get; set; }
        public int ExpiredClearances { get; set; }
    }

    public class PlayerMedicalStatus
    {
        public int PlayerId { get; set; }
        public string FullName { get; set; }
        public string JerseyNumber { get; set; }
        public DateTime? LastClearance { get; set; }

        public bool IsClearanceValid => LastClearance.HasValue && LastClearance.Value.AddMonths(8) >= DateTime.Now;

        public Injury ActiveInjury { get; set; }
    }
}