using System.ComponentModel.DataAnnotations;

namespace GelirGiderTakip.API.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public User? User { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE
        
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty; // Transaction, Transfer, etc.
        
        public string? EntityId { get; set; }
        
        public string? Details { get; set; } // JSON details
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [MaxLength(45)]
        public string? IpAddress { get; set; }
    }
}
