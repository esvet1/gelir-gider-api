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
    public class TransfersController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public TransfersController(ApiDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        // GET: api/transfers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransferDto>>> GetTransfers([FromQuery] int? limit = null)
        {
            var userId = GetCurrentUserId();
            
            IQueryable<Transfer> query = _context.Transfers
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .OrderByDescending(t => t.TransferDate);

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            var transfers = await query.ToListAsync();

            return Ok(transfers.Select(t => new TransferDto
            {
                Id = t.Id,
                FromAccountId = t.FromAccountId,
                ToAccountId = t.ToAccountId,
                FromAmount = t.FromAmount,
                ToAmount = t.ToAmount,
                ExchangeRate = t.ExchangeRate,
                TransferDate = t.TransferDate,
                Notes = t.Notes,
                CreatedDate = t.CreatedDate,
                FromCurrency = t.FromAccount?.CurrencyName,
                ToCurrency = t.ToAccount?.CurrencyName
            }));
        }

        // GET: api/transfers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TransferDto>> GetTransfer(int id)
        {
            var userId = GetCurrentUserId();
            
            var transfer = await _context.Transfers
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transfer == null)
            {
                return NotFound();
            }

            return Ok(new TransferDto
            {
                Id = transfer.Id,
                FromAccountId = transfer.FromAccountId,
                ToAccountId = transfer.ToAccountId,
                FromAmount = transfer.FromAmount,
                ToAmount = transfer.ToAmount,
                ExchangeRate = transfer.ExchangeRate,
                TransferDate = transfer.TransferDate,
                Notes = transfer.Notes,
                CreatedDate = transfer.CreatedDate,
                FromCurrency = transfer.FromAccount?.CurrencyName,
                ToCurrency = transfer.ToAccount?.CurrencyName
            });
        }

        // POST: api/transfers
        [HttpPost]
        public async Task<ActionResult<TransferDto>> CreateTransfer(CreateTransferRequest request)
        {
            var userId = GetCurrentUserId();

            // Verify accounts belong to user
            var fromAccount = await _context.Accounts.FindAsync(request.FromAccountId);
            var toAccount = await _context.Accounts.FindAsync(request.ToAccountId);

            if (fromAccount == null || fromAccount.UserId != userId ||
                toAccount == null || toAccount.UserId != userId)
            {
                return BadRequest("Invalid accounts");
            }

            if (fromAccount.Balance < request.FromAmount)
            {
                return BadRequest("Insufficient balance");
            }

            var transfer = new Transfer
            {
                FromAccountId = request.FromAccountId,
                ToAccountId = request.ToAccountId,
                FromAmount = request.FromAmount,
                ToAmount = request.ToAmount,
                ExchangeRate = request.ExchangeRate,
                TransferDate = request.TransferDate,
                Notes = request.Notes,
                UserId = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.Transfers.Add(transfer);

            // Update balances
            fromAccount.Balance -= request.FromAmount;
            toAccount.Balance += request.ToAmount;
            fromAccount.LastUpdated = DateTime.UtcNow;
            toAccount.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log audit
            await LogAudit(userId, "CREATE", "Transfer", transfer.Id, 
                $"Transfer {request.FromAmount} from {fromAccount.CurrencyName} to {toAccount.CurrencyName}");

            return CreatedAtAction(nameof(GetTransfer), new { id = transfer.Id }, 
                new TransferDto
                {
                    Id = transfer.Id,
                    FromAccountId = transfer.FromAccountId,
                    ToAccountId = transfer.ToAccountId,
                    FromAmount = transfer.FromAmount,
                    ToAmount = transfer.ToAmount,
                    ExchangeRate = transfer.ExchangeRate,
                    TransferDate = transfer.TransferDate,
                    Notes = transfer.Notes,
                    CreatedDate = transfer.CreatedDate,
                    FromCurrency = fromAccount.CurrencyName,
                    ToCurrency = toAccount.CurrencyName
                });
        }

        // PUT: api/transfers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransfer(int id, UpdateTransferRequest request)
        {
            var userId = GetCurrentUserId();

            var transfer = await _context.Transfers
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transfer == null)
            {
                return NotFound();
            }

            // Reverse old transfer
            transfer.FromAccount!.Balance += transfer.FromAmount;
            transfer.ToAccount!.Balance -= transfer.ToAmount;

            // Apply new transfer
            transfer.FromAmount = request.FromAmount;
            transfer.ToAmount = request.ToAmount;
            transfer.ExchangeRate = request.ExchangeRate;
            transfer.TransferDate = request.TransferDate;
            transfer.Notes = request.Notes;

            transfer.FromAccount.Balance -= request.FromAmount;
            transfer.ToAccount.Balance += request.ToAmount;
            transfer.FromAccount.LastUpdated = DateTime.UtcNow;
            transfer.ToAccount.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogAudit(userId, "UPDATE", "Transfer", id, 
                $"Updated transfer to {request.FromAmount}");

            return NoContent();
        }

        // DELETE: api/transfers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransfer(int id)
        {
            var userId = GetCurrentUserId();

            var transfer = await _context.Transfers
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transfer == null)
            {
                return NotFound();
            }

            // Reverse transfer
            transfer.FromAccount!.Balance += transfer.FromAmount;
            transfer.ToAccount!.Balance -= transfer.ToAmount;
            transfer.FromAccount.LastUpdated = DateTime.UtcNow;
            transfer.ToAccount.LastUpdated = DateTime.UtcNow;

            _context.Transfers.Remove(transfer);
            await _context.SaveChangesAsync();

            await LogAudit(userId, "DELETE", "Transfer", id, 
                $"Deleted transfer of {transfer.FromAmount}");

            return NoContent();
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
    public class TransferDto
    {
        public int Id { get; set; }
        public int FromAccountId { get; set; }
        public int ToAccountId { get; set; }
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public DateTime TransferDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? FromCurrency { get; set; }
        public string? ToCurrency { get; set; }
    }

    public class CreateTransferRequest
    {
        public int FromAccountId { get; set; }
        public int ToAccountId { get; set; }
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public DateTime TransferDate { get; set; } = DateTime.Now;
        public string Notes { get; set; } = string.Empty;
    }

    public class UpdateTransferRequest
    {
        public decimal FromAmount { get; set; }
        public decimal ToAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public DateTime TransferDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
