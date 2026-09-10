using System;
using System.Collections.Generic;

namespace Chuds2Chads.Data.Entities;

public enum GameType
{
    Blackjack = 1,
    Roulette = 2,
    Slots = 3,
    Poker = 4,
    HorseRace = 5
}

public enum RoomStatus
{
    Lobby = 1,
    InGame = 2,
    Closed = 3
}

public enum RoomVisibility
{
    Private = 1,
    FriendsOnly = 2
}

public class GameRoom
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public GameType Game { get; set; }

    public RoomVisibility Visibility { get; set; } = RoomVisibility.FriendsOnly;

    public RoomStatus Status { get; set; } = RoomStatus.Lobby;

    public Guid HostUserId { get; set; }

    public long MinBet { get; set; }

    public int MaxPlayers { get; set; } = 4;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public bool CloseRequested { get; set; }

    public DateTime? ClosedUtc { get; set; }

    public ICollection<RoomPlayer> Players { get; set; } = new List<RoomPlayer>();
}
