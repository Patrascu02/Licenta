using Licenta.Models.Core;

namespace Licenta.Models.Contracts
{
    public class Contract
    {
        public int ContractId { get; set; }

        public int StaffId { get; set; }
        public Staff? Staff { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Salary { get; set; } 
        public bool IsActive { get; set; }
        public string? SignedFilePath { get; set; }
    }
}