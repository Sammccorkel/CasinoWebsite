using System;
using System.Collections.Generic;
using System.Linq;

namespace Chuds2Chads.Games.Blackjack
{
    public class PlayBlackjack
    {
        public string LobbyName { get; set; }
        public GamePhase Phase { get; private set; } = GamePhase.WaitingForPlayers;

        public IDeck Deck { get; private set; }
        public Player Dealer { get; private set; } = new Player { Name = "Dealer", IsDealer = true };
        public List<Player> Players { get; set; } = new();

        public int CurrentPlayerIndex { get; private set; } = 0;
        public Player? CurrentPlayer => Players.ElementAtOrDefault(CurrentPlayerIndex);
        public bool DealerRevealed { get; private set; } = false;

        public Action? OnGameStateChanged;

        public PlayBlackjack(string lobbyName, IDeck? deck = null)
        {
            LobbyName = lobbyName;
            Deck = deck ?? new Deck();
        }

        public void StartNewRound()
        {
            if (Players.Count == 0) return;

            Phase = GamePhase.Dealing;
            Deck.InitializeAndShuffle();
            Dealer.Reset();
            DealerRevealed = false;

            foreach (var player in Players)
            {
                player.Reset();
                player.Hit(Deck.DrawCard());
                player.Hit(Deck.DrawCard());
            }

            Dealer.Hit(Deck.DrawCard());
            Dealer.Hit(Deck.DrawCard());

            CurrentPlayerIndex = 0;
            Phase = GamePhase.PlayerTurn;

            CheckCurrentPlayerStatus();
            NotifyStateChanged();
        }

        public void PlayerHit(string playerName)
        {
            if (Phase != GamePhase.PlayerTurn || CurrentPlayer?.Name != playerName) return;

            CurrentPlayer.Hit(Deck.DrawCard());
            CheckCurrentPlayerStatus();
            NotifyStateChanged();
        }

        public void PlayerStand(string playerName)
        {
            if (Phase != GamePhase.PlayerTurn || CurrentPlayer?.Name != playerName) return;

            CurrentPlayer.HasStood = true;
            NextPlayerTurn();
        }

        private void CheckCurrentPlayerStatus()
        {
            if (CurrentPlayer == null) return;

            if (CurrentPlayer.IsBusted || CurrentPlayer.Score == 21)
            {
                CurrentPlayer.HasStood = true;
                NextPlayerTurn();
            }
        }

        private void NextPlayerTurn()
        {
            CurrentPlayerIndex++;

            if (CurrentPlayerIndex >= Players.Count)
            {
                ExecuteDealerTurn();
            }
            else
            {
                CheckCurrentPlayerStatus();
                NotifyStateChanged();
            }
        }

        private void ExecuteDealerTurn()
        {
            Phase = GamePhase.DealerTurn;
            DealerRevealed = true;

            while (Dealer.Score < 17)
            {
                Dealer.Hit(Deck.DrawCard());
            }

            DetermineWinners();
        }

        private void DetermineWinners()
        {
            int dealerScore = Dealer.Score;
            bool dealerBusted = Dealer.IsBusted;

            foreach (var player in Players)
            {
                if (player.IsBusted) { player.HasWon = false; }
                else if (dealerBusted || player.Score > dealerScore) { player.HasWon = true; }
                else if (player.Score == dealerScore) { player.IsPush = true; }
                else { player.HasWon = false; }
            }

            Phase = GamePhase.GameOver;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnGameStateChanged?.Invoke();
    }
}
