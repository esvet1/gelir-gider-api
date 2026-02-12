using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GelirGiderTakip.API.Models
{
    /// <summary>
    /// Gelir ve gider işlemlerini temsil eder
    /// </summary>
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        [ForeignKey("Account")]
        public int AccountId { get; set; }
        public Account? Account { get; set; }

        [Required]
        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Kime ödendi (Gider) veya Kimden alındı (Gelir)
        /// </summary>
        [MaxLength(200)]
        public string PayeePayor { get; set; } = string.Empty;

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        /// <summary>
        /// USD karşılığı (referans için)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal UsdEquivalent { get; set; }

        [MaxLength(1000)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Multi-user tracking
        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        /// <summary>
        /// İşlem türü string (UI için)
        /// </summary>
        public string TypeText => Type == TransactionType.Income ? "Gelir" : "Gider";

        /// <summary>
        /// Formatlanmış tutar
        /// </summary>
        public string FormattedAmount => $"{Account?.CurrencySymbol}{Amount:N2}";
    }
}
