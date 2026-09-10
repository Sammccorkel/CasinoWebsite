using Chuds2Chads.Data;
using Chuds2Chads.Data.Entities;
using Chuds2Chads.Games.Blackjack;
using Chuds2Chads.Games.Poker;
using Microsoft.EntityFrameworkCore;

namespace Chuds2Chads.Services;

public class MultiplayerService
{
    private static readonly TimeSpan OfflineThreshold = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan DisconnectGracePeriod = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _syncRoot = new();
    private readonly Dictionary<Guid, BlackjackRoomRuntime> _blackjackRooms = new();
    private readonly Dictionary<Guid, PokerRoomRuntime> _pokerRooms = new();

    public MultiplayerService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task EnsureUserSetupAsync(Guid userId)
    {
        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(user.FriendCode) ||
            await db.Users.AnyAsync(u => u.Id != userId && u.FriendCode == user.FriendCode))
        {
            user.FriendCode = await GenerateFriendCodeAsync(db, userId);
        }

        await walletService.EnsureWalletAsync(userId, 1_000);
        user.LastSeenUtc = DateTime.UtcNow;
        if (user.PresenceStatus == UserPresenceStatus.Offline)
        {
            user.PresenceStatus = UserPresenceStatus.Online;
        }

        await db.SaveChangesAsync();
    }

    public async Task BackfillMissingFriendCodesAsync()
    {
        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = await db.Users.OrderBy(u => u.CreatedDate).ToListAsync();
        if (users.Count == 0)
        {
            return;
        }

        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var changed = false;

        foreach (var user in users)
        {
            if (!string.IsNullOrWhiteSpace(user.FriendCode) && seenCodes.Add(user.FriendCode))
            {
                continue;
            }

            user.FriendCode = await GenerateFriendCodeAsync(db, user.Id);
            seenCodes.Add(user.FriendCode);
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }

    public async Task<string> GenerateUniqueFriendCodeAsync(Guid userId)
    {
        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await GenerateFriendCodeAsync(db, userId);
    }

    public async Task SetPresenceAsync(Guid userId, UserPresenceStatus status, Guid? roomId = null)
    {
        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return;
        }

        user.LastSeenUtc = DateTime.UtcNow;
        user.PresenceStatus = status;
        user.ActiveRoomId = roomId;

        if (roomId.HasValue)
        {
            var roomPlayer = await db.RoomPlayers.FirstOrDefaultAsync(rp => rp.RoomId == roomId && rp.UserId == userId);
            if (roomPlayer is not null)
            {
                roomPlayer.IsConnected = true;
                roomPlayer.LastHeartbeatUtc = DateTime.UtcNow;
                roomPlayer.DisconnectedUntilUtc = null;
            }
        }

        await db.SaveChangesAsync();
        await CleanupExpiredDisconnectsAsync();
    }

    public async Task<(bool Ok, string Message)> SendFriendRequestAsync(Guid userId, string friendCode)
    {
        await EnsureUserSetupAsync(userId);
        friendCode = friendCode.Trim().ToUpperInvariant();

        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var target = await db.Users.FirstOrDefaultAsync(u => u.FriendCode == friendCode);
        if (target is null)
        {
            return (false, "Friend code not found.");
        }

        if (target.Id == userId)
        {
            return (false, "You cannot add yourself.");
        }

        var alreadyFriends = await db.Friendships.AnyAsync(f => f.UserId == userId && f.FriendUserId == target.Id);
        if (alreadyFriends)
        {
            return (false, "That user is already in your friends list.");
        }

        var existingRequest = await db.FriendRequests.AnyAsync(r =>
            ((r.RequesterUserId == userId && r.RequesteeUserId == target.Id) ||
             (r.RequesterUserId == target.Id && r.RequesteeUserId == userId)) &&
            r.Status == FriendRequestStatus.Pending);
        if (existingRequest)
        {
            return (false, "A pending request already exists.");
        }

        db.FriendRequests.Add(new FriendRequest
        {
            RequesterUserId = userId,
            RequesteeUserId = target.Id
        });

        await db.SaveChangesAsync();
        return (true, $"Friend request sent to {target.UserName}.");
    }

    public async Task<(bool Ok, string Message)> RespondToFriendRequestAsync(Guid userId, Guid requestId, bool accept)
    {
        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var request = await db.FriendRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.RequesteeUserId == userId);
        if (request is null)
        {
            return (false, "Friend request not found.");
        }

        if (request.Status != FriendRequestStatus.Pending)
        {
            return (false, "That request has already been handled.");
        }

        request.Status = accept ? FriendRequestStatus.Accepted : FriendRequestStatus.Ignored;
        request.RespondedUtc = DateTime.UtcNow;

        if (accept)
        {
            if (!await db.Friendships.AnyAsync(f => f.UserId == request.RequesterUserId && f.FriendUserId == request.RequesteeUserId))
            {
                db.Friendships.Add(new Friendship { UserId = request.RequesterUserId, FriendUserId = request.RequesteeUserId });
            }

            if (!await db.Friendships.AnyAsync(f => f.UserId == request.RequesteeUserId && f.FriendUserId == request.RequesterUserId))
            {
                db.Friendships.Add(new Friendship { UserId = request.RequesteeUserId, FriendUserId = request.RequesterUserId });
            }
        }

        await db.SaveChangesAsync();
        return (true, accept ? "Friend request accepted." : "Friend request ignored.");
    }

    public async Task<DashboardViewModel?> GetDashboardAsync(Guid userId)
    {
        await EnsureUserSetupAsync(userId);
        await CleanupExpiredDisconnectsAsync();

        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return null;
        }

        var wallet = await db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId);
        var requests = await db.FriendRequests.AsNoTracking()
            .Where(r => r.RequesteeUserId == userId && r.Status == FriendRequestStatus.Pending)
            .ToListAsync();

        var requesterIds = requests.Select(r => r.RequesterUserId).Distinct().ToList();
        var requestUsers = await db.Users.AsNoTracking()
            .Where(u => requesterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var friendshipRows = await db.Friendships.AsNoTracking()
            .Where(f => f.UserId == userId)
            .ToListAsync();
        var friendIds = friendshipRows.Select(f => f.FriendUserId).ToList();
        var friendUsers = await db.Users.AsNoTracking()
            .Where(u => friendIds.Contains(u.Id))
            .ToListAsync();

        var activeRoomIds = friendUsers.Where(u => u.ActiveRoomId.HasValue).Select(u => u.ActiveRoomId!.Value).Distinct().ToList();
        var activeRooms = activeRoomIds.Count == 0
            ? new Dictionary<Guid, GameRoom>()
            : await db.GameRooms.AsNoTracking().Where(r => activeRoomIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id);

        var visibleRooms = await GetVisibleRoomsAsync(db, userId);
        var totalGames = await db.GameSessions.AsNoTracking().CountAsync();
        var totalWins = await db.GameSessions.AsNoTracking().CountAsync(s => s.NetCoins > 0);
        var net = await db.GameSessions.AsNoTracking().SumAsync(s => (long?)s.NetCoins) ?? 0;

        return new DashboardViewModel
        {
            UserId = user.Id,
            UserName = user.UserName ?? "Player",
            FriendCode = user.FriendCode,
            Chips = wallet?.Balance ?? 0,
            DailyEarnings = net,
            TotalGames = totalGames,
            TotalWins = totalWins,
            RankLabel = wallet?.Balance >= 10_000 ? "High Roller" : wallet?.Balance >= 5_000 ? "Card Shark" : "Rising Chad",
            PendingRequests = requests.Select(r => new FriendRequestViewModel
            {
                RequestId = r.Id,
                RequesterUserId = r.RequesterUserId,
                RequesterName = requestUsers.TryGetValue(r.RequesterUserId, out var requester) ? requester.UserName ?? "Player" : "Player",
                RequesterFriendCode = requestUsers.TryGetValue(r.RequesterUserId, out requester) ? requester.FriendCode : string.Empty,
                CreatedUtc = r.CreatedUtc
            }).OrderByDescending(r => r.CreatedUtc).ToList(),
            Friends = friendUsers.OrderBy(f => f.UserName).Select(friend =>
            {
                activeRooms.TryGetValue(friend.ActiveRoomId ?? Guid.Empty, out var room);
                return new FriendSummaryViewModel
                {
                    FriendUserId = friend.Id,
                    Name = friend.UserName ?? "Player",
                    FriendCode = friend.FriendCode,
                    Status = ResolvePresenceStatus(friend),
                    ActiveRoomId = friend.ActiveRoomId,
                    ActiveRoomName = room?.Name,
                    ActiveGame = room?.Game,
                    CanJoinActiveRoom = room is not null && visibleRooms.Any(r => r.Id == room.Id)
                };
            }).ToList(),
            JoinableRooms = visibleRooms
                .OrderByDescending(r => r.Status == RoomStatus.InGame)
                .ThenBy(r => r.Name)
                .Select(MapRoom)
                .ToList()
        };
    }

    public async Task<(bool Ok, string Message)> RequestRoomDeletionAsync(Guid hostUserId, Guid roomId)
    {
        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var room = await db.GameRooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room is null || room.Status == RoomStatus.Closed)
        {
            return (false, "That table is no longer available.");
        }

        if (room.HostUserId != hostUserId)
        {
            return (false, "Only the host can delete this table.");
        }

        if (room.CloseRequested)
        {
            return (true, "This table is already scheduled to close.");
        }

        if (room.Status == RoomStatus.InGame)
        {
            room.CloseRequested = true;
            await db.SaveChangesAsync();
            return (true, "This table will close after the current round ends.");
        }

        await FinalizeRoomClosureAsync(db, room);
        await db.SaveChangesAsync();
        return (true, "Table deleted.");
    }

    public async Task<(bool Ok, string Message, Guid? RoomId)> CreateRoomAsync(Guid hostUserId, CreateRoomRequest request)
    {
        await EnsureUserSetupAsync(hostUserId);

        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var host = await db.Users.FirstOrDefaultAsync(u => u.Id == hostUserId);
        if (host is null)
        {
            return (false, "Host account not found.", null);
        }

        if (host.ActiveRoomId.HasValue)
        {
            return (false, "Leave your current table before creating a new one.", null);
        }

        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == hostUserId);
        var stack = wallet?.Balance ?? 0;
        var maxPlayers = request.Visibility == RoomVisibility.Private
            ? 1
            : request.Game == GameType.Poker ? 6 : 4;

        var room = new GameRoom
        {
            Code = await GenerateRoomCodeAsync(db),
            Name = request.Name.Trim(),
            Game = request.Game,
            Visibility = request.Visibility,
            Status = RoomStatus.Lobby,
            HostUserId = hostUserId,
            MinBet = Math.Max(10, request.MinBet),
            MaxPlayers = maxPlayers
        };

        db.GameRooms.Add(room);
        db.RoomPlayers.Add(new RoomPlayer
        {
            Room = room,
            UserId = hostUserId,
            DisplayName = host.UserName ?? "Host",
            Seat = 1,
            IsHost = true,
            IsConnected = true,
            Stack = stack,
            InitialStack = stack,
            LastHeartbeatUtc = DateTime.UtcNow
        });

        foreach (var invitedId in request.InvitedFriendIds.Distinct().Where(id => id != hostUserId))
        {
            db.GameRoomInvites.Add(new GameRoomInvite
            {
                Room = room,
                InviteeUserId = invitedId,
                InvitedByUserId = hostUserId
            });
        }

        host.ActiveRoomId = room.Id;
        host.PresenceStatus = UserPresenceStatus.InLobby;
        host.LastSeenUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        EnsureRuntime(room.Id, room.Game);
        await JoinRuntimeAsync(room.Id, hostUserId, host.UserName ?? "Host", stack, room.Game);
        await ConfigurePrivateRuntimeAsync(room.Id, room.Game, room.Visibility, room.MinBet);
        return (true, "Table created.", room.Id);
    }

    public async Task<(bool Ok, string Message)> JoinRoomAsync(Guid userId, Guid roomId)
    {
        await EnsureUserSetupAsync(userId);

        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var room = await db.GameRooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (user is null || room is null || room.Status == RoomStatus.Closed || room.CloseRequested)
        {
            return (false, "That table is not available.");
        }

        if (user.ActiveRoomId.HasValue && user.ActiveRoomId != roomId)
        {
            return (false, "Leave your current table before joining another one.");
        }

        if (!await CanJoinRoomAsync(db, userId, room))
        {
            return (false, "You do not have access to that table.");
        }

        var currentCount = await db.RoomPlayers.CountAsync(rp => rp.RoomId == roomId);
        if (currentCount >= room.MaxPlayers)
        {
            return (false, "That table is full.");
        }

        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        var stack = wallet?.Balance ?? 0;
        var roomPlayer = await db.RoomPlayers.FirstOrDefaultAsync(rp => rp.RoomId == roomId && rp.UserId == userId);
        if (roomPlayer is null)
        {
            roomPlayer = new RoomPlayer
            {
                RoomId = roomId,
                UserId = userId,
                DisplayName = user.UserName ?? "Player",
                Seat = currentCount + 1,
                IsConnected = true,
                Stack = stack,
                InitialStack = stack,
                LastHeartbeatUtc = DateTime.UtcNow
            };
            db.RoomPlayers.Add(roomPlayer);
        }
        else
        {
            roomPlayer.IsConnected = true;
            roomPlayer.LastHeartbeatUtc = DateTime.UtcNow;
            roomPlayer.DisconnectedUntilUtc = null;
            roomPlayer.DisplayName = user.UserName ?? "Player";
            if (room.Game == GameType.Poker && roomPlayer.Stack <= 0)
            {
                roomPlayer.Stack = stack;
                roomPlayer.InitialStack = stack;
            }
        }

        user.ActiveRoomId = roomId;
        user.PresenceStatus = room.Status == RoomStatus.InGame ? UserPresenceStatus.AtTable : UserPresenceStatus.InLobby;
        user.LastSeenUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        EnsureRuntime(roomId, room.Game);
        await JoinRuntimeAsync(roomId, userId, user.UserName ?? "Player", roomPlayer.Stack, room.Game);
        return (true, "Joined table.");
    }

    public async Task LeaveRoomAsync(Guid userId, Guid roomId, bool disconnected = false)
    {
        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();
        var room = await db.GameRooms.FirstOrDefaultAsync(r => r.Id == roomId);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var roomPlayer = await db.RoomPlayers.FirstOrDefaultAsync(rp => rp.RoomId == roomId && rp.UserId == userId);
        if (room is null || user is null || roomPlayer is null)
        {
            return;
        }

        if (disconnected)
        {
            roomPlayer.IsConnected = false;
            roomPlayer.DisconnectedUntilUtc = DateTime.UtcNow.Add(DisconnectGracePeriod);
            user.PresenceStatus = UserPresenceStatus.Offline;
            user.LastSeenUtc = DateTime.UtcNow;
            MarkDisconnectedInRuntime(roomId, room.Game, userId);
            await db.SaveChangesAsync();
            return;
        }

        if (room.Game == GameType.Poker)
        {
            var stack = GetPokerStack(roomId, userId, roomPlayer.Stack);
            var delta = stack - roomPlayer.InitialStack;
            if (delta != 0)
            {
                await walletService.AdjustBalanceAsync(userId, delta, $"poker-settle-{roomId}");
            }

            roomPlayer.Stack = stack;
            roomPlayer.InitialStack = stack;
        }

        db.RoomPlayers.Remove(roomPlayer);
        user.ActiveRoomId = null;
        user.PresenceStatus = UserPresenceStatus.Online;
        user.LastSeenUtc = DateTime.UtcNow;
        RemoveFromRuntime(roomId, room.Game, userId);

        var remainingPlayers = await db.RoomPlayers.Where(rp => rp.RoomId == roomId).ToListAsync();
        if (remainingPlayers.Count == 0)
        {
            room.Status = RoomStatus.Closed;
            room.ClosedUtc = DateTime.UtcNow;
            RemoveRuntime(roomId, room.Game);
        }
        else if (room.HostUserId == userId)
        {
            var nextHost = remainingPlayers.OrderBy(rp => rp.JoinedUtc).First();
            nextHost.IsHost = true;
            room.HostUserId = nextHost.UserId;
        }

        await db.SaveChangesAsync();
    }

    public async Task<BlackjackTableViewModel?> GetBlackjackTableAsync(Guid userId, Guid roomId)
    {
        await EnsureUserSetupAsync(userId);
        await SetPresenceAsync(userId, UserPresenceStatus.AtTable, roomId);

        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var room = await db.GameRooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId && r.Game == GameType.Blackjack);
        if (room is null)
        {
            return null;
        }

        EnsureRuntime(roomId, GameType.Blackjack);
        MaybeAutoStandDisconnected(roomId);
        var runtime = GetBlackjackRuntime(roomId);
        if (runtime is null)
        {
            return null;
        }

        if (runtime.Game.Phase == GamePhase.GameOver && !runtime.RoundSettled)
        {
            await SettleBlackjackRoundAsync(roomId);
        }

        var wallet = await db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId);
        return new BlackjackTableViewModel
        {
            RoomId = room.Id,
            RoomName = room.Name,
            RoomCode = room.Code,
            MinBet = room.MinBet,
            IsPrivate = room.Visibility == RoomVisibility.Private,
            IsClosing = room.CloseRequested,
            Balance = wallet?.Balance ?? 0,
            Game = runtime.Game,
            PendingBets = runtime.PendingBets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            LastRoundResults = runtime.LastRoundResults
                .Select(result => new BlackjackRoundResultViewModel
                {
                    UserId = result.UserId,
                    PlayerName = result.PlayerName,
                    PlayerScore = result.PlayerScore,
                    DealerScore = result.DealerScore,
                    Bet = result.Bet,
                    NetChips = result.NetChips,
                    ResultLabel = result.ResultLabel
                })
                .ToList()
        };
    }

    public async Task<(bool Ok, string Message)> PlaceBlackjackBetAsync(Guid userId, Guid roomId, long amount)
    {
        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();
        var room = await db.GameRooms.FirstOrDefaultAsync(r => r.Id == roomId && r.Game == GameType.Blackjack);
        if (room is null)
        {
            return (false, "Table not found.");
        }

        if (amount < room.MinBet)
        {
            return (false, $"Minimum bet is {room.MinBet} chips.");
        }

        EnsureRuntime(roomId, GameType.Blackjack);
        var runtime = GetBlackjackRuntime(roomId)!;
        if (runtime.PendingBets.ContainsKey(userId))
        {
            return (false, "You already bet for this round.");
        }

        var placed = await walletService.TryPlaceBetAsync(userId, amount, $"blackjack-bet-{roomId}-{DateTime.UtcNow:yyyyMMddHHmmss}");
        if (!placed)
        {
            return (false, "Insufficient chips.");
        }

        lock (_syncRoot)
        {
            runtime.PendingBets[userId] = amount;
            var player = runtime.Game.Players.FirstOrDefault(p => p.UserId == userId);
            if (player is not null)
            {
                player.IsReady = true;
                player.CurrentBet = amount;
            }

            var activePlayers = runtime.Game.Players.Where(p => !p.IsDisconnected).ToList();
            var requiredPlayers = room.Visibility == RoomVisibility.Private ? 1 : 2;
            if (activePlayers.Count >= requiredPlayers && activePlayers.All(p => runtime.PendingBets.ContainsKey(p.UserId)))
            {
                runtime.RoundSettled = false;
                runtime.LastRoundResults.Clear();
                runtime.Game.StartNewRound();
            }
        }

        room.Status = RoomStatus.InGame;
        await db.SaveChangesAsync();
        return (true, "Bet placed.");
    }

    public Task PlayerHitAsync(Guid userId, Guid roomId)
    {
        var runtime = GetBlackjackRuntime(roomId);
        var playerName = runtime?.Game.Players.FirstOrDefault(p => p.UserId == userId)?.Name;
        if (!string.IsNullOrWhiteSpace(playerName))
        {
            runtime!.Game.PlayerHit(playerName);
        }

        return Task.CompletedTask;
    }

    public async Task PlayerStandAsync(Guid userId, Guid roomId)
    {
        var runtime = GetBlackjackRuntime(roomId);
        var playerName = runtime?.Game.Players.FirstOrDefault(p => p.UserId == userId)?.Name;
        if (!string.IsNullOrWhiteSpace(playerName))
        {
            runtime!.Game.PlayerStand(playerName);
        }

        if (runtime?.Game.Phase == GamePhase.GameOver && !runtime.RoundSettled)
        {
            await SettleBlackjackRoundAsync(roomId);
        }
    }

    public async Task<PokerTableViewModel?> GetPokerTableAsync(Guid userId, Guid roomId)
    {
        await EnsureUserSetupAsync(userId);
        await SetPresenceAsync(userId, UserPresenceStatus.AtTable, roomId);

        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var room = await db.GameRooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId && r.Game == GameType.Poker);
        if (room is null)
        {
            return null;
        }

        EnsureRuntime(roomId, GameType.Poker);
        await ConfigurePrivateRuntimeAsync(roomId, room.Game, room.Visibility, room.MinBet);
        MaybeAutoFoldDisconnected(roomId);
        var runtime = GetPokerRuntime(roomId);
        if (runtime is null)
        {
            return null;
        }

        if (room.Visibility == RoomVisibility.Private &&
            runtime.Game.Phase == PokerPhase.Waiting &&
            runtime.Game.Players.Count >= 2)
        {
            runtime.HandSettled = false;
            runtime.Game.StartNewRound();
        }

        if (runtime.Game.Phase == PokerPhase.GameOver && !runtime.HandSettled)
        {
            await SettlePokerHandAsync(roomId);
        }

        var wallet = await db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId);
        return new PokerTableViewModel
        {
            RoomId = room.Id,
            RoomName = room.Name,
            RoomCode = room.Code,
            MinBet = room.MinBet,
            IsPrivate = room.Visibility == RoomVisibility.Private,
            Balance = wallet?.Balance ?? 0,
            Game = runtime.Game,
            IsClosing = room.CloseRequested
        };
    }

    public Task<(bool Ok, string Message)> StartPokerRoundAsync(Guid roomId)
    {
        var runtime = GetPokerRuntime(roomId);
        if (runtime is null)
        {
            return Task.FromResult((false, "Table not found."));
        }

        lock (_syncRoot)
        {
            runtime.HandSettled = false;
            runtime.Game.StartNewRound();
        }

        return Task.FromResult((true, "Round started."));
    }

    public async Task ProcessPokerActionAsync(Guid userId, Guid roomId, PokerAction action, long amount = 0)
    {
        var runtime = GetPokerRuntime(roomId);
        var player = runtime?.Game.Players.FirstOrDefault(p => p.UserId == userId);
        if (runtime is null || player is null)
        {
            return;
        }

        runtime.Game.ProcessPlayerAction(player.Name, action, amount);
        await SyncPokerStacksAsync(roomId);

        if (runtime.Game.Phase == PokerPhase.GameOver && !runtime.HandSettled)
        {
            await SettlePokerHandAsync(roomId);
        }
    }

    private AsyncServiceScope CreateAsyncScope() => _scopeFactory.CreateAsyncScope();

    private async Task<string> GenerateFriendCodeAsync(AppDbContext db, Guid? preferredUserId = null)
    {
        if (preferredUserId.HasValue)
        {
            var raw = preferredUserId.Value.ToString("N").ToUpperInvariant();
            var preferredCode = $"C2C-{raw[..6]}-{raw[6..12]}";
            var exists = await db.Users.AnyAsync(u => u.FriendCode == preferredCode && u.Id != preferredUserId.Value);
            if (!exists)
            {
                return preferredCode;
            }
        }

        string code;
        do
        {
            code = $"C2C-{Random.Shared.Next(100000, 999999)}-{Random.Shared.Next(100000, 999999)}";
        }
        while (await db.Users.AnyAsync(u => u.FriendCode == code));

        return code;
    }

    private async Task CleanupExpiredDisconnectsAsync()
    {
        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var expiredPlayers = await db.RoomPlayers
            .Where(rp => !rp.IsConnected && rp.DisconnectedUntilUtc.HasValue && rp.DisconnectedUntilUtc < DateTime.UtcNow)
            .ToListAsync();
        if (expiredPlayers.Count == 0)
        {
            return;
        }

        foreach (var roomPlayer in expiredPlayers)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == roomPlayer.UserId);
            if (user is not null && user.ActiveRoomId == roomPlayer.RoomId)
            {
                user.ActiveRoomId = null;
                user.PresenceStatus = UserPresenceStatus.Offline;
            }

            db.RoomPlayers.Remove(roomPlayer);
        }

        await db.SaveChangesAsync();
    }

    private async Task SettleBlackjackRoundAsync(Guid roomId)
    {
        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();
        var runtime = GetBlackjackRuntime(roomId);
        if (runtime is null || runtime.RoundSettled)
        {
            return;
        }

        foreach (var player in runtime.Game.Players)
        {
            if (!runtime.PendingBets.TryGetValue(player.UserId, out var bet))
            {
                continue;
            }

            var payout = player.HasWon ? bet * 2 : player.IsPush ? bet : 0;
            var netChips = payout - bet;
            var resultLabel = player.HasWon ? "Won" : player.IsPush ? "Push" : "Lost";
            if (payout > 0)
            {
                await walletService.CreditPayoutAsync(player.UserId, payout, $"blackjack-payout-{roomId}-{DateTime.UtcNow:yyyyMMddHHmmss}");
            }

            runtime.LastRoundResults.RemoveAll(result => result.UserId == player.UserId);
            runtime.LastRoundResults.Add(new BlackjackRoundResult
            {
                UserId = player.UserId,
                PlayerName = player.Name,
                PlayerScore = player.Score,
                DealerScore = runtime.Game.Dealer.Score,
                Bet = bet,
                NetChips = netChips,
                ResultLabel = resultLabel
            });

            db.GameSessions.Add(new GameSession
            {
                RoomId = roomId,
                Game = GameType.Blackjack,
                StateJson = $"{{\"player\":\"{player.Name}\",\"score\":{player.Score},\"dealer\":{runtime.Game.Dealer.Score}}}",
                EndedUtc = DateTime.UtcNow,
                NetCoins = netChips
            });
        }

        runtime.PendingBets.Clear();
        runtime.RoundSettled = true;
        var room = await db.GameRooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room is not null)
        {
            if (room.CloseRequested)
            {
                await FinalizeRoomClosureAsync(db, room);
            }
            else
            {
                room.Status = RoomStatus.Lobby;
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task SettlePokerHandAsync(Guid roomId)
    {
        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var walletService = scope.ServiceProvider.GetRequiredService<WalletService>();
        var runtime = GetPokerRuntime(roomId);
        if (runtime is null || runtime.HandSettled)
        {
            return;
        }

        foreach (var player in runtime.Game.Players.Where(p => !p.IsBot))
        {
            var delta = player.Gold - player.InitialGold;
            if (delta != 0)
            {
                await walletService.AdjustBalanceAsync(player.UserId, delta, $"poker-hand-{roomId}");
                player.InitialGold = player.Gold;
            }

            var roomPlayer = await db.RoomPlayers.FirstOrDefaultAsync(rp => rp.RoomId == roomId && rp.UserId == player.UserId);
            if (roomPlayer is not null)
            {
                roomPlayer.Stack = player.Gold;
                roomPlayer.InitialStack = player.Gold;
            }

            db.GameSessions.Add(new GameSession
            {
                RoomId = roomId,
                Game = GameType.Poker,
                StateJson = $"{{\"player\":\"{player.Name}\",\"winner\":\"{runtime.Game.WinnerName}\"}}",
                EndedUtc = DateTime.UtcNow,
                NetCoins = delta
            });
        }

        runtime.HandSettled = true;
        var room = await db.GameRooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room is not null)
        {
            if (room.CloseRequested)
            {
                await FinalizeRoomClosureAsync(db, room);
            }
            else
            {
                room.Status = RoomStatus.Lobby;
            }
        }

        await db.SaveChangesAsync();
    }

    private static FriendStatus ResolvePresenceStatus(ApplicationUser user)
    {
        if (DateTime.UtcNow - user.LastSeenUtc > OfflineThreshold)
        {
            return FriendStatus.Offline;
        }

        return user.PresenceStatus switch
        {
            UserPresenceStatus.InLobby => FriendStatus.InLobby,
            UserPresenceStatus.AtTable => FriendStatus.AtTable,
            _ => FriendStatus.Online
        };
    }

    private async Task<List<GameRoom>> GetVisibleRoomsAsync(AppDbContext db, Guid userId)
    {
        var friendshipIds = await db.Friendships.AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.FriendUserId)
            .ToListAsync();
        var invitedRoomIds = await db.GameRoomInvites.AsNoTracking()
            .Where(i => i.InviteeUserId == userId)
            .Select(i => i.RoomId)
            .ToListAsync();

        return await db.GameRooms.AsNoTracking()
            .Where(r => r.Status != RoomStatus.Closed && !r.CloseRequested)
            .Where(r =>
                r.HostUserId == userId ||
                (r.Visibility == RoomVisibility.FriendsOnly && friendshipIds.Contains(r.HostUserId)) ||
                (r.Visibility == RoomVisibility.Private && invitedRoomIds.Contains(r.Id)))
            .ToListAsync();
    }

    private async Task<bool> CanJoinRoomAsync(AppDbContext db, Guid userId, GameRoom room)
    {
        if (room.HostUserId == userId)
        {
            return true;
        }

        if (room.Visibility == RoomVisibility.Private)
        {
            return await db.GameRoomInvites.AnyAsync(i => i.RoomId == room.Id && i.InviteeUserId == userId);
        }

        return await db.Friendships.AnyAsync(f => f.UserId == userId && f.FriendUserId == room.HostUserId);
    }

    private RoomSummaryViewModel MapRoom(GameRoom room)
        => new()
        {
            RoomId = room.Id,
            Name = room.Name,
            Code = room.Code,
            Game = room.Game,
            Visibility = room.Visibility,
            MinBet = room.MinBet,
            CurrentPlayers = GetCurrentPlayerCount(room.Id, room.Game),
            MaxPlayers = room.MaxPlayers,
            Status = room.Status,
            HostUserId = room.HostUserId,
            IsHost = false,
            IsClosing = room.CloseRequested
        };

    private async Task FinalizeRoomClosureAsync(AppDbContext db, GameRoom room)
    {
        var roomPlayers = await db.RoomPlayers.Where(rp => rp.RoomId == room.Id).ToListAsync();
        var userIds = roomPlayers.Select(rp => rp.UserId).Distinct().ToList();
        var users = userIds.Count == 0
            ? new List<ApplicationUser>()
            : await db.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

        foreach (var user in users)
        {
            if (user.ActiveRoomId == room.Id)
            {
                user.ActiveRoomId = null;
                user.PresenceStatus = UserPresenceStatus.Online;
                user.LastSeenUtc = DateTime.UtcNow;
            }
        }

        db.RoomPlayers.RemoveRange(roomPlayers);
        room.Status = RoomStatus.Closed;
        room.CloseRequested = true;
        room.ClosedUtc = DateTime.UtcNow;
        RemoveRuntime(room.Id, room.Game);
    }

    private int GetCurrentPlayerCount(Guid roomId, GameType game)
    {
        lock (_syncRoot)
        {
            return game switch
            {
                GameType.Blackjack when _blackjackRooms.TryGetValue(roomId, out var blackjack) => blackjack.Game.Players.Count,
                GameType.Poker when _pokerRooms.TryGetValue(roomId, out var poker) => poker.Game.Players.Count(p => !p.IsBot),
                _ => 0
            };
        }
    }

    private void EnsureRuntime(Guid roomId, GameType game)
    {
        lock (_syncRoot)
        {
            if (game == GameType.Blackjack && !_blackjackRooms.ContainsKey(roomId))
            {
                _blackjackRooms[roomId] = new BlackjackRoomRuntime(roomId);
            }
            else if (game == GameType.Poker && !_pokerRooms.ContainsKey(roomId))
            {
                _pokerRooms[roomId] = new PokerRoomRuntime(roomId);
            }
        }
    }

    private Task JoinRuntimeAsync(Guid roomId, Guid userId, string displayName, long stack, GameType game)
    {
        lock (_syncRoot)
        {
            if (game == GameType.Blackjack)
            {
                var runtime = GetBlackjackRuntime(roomId);
                if (runtime is not null && runtime.Game.Players.All(p => p.UserId != userId))
                {
                    runtime.Game.Players.Add(new Player { UserId = userId, Name = displayName });
                }
            }
            else if (game == GameType.Poker)
            {
                var runtime = GetPokerRuntime(roomId);
                if (runtime is not null)
                {
                    var player = runtime.Game.Players.FirstOrDefault(p => p.UserId == userId);
                    if (player is null)
                    {
                        runtime.Game.Players.Add(new PokerPlayer
                        {
                            UserId = userId,
                            Name = displayName,
                            Gold = stack,
                            InitialGold = stack
                        });
                    }
                    else
                    {
                        player.IsDisconnected = false;
                        player.Name = displayName;
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    private async Task ConfigurePrivateRuntimeAsync(Guid roomId, GameType game, RoomVisibility visibility, long minBet)
    {
        if (visibility != RoomVisibility.Private || game != GameType.Poker)
        {
            return;
        }

        var runtime = GetPokerRuntime(roomId);
        if (runtime is null)
        {
            return;
        }

        lock (_syncRoot)
        {
            runtime.Game.SmallBlindAmount = Math.Max(10, minBet);
            runtime.Game.BigBlindAmount = Math.Max(runtime.Game.SmallBlindAmount * 2, 20);

            if (runtime.Game.Players.All(p => !p.IsBot))
            {
                runtime.Game.Players.Add(new PokerPlayer
                {
                    UserId = Guid.Empty,
                    Name = "ChadBot (AI)",
                    IsBot = true,
                    Gold = 1_000,
                    InitialGold = 1_000
                });
            }
        }

        await Task.CompletedTask;
    }

    private void RemoveFromRuntime(Guid roomId, GameType game, Guid userId)
    {
        lock (_syncRoot)
        {
            if (game == GameType.Blackjack && _blackjackRooms.TryGetValue(roomId, out var blackjack))
            {
                blackjack.Game.Players.RemoveAll(p => p.UserId == userId);
                blackjack.PendingBets.Remove(userId);
            }
            else if (game == GameType.Poker && _pokerRooms.TryGetValue(roomId, out var poker))
            {
                poker.Game.Players.RemoveAll(p => p.UserId == userId);
            }
        }
    }

    private void RemoveRuntime(Guid roomId, GameType game)
    {
        lock (_syncRoot)
        {
            if (game == GameType.Blackjack)
            {
                _blackjackRooms.Remove(roomId);
            }
            else if (game == GameType.Poker)
            {
                _pokerRooms.Remove(roomId);
            }
        }
    }

    private void MarkDisconnectedInRuntime(Guid roomId, GameType game, Guid userId)
    {
        lock (_syncRoot)
        {
            if (game == GameType.Blackjack && _blackjackRooms.TryGetValue(roomId, out var blackjack))
            {
                var player = blackjack.Game.Players.FirstOrDefault(p => p.UserId == userId);
                if (player is not null)
                {
                    player.IsDisconnected = true;
                }
            }
            else if (game == GameType.Poker && _pokerRooms.TryGetValue(roomId, out var poker))
            {
                var player = poker.Game.Players.FirstOrDefault(p => p.UserId == userId);
                if (player is not null)
                {
                    player.IsDisconnected = true;
                }
            }
        }
    }

    private void MaybeAutoStandDisconnected(Guid roomId)
    {
        lock (_syncRoot)
        {
            if (_blackjackRooms.TryGetValue(roomId, out var runtime) &&
                runtime.Game.CurrentPlayer is { IsDisconnected: true } player)
            {
                runtime.Game.PlayerStand(player.Name);
            }
        }
    }

    private void MaybeAutoFoldDisconnected(Guid roomId)
    {
        lock (_syncRoot)
        {
            if (_pokerRooms.TryGetValue(roomId, out var runtime) &&
                runtime.Game.CurrentPlayer is { IsDisconnected: true } player)
            {
                runtime.Game.ProcessPlayerAction(player.Name, PokerAction.Fold);
            }
        }
    }

    private async Task SyncPokerStacksAsync(Guid roomId)
    {
        await using var scope = CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var runtime = GetPokerRuntime(roomId);
        if (runtime is null)
        {
            return;
        }

        foreach (var player in runtime.Game.Players.Where(p => !p.IsBot))
        {
            var roomPlayer = await db.RoomPlayers.FirstOrDefaultAsync(rp => rp.RoomId == roomId && rp.UserId == player.UserId);
            if (roomPlayer is not null)
            {
                roomPlayer.Stack = player.Gold;
            }
        }

        await db.SaveChangesAsync();
    }

    private long GetPokerStack(Guid roomId, Guid userId, long fallback)
    {
        lock (_syncRoot)
        {
            if (_pokerRooms.TryGetValue(roomId, out var runtime))
            {
                return runtime.Game.Players.FirstOrDefault(p => p.UserId == userId)?.Gold ?? fallback;
            }
        }

        return fallback;
    }

    private BlackjackRoomRuntime? GetBlackjackRuntime(Guid roomId)
    {
        lock (_syncRoot)
        {
            _blackjackRooms.TryGetValue(roomId, out var runtime);
            return runtime;
        }
    }

    private PokerRoomRuntime? GetPokerRuntime(Guid roomId)
    {
        lock (_syncRoot)
        {
            _pokerRooms.TryGetValue(roomId, out var runtime);
            return runtime;
        }
    }

    private async Task<string> GenerateRoomCodeAsync(AppDbContext db)
    {
        string code;
        do
        {
            code = $"TBL-{Random.Shared.Next(100000, 999999)}";
        }
        while (await db.GameRooms.AnyAsync(r => r.Code == code));

        return code;
    }

    private sealed class BlackjackRoomRuntime
    {
        public BlackjackRoomRuntime(Guid roomId)
        {
            Game = new PlayBlackjack(roomId.ToString());
        }

        public PlayBlackjack Game { get; }
        public Dictionary<Guid, long> PendingBets { get; } = new();
        public List<BlackjackRoundResult> LastRoundResults { get; } = new();
        public bool RoundSettled { get; set; }
    }

    private sealed class PokerRoomRuntime
    {
        public PokerRoomRuntime(Guid roomId)
        {
            Game = new TexasHoldemGame(roomId.ToString());
        }

        public TexasHoldemGame Game { get; }
        public bool HandSettled { get; set; }
    }
}

public enum FriendStatus
{
    Offline,
    Online,
    InLobby,
    AtTable
}

public class FriendRequestViewModel
{
    public Guid RequestId { get; set; }
    public Guid RequesterUserId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterFriendCode { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public class FriendSummaryViewModel
{
    public Guid FriendUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FriendCode { get; set; } = string.Empty;
    public FriendStatus Status { get; set; }
    public Guid? ActiveRoomId { get; set; }
    public string? ActiveRoomName { get; set; }
    public GameType? ActiveGame { get; set; }
    public bool CanJoinActiveRoom { get; set; }
}

public class DashboardViewModel
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FriendCode { get; set; } = string.Empty;
    public long Chips { get; set; }
    public long DailyEarnings { get; set; }
    public int TotalWins { get; set; }
    public int TotalGames { get; set; }
    public string RankLabel { get; set; } = string.Empty;
    public List<FriendRequestViewModel> PendingRequests { get; set; } = new();
    public List<FriendSummaryViewModel> Friends { get; set; } = new();
    public List<RoomSummaryViewModel> JoinableRooms { get; set; } = new();
}

public class RoomSummaryViewModel
{
    public Guid RoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public GameType Game { get; set; }
    public RoomVisibility Visibility { get; set; }
    public long MinBet { get; set; }
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public RoomStatus Status { get; set; }
    public Guid HostUserId { get; set; }
    public bool IsHost { get; set; }
    public bool IsClosing { get; set; }
}

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public GameType Game { get; set; }
    public RoomVisibility Visibility { get; set; } = RoomVisibility.FriendsOnly;
    public long MinBet { get; set; } = 100;
    public List<Guid> InvitedFriendIds { get; set; } = new();
}

public class BlackjackTableViewModel
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public long Balance { get; set; }
    public long MinBet { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsClosing { get; set; }
    public PlayBlackjack Game { get; set; } = default!;
    public Dictionary<Guid, long> PendingBets { get; set; } = new();
    public List<BlackjackRoundResultViewModel> LastRoundResults { get; set; } = new();
}

public class PokerTableViewModel
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public long Balance { get; set; }
    public long MinBet { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsClosing { get; set; }
    public TexasHoldemGame Game { get; set; } = default!;
}

public class BlackjackRoundResultViewModel
{
    public Guid UserId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int PlayerScore { get; set; }
    public int DealerScore { get; set; }
    public long Bet { get; set; }
    public long NetChips { get; set; }
    public string ResultLabel { get; set; } = string.Empty;
}

internal sealed class BlackjackRoundResult
{
    public Guid UserId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int PlayerScore { get; set; }
    public int DealerScore { get; set; }
    public long Bet { get; set; }
    public long NetChips { get; set; }
    public string ResultLabel { get; set; } = string.Empty;
}
