using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chuds2Chads.Games.Blackjack;

namespace Chuds2Chads.Games.Poker
{
    public class TexasHoldemGame
    {
        public string LobbyName { get; set; }
        public PokerPhase Phase { get; private set; } = PokerPhase.Waiting;
        
        public Deck Deck { get; private set; } = new Deck();
        public List<PokerPlayer> Players { get; set; } = new();
        public List<Card> CommunityCards { get; private set; } = new();
        
        public long Pot { get; private set; } = 0;
        public long CurrentHighestBet { get; private set; } = 0;
        
        // Turn Order & Blinds
        public int CurrentTurnIndex { get; private set; } = 0;
        public int DealerIndex { get; private set; } = 0; 
        public long SmallBlindAmount { get; set; } = 10;
        public long BigBlindAmount { get; set; } = 20;

        public PokerPlayer? CurrentPlayer => Players.ElementAtOrDefault(CurrentTurnIndex);
        public string WinnerName { get; private set; } = "";
        public long WinAmount { get; private set; } = 0;

        public Action? OnGameStateChanged;
        private Random _rng = new Random();

        public TexasHoldemGame(string lobbyName)
        {
            LobbyName = lobbyName;
        }

        public void StartNewRound()
        {
            if (Players.Count < 2) return;

            // Kick players who are completely out of gold
            Players.RemoveAll(p => p.Gold <= 0);
            if (Players.Count < 2)
            {
                Phase = PokerPhase.Waiting;
                NotifyStateChanged();
                return;
            }

            Phase = PokerPhase.PreFlop;
            Deck.InitializeAndShuffle();
            CommunityCards.Clear();
            Pot = 0;
            WinnerName = "";

            // Rotate Dealer
            DealerIndex = (DealerIndex + 1) % Players.Count;

            foreach (var player in Players)
            {
                player.ResetForNewRound();
                player.Hand.Add(Deck.DrawCard());
                player.Hand.Add(Deck.DrawCard());
            }

            PostBlinds();

            // In Heads Up (1v1), the Dealer is the Small Blind and acts FIRST pre-flop.
            if (Players.Count == 2)
            {
                CurrentTurnIndex = DealerIndex; 
            }
            else
            {
                // Standard poker: Player after Big Blind acts first pre-flop
                CurrentTurnIndex = (DealerIndex + 3) % Players.Count;
            }

            NotifyStateChanged();
            CheckIfBotTurn();
        }

        private void PostBlinds()
        {
            int sbIndex = Players.Count == 2 ? DealerIndex : (DealerIndex + 1) % Players.Count;
            int bbIndex = Players.Count == 2 ? (DealerIndex + 1) % 2 : (DealerIndex + 2) % Players.Count;

            var sbPlayer = Players[sbIndex];
            var bbPlayer = Players[bbIndex];

            // Post Small Blind
            long actualSb = Math.Min(sbPlayer.Gold, SmallBlindAmount);
            sbPlayer.Gold -= actualSb;
            sbPlayer.CurrentBet = actualSb;
            Pot += actualSb;

            // Post Big Blind
            long actualBb = Math.Min(bbPlayer.Gold, BigBlindAmount);
            bbPlayer.Gold -= actualBb;
            bbPlayer.CurrentBet = actualBb;
            Pot += actualBb;

            CurrentHighestBet = BigBlindAmount;
        }

        public void ProcessPlayerAction(string playerName, PokerAction action, long betAmount = 0)
        {
            if (CurrentPlayer == null || CurrentPlayer.Name != playerName || Phase == PokerPhase.GameOver) return;

            var player = CurrentPlayer;
            player.HasActedThisRound = true;

            switch (action)
            {
                case PokerAction.Fold:
                    player.HasFolded = true;
                    break;
                case PokerAction.Check:
                    if (player.CurrentBet < CurrentHighestBet) return; // Illegal check
                    break;
                case PokerAction.Call:
                    long callAmount = Math.Min(player.Gold, CurrentHighestBet - player.CurrentBet);
                    PlaceBet(player, callAmount);
                    break;
                case PokerAction.Raise:
                    long totalBet = CurrentHighestBet + betAmount;
                    long raiseAmount = Math.Min(player.Gold, totalBet - player.CurrentBet);
                    PlaceBet(player, raiseAmount);
                    break;
            }

            AdvanceTurn();
        }

        private void PlaceBet(PokerPlayer player, long amount)
        {
            player.Gold -= amount;
            player.CurrentBet += amount;
            Pot += amount;
            
            if (player.CurrentBet > CurrentHighestBet)
            {
                CurrentHighestBet = player.CurrentBet;
                // Everyone else must act again to match the new raise
                foreach (var p in Players.Where(p => p != player && !p.HasFolded && !p.IsAllIn))
                {
                    p.HasActedThisRound = false; 
                }
            }
        }

