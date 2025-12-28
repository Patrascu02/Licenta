using Licenta.Models.Roles;

namespace Licenta.Models.Contracts
{
    public class Contract
    {
        public int ContractId { get; set; }
        public int PlayerId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Salary { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
        public string? SignedFilePath { get; set; }

        public Player? Player { get; set; }
    }
}
