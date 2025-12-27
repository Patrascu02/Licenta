using Licenta.Models.Roles;

namespace Licenta.Models.Medical
{
    public class Injury
    {
        public int InjuryId { get; set; }
        public int PlayerId { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EstimatedRecoveryDate { get; set; }
        public string Status { get; set; }
        public string MedicalReportFile { get; set; }

        public Player Player { get; set; }
    }
}
