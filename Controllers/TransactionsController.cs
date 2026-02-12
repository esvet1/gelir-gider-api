using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GelirGiderTakip.API.Data;
using GelirGiderTakip.API.Models;
using System.Security.Claims;

namespace GelirGiderTakip.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public TransactionsController(ApiDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        // GET: api/transactions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
            [FromQuery] TransactionType? type = null,
            [FromQuery] int? limit = null)
        {
            var userId = GetCurrentUserId();
            
            var query = _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Where(t => t.UserId == userId);

            if (type.HasValue)
            {
                query = query.Where(t => t.Type == type.Value);
            }

            query = query.OrderByDescending(t => t.TransactionDate);

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            var transactions = await query.ToListAsync();

            return Ok(transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                Type = t.Type,
                AccountId = t.AccountId,
                CategoryId = t.CategoryId,
                Amount = t.Amount,
                Description = t.Description,
                PayeePayor = t.PayeePayor,
                TransactionDate = t.TransactionDate,
                UsdEquivalent = t.UsdEquivalent,
                Notes = t.Notes,
                CreatedDate = t.CreatedDate,
                AccountName = t.Account?.CurrencyName,
                CategoryName = t.Category?.Name
            }));
        }

        // GET: api/transactions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDto>> GetTransaction(int id)
        {
            var userId = GetCurrentUserId();
            
            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null)
            {
                return NotFound();
            }

            return Ok(new TransactionDto
            {
                Id = transaction.Id,
                Type = transaction.Type,
                AccountId = transaction.AccountId,
                CategoryId = transaction.CategoryId,
                Amount = transaction.Amount,
                Description = transaction.Description,
                PayeePayor = transaction.PayeePayor,
                TransactionDate = transaction.TransactionDate,
                UsdEquivalent = transaction.UsdEquivalent,
                Notes = transaction.Notes,
                CreatedDate = transaction.CreatedDate,
                AccountName = transaction.Account?.CurrencyName,
                CategoryName = transaction.Category?.Name
            });
        }

        // POST: api/transactions
        [HttpPost]
        public async Task<ActionResult<TransactionDto>> CreateTransaction(CreateTransactionRequest request)
        {
            var userId = GetCurrentUserId();

            // Verify account belongs to user
            var account = await _context.Accounts.FindAsync(request.AccountId);
            if (account == null || account.UserId != userId)
            {
                return BadRequest("Invalid account");
            }

            var transaction = new Transaction
            {
                Type = request.Type,
                AccountId = request.AccountId,
                CategoryId = request.CategoryId,
                Amount = request.Amount,
                Description = request.Description,
                PayeePayor = request.PayeePayor,
                TransactionDate = request.TransactionDate,
                UsdEquivalent = request.UsdEquivalent,
                Notes = request.Notes,
                UserId = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);

            // Update account balance
            if (request.Type == TransactionType.Income)
            {
                account.Balance += request.Amount;
            }
            else if (request.Type == TransactionType.Expense)
            {
                account.Balance -= request.Amount;
            }

            account.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log audit
            await LogAudit(userId, "CREATE", "Transaction", transaction.Id, 
                $"Created {request.Type} transaction of {request.Amount}");

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, 
                new TransactionDto
                {
                    Id = transaction.Id,
                    Type = transaction.Type,
                    AccountId = transaction.AccountId,
                    CategoryId = transaction.CategoryId,
                    Amount = transaction.Amount,
                    Description = transaction.Description,
                    PayeePayor = transaction.PayeePayor,
                    TransactionDate = transaction.TransactionDate,
                    UsdEquivalent = transaction.UsdEquivalent,
                    Notes = transaction.Notes,
                    CreatedDate = transaction.CreatedDate
                });
        }

        // PUT: api/transactions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(int id, UpdateTransactionRequest request)
        {
            var userId = GetCurrentUserId();

            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null)
            {
                return NotFound();
            }

            // Reverse old balance change
            if (transaction.Type == TransactionType.Income)
            {
                transaction.Account!.Balance -= transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Expense)
            {
                transaction.Account!.Balance += transaction.Amount;
            }

            // Update transaction
            transaction.Type = request.Type;
            transaction.CategoryId = request.CategoryId;
            transaction.Amount = request.Amount;
            transaction.Description = request.Description;
            transaction.PayeePayor = request.PayeePayor;
            transaction.TransactionDate = request.TransactionDate;
            transaction.UsdEquivalent = request.UsdEquivalent;
            transaction.Notes = request.Notes;

            // Apply new balance change
            if (request.Type == TransactionType.Income)
            {
                transaction.Account.Balance += request.Amount;
            }
            else if (request.Type == TransactionType.Expense)
            {
                transaction.Account.Balance -= request.Amount;
            }

            transaction.Account.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogAudit(userId, "UPDATE", "Transaction", id, 
                $"Updated transaction to {request.Amount}");

            return NoContent();
        }

        // DELETE: api/transactions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var userId = GetCurrentUserId();

            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null)
            {
                return NotFound();
            }

            // Reverse balance change
            if (transaction.Type == TransactionType.Income)
            {
                transaction.Account!.Balance -= transaction.Amount;
            }
            else if (transaction.Type == TransactionType.Expense)
            {
                transaction.Account!.Balance += transaction.Amount;
            }

            transaction.Account!.LastUpdated = DateTime.UtcNow;

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            await LogAudit(userId, "DELETE", "Transaction", id, 
                $"Deleted {transaction.Type} transaction of {transaction.Amount}");

            return NoContent();
        }

        // GET: api/transactions/stats
        [HttpGet("stats")]
        public async Task<ActionResult<MonthlyStatsDto>> GetMonthlyStats(
            [FromQuery] int? year = null,
            [FromQuery] int? month = null)
        {
            var userId = GetCurrentUserId();
            var targetDate = new DateTime(year ?? DateTime.Now.Year, month ?? DateTime.Now.Month, 1);

            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId &&
                           t.TransactionDate.Year == targetDate.Year &&
                           t.TransactionDate.Month == targetDate.Month)
                .ToListAsync();

            var totalIncome = transactions
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);

            var totalExpense = transactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            return Ok(new MonthlyStatsDto
            {
                Year = targetDate.Year,
                Month = targetDate.Month,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetIncome = totalIncome - totalExpense,
                TransactionCount = transactions.Count
            });
        }

        private async Task LogAudit(int userId, string action, string entityType, int entityId, string details)
        {
            var audit = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId.ToString(),
                Details = details,
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            };

            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
    }

    // DTOs
    public class TransactionDto
    {
        public int Id { get; set; }
        public TransactionType Type { get; set; }
        public int AccountId { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PayeePayor { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal UsdEquivalent { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? AccountName { get; set; }
        public string? CategoryName { get; set; }
    }

    public class CreateTransactionRequest
    {
        public TransactionType Type { get; set; }
        public int AccountId { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PayeePayor { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public decimal UsdEquivalent { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class UpdateTransactionRequest
    {
        public TransactionType Type { get; set; }
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PayeePayor { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal UsdEquivalent { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class MonthlyStatsDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetIncome { get; set; }
        public int TransactionCount { get; set; }
    }
}
