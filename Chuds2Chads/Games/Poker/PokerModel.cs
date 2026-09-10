using System.Collections.Generic;
using Chuds2Chads.Games.Blackjack; // Reusing your Card and Deck classes!

namespace Chuds2Chads.Games.Poker
{
    public enum PokerPhase { Waiting, PreFlop, Flop, Turn, River, Showdown, GameOver }
    public enum PokerAction { Fold, Check, Call, Raise }

    public class PokerPlayer
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Card> Hand { get; set; } = new();
        
        // Banking & Betting
        public long Gold { get; set; } = 1000;
        public long InitialGold { get; set; } = 1000;
        public long CurrentBet { get; set; } = 0;
        
        // State
        public bool HasFolded { get; set; }
        public bool IsBot { get; set; }
        public bool IsDisconnected { get; set; }
        public bool IsAllIn => Gold == 0 && CurrentBet > 0;
        public bool HasActedThisRound { get; set; }

        public void ResetForNewRound()
        {
            Hand.Clear();
            CurrentBet = 0;
            HasFolded = false;
            HasActedThisRound = false;
        }
    }

    public class PokerTableInfo
    {
        public string LobbyName { get; set; } = string.Empty;
        public List<string> Players { get; set; } = new();
        public int MaxPlayers { get; set; } = 4;
        public bool IsFull => Players.Count >= MaxPlayers;
    }
}
