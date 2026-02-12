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
    public class AccountsController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public AccountsController(ApiDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        // GET: api/accounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountDto>>> GetAccounts()
        {
            var userId = GetCurrentUserId();
            
            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId)
                .ToListAsync();

            return Ok(accounts.Select(a => new AccountDto
            {
                Id = a.Id,
                Currency = a.Currency,
                Balance = a.Balance,
                CreatedDate = a.CreatedDate,
                LastUpdated = a.LastUpdated,
                CurrencySymbol = a.CurrencySymbol,
                CurrencyName = a.CurrencyName
            }));
        }

        // GET: api/accounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AccountDto>> GetAccount(int id)
        {
            var userId = GetCurrentUserId();
            
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (account == null)
            {
                return NotFound();
            }

            return Ok(new AccountDto
            {
                Id = account.Id,
                Currency = account.Currency,
                Balance = account.Balance,
                CreatedDate = account.CreatedDate,
                LastUpdated = account.LastUpdated,
                CurrencySymbol = account.CurrencySymbol,
                CurrencyName = account.CurrencyName
            });
        }

        // POST: api/accounts
        [HttpPost]
        public async Task<ActionResult<AccountDto>> CreateAccount(CreateAccountRequest request)
        {
            var userId = GetCurrentUserId();

            // Check if user already has this currency account
            var existing = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Currency == request.Currency);

            if (existing != null)
            {
                return BadRequest("Account with this currency already exists");
            }

            var account = new Account
            {
                Currency = request.Currency,
                Balance = request.InitialBalance,
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, 
                new AccountDto
                {
                    Id = account.Id,
                    Currency = account.Currency,
                    Balance = account.Balance,
                    CreatedDate = account.CreatedDate,
                    LastUpdated = account.LastUpdated,
                    CurrencySymbol = account.CurrencySymbol,
                    CurrencyName = account.CurrencyName
                });
        }

        // DELETE: api/accounts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var userId = GetCurrentUserId();

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (account == null)
            {
                return NotFound();
            }

            // Check if account has transactions
            var hasTransactions = await _context.Transactions
                .AnyAsync(t => t.AccountId == id);

            if (hasTransactions)
            {
                return BadRequest("Cannot delete account with existing transactions");
            }

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // DTOs
    public class AccountDto
    {
        public int Id { get; set; }
        public CurrencyType Currency { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public string CurrencySymbol { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
    }

    public class CreateAccountRequest
    {
        public CurrencyType Currency { get; set; }
        public decimal InitialBalance { get; set; } = 0;
    }
}
