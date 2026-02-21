using Licenta.Models.Core;
using System.ComponentModel.DataAnnotations;

namespace Licenta.Models.Communication
{
    public class MessageGroupMember
    {
        [Key]
        public int Id { get; set; }

        public int GroupId { get; set; }
        public MessageGroup Group { get; set; }

        public int StaffId { get; set; }
        public Staff Staff { get; set; }

        public DateTime LastReadAt { get; set; } = DateTime.Now;
    }
}