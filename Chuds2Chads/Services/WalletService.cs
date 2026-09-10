using Chuds2Chads.Data;
using Chuds2Chads.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chuds2Chads.Services;

/// <summary>
/// Handles wallet deductions (bets) and credits (payouts) with full transaction logging.
/// Register as Scoped in Program.cs:
///   builder.Services.AddScoped&lt;WalletService&gt;();
/// </summary>
public class WalletService
{
    private readonly AppDbContext _db;

    public WalletService(AppDbContext db)
    {
        _db = db;
    }

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>Returns the current balance for a user, or null if no wallet exists.</summary>
    public async Task<long?> GetBalanceAsync(Guid userId)
    {
        var wallet = await _db.Wallets.AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId);
        return wallet?.Balance;
    }

    /// <summary>
    /// Attempts to deduct <paramref name="amount"/> from the user's wallet as a bet.
    /// Returns true on success, false if the balance is insufficient or the wallet is missing.
    /// </summary>
    public async Task<bool> TryPlaceBetAsync(Guid userId, long amount, string reference)
    {
        if (amount <= 0) return false;

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet is null || wallet.Balance < amount) return false;

        wallet.Balance    -= amount;
        wallet.UpdatedUtc  = DateTime.UtcNow;

        _db.Transactions.Add(new Transaction
        {
            UserId       = userId,
            Type         = TransactionType.BetPlaced,
            Amount       = -amount,
            BalanceAfter = wallet.Balance,
            Reference    = reference,
            CreatedUtc   = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Credits <paramref name="amount"/> to the user's wallet as a payout.
    /// Creates the wallet automatically if it doesn't exist yet (shouldn't happen in prod).
    /// </summary>
    public async Task CreditPayoutAsync(Guid userId, long amount, string reference)
    {
        if (amount <= 0) return;

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet is null)
        {
            wallet = new Wallet { UserId = userId, Balance = 0 };
            _db.Wallets.Add(wallet);
        }

        wallet.Balance    += amount;
        wallet.UpdatedUtc  = DateTime.UtcNow;

        _db.Transactions.Add(new Transaction
        {
            UserId       = userId,
            Type         = TransactionType.Payout,
            Amount       = amount,
            BalanceAfter = wallet.Balance,
            Reference    = reference,
            CreatedUtc   = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Applies a signed balance adjustment and records a matching transaction entry.
    /// Positive values credit chips and negative values deduct chips.
    /// </summary>
    public async Task<bool> AdjustBalanceAsync(Guid userId, long amount, string reference)
    {
        if (amount == 0) return true;

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet is null)
        {
            wallet = new Wallet { UserId = userId, Balance = 0 };
            _db.Wallets.Add(wallet);
        }

        if (amount < 0 && wallet.Balance < Math.Abs(amount))
        {
            return false;
        }

        wallet.Balance += amount;
        wallet.UpdatedUtc = DateTime.UtcNow;

        _db.Transactions.Add(new Transaction
        {
            UserId = userId,
            Type = TransactionType.Adjustment,
            Amount = amount,
            BalanceAfter = wallet.Balance,
            Reference = reference,
            CreatedUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Ensures a wallet row exists for the user (called after registration / first login).
    /// Safe to call multiple times — is a no-op if the wallet already exists.
    /// </summary>
    public async Task EnsureWalletAsync(Guid userId, long startingBalance = 1_000)
    {
        var exists = await _db.Wallets.AnyAsync(w => w.UserId == userId);
        if (exists) return;

        _db.Wallets.Add(new Wallet
        {
            UserId     = userId,
            Balance    = startingBalance,
            UpdatedUtc = DateTime.UtcNow
        });

        _db.Transactions.Add(new Transaction
        {
            UserId       = userId,
            Type         = TransactionType.Deposit,
            Amount       = startingBalance,
            BalanceAfter = startingBalance,
            Reference    = "welcome-bonus",
            CreatedUtc   = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
