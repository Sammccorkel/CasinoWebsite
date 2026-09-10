using Chuds2Chads.Data;
using Microsoft.EntityFrameworkCore;

namespace Chuds2Chads.Services;

public class LeaderboardService
{
    private readonly AppDbContext _db;

    public LeaderboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LeaderboardEntryViewModel>> GetLeaderboardAsync()
    {
        var players = await (
            from user in _db.Users.AsNoTracking()
            join wallet in _db.Wallets.AsNoTracking() on user.Id equals wallet.UserId into walletGroup
            from wallet in walletGroup.DefaultIfEmpty()
            select new
            {
                user.Id,
                UserName = user.UserName,
                user.CreatedDate,
                Chips = wallet != null ? wallet.Balance : 0L
            })
            .OrderByDescending(player => player.Chips)
            .ThenBy(player => player.UserName)
            .ThenBy(player => player.CreatedDate)
            .ToListAsync();

        return players
            .Select((player, index) => new LeaderboardEntryViewModel
            {
                Rank = index + 1,
                UserId = player.Id,
                UserName = string.IsNullOrWhiteSpace(player.UserName) ? "Player" : player.UserName,
                Chips = player.Chips
            })
            .ToList();
    }
}

public class LeaderboardEntryViewModel
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public long Chips { get; set; }
}