        private async void AdvanceTurn()
        {
            var activePlayers = Players.Where(p => !p.HasFolded).ToList();
            
            if (activePlayers.Count == 1)
            {
                await HandleWinner(new List<PokerPlayer> { activePlayers.First() }, "Folded");
                return;
            }

            // Move to next active player
            do {
                CurrentTurnIndex = (CurrentTurnIndex + 1) % Players.Count;
            } while (CurrentPlayer!.HasFolded || CurrentPlayer.IsAllIn);

            // Check if betting round is over
            bool bettingRoundOver = Players.Where(p => !p.HasFolded && !p.IsAllIn)
                                           .All(p => p.HasActedThisRound && p.CurrentBet == CurrentHighestBet);

            // If everyone is All-In (or folded), fast-forward the game!
            bool onlyOnePlayerNotAllIn = Players.Count(p => !p.HasFolded && !p.IsAllIn) <= 1;
            if (bettingRoundOver || onlyOnePlayerNotAllIn)
            {
                await AdvancePhaseWithDelay();
            }
            else
            {
                NotifyStateChanged();
                CheckIfBotTurn();
            }
        }

        private async Task AdvancePhaseWithDelay()
        {
            foreach (var p in Players) { p.HasActedThisRound = false; p.CurrentBet = 0; }
            CurrentHighestBet = 0;
            
            await Task.Delay(1500); 

            if (Phase == PokerPhase.PreFlop) {
                Phase = PokerPhase.Flop;
                CommunityCards.AddRange(new[] { Deck.DrawCard(), Deck.DrawCard(), Deck.DrawCard() });
            }
            else if (Phase == PokerPhase.Flop) {
                Phase = PokerPhase.Turn;
                CommunityCards.Add(Deck.DrawCard());
            }
            else if (Phase == PokerPhase.Turn) {
                Phase = PokerPhase.River;
                CommunityCards.Add(Deck.DrawCard());
            }
            else if (Phase == PokerPhase.River) {
                Phase = PokerPhase.Showdown;
                await EvaluateHands(); 
                return;
            }

            // Post-Flop: The player AFTER the dealer acts first
            CurrentTurnIndex = (DealerIndex + 1) % Players.Count;
            while (CurrentPlayer!.HasFolded || CurrentPlayer.IsAllIn)
            {
                CurrentTurnIndex = (CurrentTurnIndex + 1) % Players.Count;
            }

            // If we are fast-forwarding an All-In, skip betting entirely
            if (Players.Count(p => !p.HasFolded && !p.IsAllIn) <= 1)
            {
                AdvanceTurn();
                return;
            }

            NotifyStateChanged();
            CheckIfBotTurn();
        }

        private async Task EvaluateHands()
        {
            var activePlayers = Players.Where(p => !p.HasFolded).ToList();
            if (activePlayers.Count == 1)
            {
                await HandleWinner(new List<PokerPlayer> { activePlayers[0] }, "Folded");
                return;
            }

            var evaluations = activePlayers.Select(p => PokerHandEvaluator.Evaluate(p, CommunityCards)).ToList();
            evaluations.Sort();
            evaluations.Reverse(); 

            var bestEvaluation = evaluations.First();
            var winners = evaluations.Where(e => e.CompareTo(bestEvaluation) == 0).Select(e => e.Player).ToList();

            await HandleWinner(winners, bestEvaluation.Rank.ToString());
        }

        private async Task HandleWinner(List<PokerPlayer> winners, string winningHandText)
        {
            Phase = PokerPhase.GameOver;
            long splitAmount = Pot / winners.Count;
            WinAmount = splitAmount;
            
            foreach(var winner in winners) winner.Gold += splitAmount;

            if (winners.Count == 1)
            {
                string cleanHandName = System.Text.RegularExpressions.Regex.Replace(winningHandText, "([A-Z])", " $1").Trim();
                WinnerName = winningHandText == "Folded" ? $"{winners[0].Name} WINS (Opponent Folded)" : $"{winners[0].Name} WINS ({cleanHandName})";
            }
            else
            {
                WinnerName = $"SPLIT POT: {string.Join(" & ", winners.Select(w => w.Name))}";
            }

            Pot = 0;
            NotifyStateChanged();

            await Task.CompletedTask;
        }

        private async void CheckIfBotTurn()
        {
            if (CurrentPlayer != null && CurrentPlayer.IsBot && Phase != PokerPhase.GameOver)
            {
                await Task.Delay(1500); // Bot "thinking"
                
                long costToCall = CurrentHighestBet - CurrentPlayer.CurrentBet;
                
                // Smarter Bot Logic
                if (costToCall > 0)
                {
                    // If the bet is huge (more than half the bot's gold), it has a 50% chance to chicken out and fold
                    if (costToCall > (CurrentPlayer.Gold / 2) && _rng.Next(100) < 50)
                    {
                        ProcessPlayerAction(CurrentPlayer.Name, PokerAction.Fold);
                    }
                    else
                    {
                        ProcessPlayerAction(CurrentPlayer.Name, PokerAction.Call);
                    }
                }
                else
                {
                    // Sometimes the bot will randomly raise if nobody has bet! (20% chance)
                    if (_rng.Next(100) < 20 && CurrentPlayer.Gold > 20)
                    {
                        ProcessPlayerAction(CurrentPlayer.Name, PokerAction.Raise, 20);
                    }
                    else
                    {
                        ProcessPlayerAction(CurrentPlayer.Name, PokerAction.Check);
                    }
                }
            }
        }

        private void NotifyStateChanged() => OnGameStateChanged?.Invoke();
    }
}
