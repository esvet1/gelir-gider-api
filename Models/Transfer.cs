using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GelirGiderTakip.API.Models
{
    /// <summary>
    /// Kasalar arası transfer işlemlerini temsil eder (Döviz bozdurma dahil)
    /// </summary>
    public class Transfer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("FromAccount")]
        public int FromAccountId { get; set; }
        public Account? FromAccount { get; set; }

        [Required]
        [ForeignKey("ToAccount")]
        public int ToAccountId { get; set; }
        public Account? ToAccount { get; set; }

        /// <summary>
        /// Kaynak kasadan çıkan miktar
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FromAmount { get; set; }

        /// <summary>
        /// Hedef kasaya giren miktar
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ToAmount { get; set; }

        /// <summary>
        /// Kullanılan döviz kuru
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal ExchangeRate { get; set; }

        public DateTime TransferDate { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Multi-user tracking
        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        /// <summary>
        /// Transfer bilgisi string (UI için)
        /// </summary>
        public string TransferInfo => $"{FromAccount?.CurrencyName} → {ToAccount?.CurrencyName}";
    }
}
