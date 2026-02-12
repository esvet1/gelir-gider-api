using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GelirGiderTakip.API.Data;
using GelirGiderTakip.API.Models;
using System.Security.Claims;
using System.Text;

namespace GelirGiderTakip.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BackupController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public BackupController(ApiDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        /// <summary>
        /// SQL formatında tam yedek indir
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportBackup()
        {
            var userId = GetCurrentUserId();
            var sb = new StringBuilder();

            sb.AppendLine("-- GelirGiderTakip v1.3 Yedek Dosyası");
            sb.AppendLine($"-- Oluşturulma: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"-- Kullanıcı ID: {userId}");
            sb.AppendLine();
            sb.AppendLine("BEGIN;");
            sb.AppendLine();

            // Cleanup
            sb.AppendLine($"-- Mevcut verileri temizle");
            sb.AppendLine($"DELETE FROM \"Transfers\" WHERE \"UserId\" = {userId};");
            sb.AppendLine($"DELETE FROM \"Transactions\" WHERE \"UserId\" = {userId};");
            sb.AppendLine($"DELETE FROM \"Accounts\" WHERE \"UserId\" = {userId};");
            sb.AppendLine("DELETE FROM \"Categories\";");
            sb.AppendLine();

            // Categories
            var categories = await _context.Categories.ToListAsync();
            if (categories.Any())
            {
                sb.AppendLine("-- Kategoriler");
                foreach (var c in categories)
                {
                    var name = c.Name.Replace("'", "''");
                    var icon = c.Icon.Replace("'", "''");
                    var color = c.Color.Replace("'", "''");
                    sb.AppendLine($"INSERT INTO \"Categories\" (\"Id\", \"Name\", \"Type\", \"Icon\", \"Color\", \"IsActive\") VALUES ({c.Id}, '{name}', {(int)c.Type}, '{icon}', '{color}', {(c.IsActive ? "TRUE" : "FALSE")});");
                }
                sb.AppendLine();
            }

            // Accounts
            var accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
            if (accounts.Any())
            {
                sb.AppendLine("-- Hesaplar/Kasalar");
                foreach (var a in accounts)
                {
                    sb.AppendLine($"INSERT INTO \"Accounts\" (\"Id\", \"Currency\", \"Balance\", \"CreatedDate\", \"LastUpdated\", \"UserId\") VALUES ({a.Id}, {(int)a.Currency}, {a.Balance.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '{a.CreatedDate:yyyy-MM-dd HH:mm:ss}', '{a.LastUpdated:yyyy-MM-dd HH:mm:ss}', {userId});");
                }
                sb.AppendLine();
            }

            // Transactions
            var transactions = await _context.Transactions.Where(t => t.UserId == userId).OrderBy(t => t.Id).ToListAsync();
            if (transactions.Any())
            {
                sb.AppendLine("-- İşlemler");
                foreach (var t in transactions)
                {
                    var desc = t.Description.Replace("'", "''");
                    var payee = t.PayeePayor.Replace("'", "''");
                    var notes = t.Notes.Replace("'", "''");
                    sb.AppendLine($"INSERT INTO \"Transactions\" (\"Id\", \"Type\", \"AccountId\", \"CategoryId\", \"Amount\", \"Description\", \"PayeePayor\", \"TransactionDate\", \"UsdEquivalent\", \"Notes\", \"CreatedDate\", \"UserId\") VALUES ({t.Id}, {(int)t.Type}, {t.AccountId}, {t.CategoryId}, {t.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '{desc}', '{payee}', '{t.TransactionDate:yyyy-MM-dd HH:mm:ss}', {t.UsdEquivalent.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '{notes}', '{t.CreatedDate:yyyy-MM-dd HH:mm:ss}', {userId});");
                }
                sb.AppendLine();
            }

            // Transfers
            var transfers = await _context.Transfers.Where(tr => tr.UserId == userId).OrderBy(tr => tr.Id).ToListAsync();
            if (transfers.Any())
            {
                sb.AppendLine("-- Transferler");
                foreach (var tr in transfers)
                {
                    var notes = tr.Notes.Replace("'", "''");
                    sb.AppendLine($"INSERT INTO \"Transfers\" (\"Id\", \"FromAccountId\", \"ToAccountId\", \"FromAmount\", \"ToAmount\", \"ExchangeRate\", \"TransferDate\", \"Notes\", \"CreatedDate\", \"UserId\") VALUES ({tr.Id}, {tr.FromAccountId}, {tr.ToAccountId}, {tr.FromAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {tr.ToAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {tr.ExchangeRate.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '{tr.TransferDate:yyyy-MM-dd HH:mm:ss}', '{notes}', '{tr.CreatedDate:yyyy-MM-dd HH:mm:ss}', {userId});");
                }
                sb.AppendLine();
            }

            // Reset sequences
            sb.AppendLine("-- Sayaçları güncelle");
            sb.AppendLine("SELECT setval(pg_get_serial_sequence('\"Categories\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"Categories\"), 1));");
            sb.AppendLine("SELECT setval(pg_get_serial_sequence('\"Accounts\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"Accounts\"), 1));");
            sb.AppendLine("SELECT setval(pg_get_serial_sequence('\"Transactions\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"Transactions\"), 1));");
            sb.AppendLine("SELECT setval(pg_get_serial_sequence('\"Transfers\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"Transfers\"), 1));");
            sb.AppendLine();
            sb.AppendLine("COMMIT;");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"GelirGider_Yedek_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
            return File(bytes, "application/sql", fileName);
        }

        /// <summary>
        /// Yedek dosyasından geri yükle (JSON formatında işlem listesi)
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> ImportBackup([FromBody] BackupImportRequest request)
        {
            var userId = GetCurrentUserId();

            try
            {
                // Mevcut verileri sil
                var existingTransfers = await _context.Transfers.Where(t => t.UserId == userId).ToListAsync();
                _context.Transfers.RemoveRange(existingTransfers);

                var existingTransactions = await _context.Transactions.Where(t => t.UserId == userId).ToListAsync();
                _context.Transactions.RemoveRange(existingTransactions);

                var existingAccounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
                _context.Accounts.RemoveRange(existingAccounts);

                var existingCategories = await _context.Categories.ToListAsync();
                _context.Categories.RemoveRange(existingCategories);

                await _context.SaveChangesAsync();

                // Kategorileri yükle
                if (request.Categories != null)
                {
                    foreach (var c in request.Categories)
                    {
                        _context.Categories.Add(new Category
                        {
                            Name = c.Name,
                            Type = (TransactionType)c.Type,
                            Icon = c.Icon,
                            Color = c.Color,
                            IsActive = c.IsActive
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // Hesapları yükle
                if (request.Accounts != null)
                {
                    foreach (var a in request.Accounts)
                    {
                        _context.Accounts.Add(new Account
                        {
                            Currency = (CurrencyType)a.Currency,
                            Balance = a.Balance,
                            CreatedDate = DateTime.SpecifyKind(a.CreatedDate, DateTimeKind.Utc),
                            LastUpdated = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                            UserId = userId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Yedek başarıyla geri yüklendi!", categoriesCount = request.Categories?.Count ?? 0, accountsCount = request.Accounts?.Count ?? 0 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Geri yükleme hatası: " + ex.Message });
            }
        }
    }

    // Import DTOs
    public class BackupImportRequest
    {
        public List<BackupCategoryItem>? Categories { get; set; }
        public List<BackupAccountItem>? Accounts { get; set; }
    }

    public class BackupCategoryItem
    {
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public string Icon { get; set; } = "📁";
        public string Color { get; set; } = "#2196F3";
        public bool IsActive { get; set; } = true;
    }

    public class BackupAccountItem
    {
        public int Currency { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
