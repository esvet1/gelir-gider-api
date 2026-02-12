using System.ComponentModel.DataAnnotations;

namespace GelirGiderTakip.API.Models
{
    /// <summary>
    /// Gelir ve Gider kategorilerini temsil eder
    /// </summary>
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public TransactionType Type { get; set; }

        /// <summary>
        /// İkon path veya emoji
        /// </summary>
        [MaxLength(50)]
        public string Icon { get; set; } = "📁";

        /// <summary>
        /// Kategori rengi (Hex format, örn: #FF5722)
        /// </summary>
        [MaxLength(7)]
        public string Color { get; set; } = "#2196F3";

        public bool IsActive { get; set; } = true;
    }
}
