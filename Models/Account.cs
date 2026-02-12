using System;
using System.ComponentModel.DataAnnotations;

namespace GelirGiderTakip.API.Models
{
    /// <summary>
    /// Kasa hesabını temsil eder (TL, EUR, USD)
    /// </summary>
    public class Account
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public CurrencyType Currency { get; set; }

        public decimal Balance { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // Multi-user tracking
        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        /// <summary>
        /// Para birimi sembolünü döner
        /// </summary>
        public string CurrencySymbol
        {
            get
            {
                return Currency switch
                {
                    CurrencyType.TL => "₺",
                    CurrencyType.EUR => "€",
                    CurrencyType.USD => "$",
                    _ => ""
                };
            }
        }

        /// <summary>
        /// Para birimi adını döner
        /// </summary>
        public string CurrencyName
        {
            get
            {
                return Currency switch
                {
                    CurrencyType.TL => "TL",
                    CurrencyType.EUR => "EUR",
                    CurrencyType.USD => "USD",
                    _ => ""
                };
            }
        }

        /// <summary>
        /// Formatlanmış bakiye string'i (örn: "₺1,250.50")
        /// </summary>
        public string FormattedBalance => $"{CurrencySymbol}{Balance:N2}";
    }
}
