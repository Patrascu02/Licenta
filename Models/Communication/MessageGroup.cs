using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Licenta.Models.Communication
{
    public class MessageGroup
    {
        [Key]
        public int GroupId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Relații
        public ICollection<MessageGroupMember> Members { get; set; }
        public ICollection<Message> Messages { get; set; }
    }
}