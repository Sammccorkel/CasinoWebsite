using System.Collections.Generic;
using System.Linq;

namespace Chuds2Chads.Games.Poker
{
    public class PokerLobbyService
    {
        private List<PokerTableInfo> _lobbies = new();
        private Dictionary<string, TexasHoldemGame> _games = new();

        public List<PokerTableInfo> GetAvailableLobbies() => _lobbies;

        public void CreateLobby(string lobbyName, string hostName)
        {
            if (!_lobbies.Any(l => l.LobbyName == lobbyName))
            {
                var newLobby = new PokerTableInfo { LobbyName = lobbyName };
                _lobbies.Add(newLobby);
                
                var game = new TexasHoldemGame(lobbyName);
                _games[lobbyName] = game;

                // Automatically add the designated Bot when a lobby is created
                game.Players.Add(new PokerPlayer { Name = "ChadBot (AI)", IsBot = true });
                newLobby.Players.Add("ChadBot (AI)");
            }
        }

        public bool JoinLobby(string lobbyName, string playerName)
        {
            var lobby = _lobbies.FirstOrDefault(l => l.LobbyName == lobbyName);
            var game = _games.GetValueOrDefault(lobbyName);

            if (lobby != null && game != null && !lobby.IsFull && !lobby.Players.Contains(playerName))
            {
                // Kick the bot when Player 3 joins (meaning 2 real players are already here)
                int realPlayerCount = lobby.Players.Count(p => !p.Contains("ChadBot"));
                if (realPlayerCount == 2 && lobby.Players.Contains("ChadBot (AI)"))
                {
                    lobby.Players.Remove("ChadBot (AI)");
                    
                    // Find the bot and safely remove it from the table
                    var bot = game.Players.FirstOrDefault(p => p.IsBot);
                    if (bot != null)
                    {
                        bot.HasFolded = true; // Fold them out of the active hand just in case
                        game.Players.Remove(bot);
                    }
                }

                lobby.Players.Add(playerName);
                
                var newPlayer = new PokerPlayer { Name = playerName };
                
                if (game.Phase != PokerPhase.Waiting && game.Phase != PokerPhase.GameOver)
                {
                    newPlayer.HasFolded = true; 
                }
                
                game.Players.Add(newPlayer);
                
                
                if (game.Phase == PokerPhase.Waiting && game.Players.Count >= 2)
                {
                    game.StartNewRound();
                }

                return true;
            }
            return lobby != null && lobby.Players.Contains(playerName);
        }

        public TexasHoldemGame? GetGameInstance(string lobbyName)
        {
            _games.TryGetValue(lobbyName, out var game);
            return game;
        }

        public void LeaveLobby(string lobbyName, string playerName)
        {
            var lobby = _lobbies.FirstOrDefault(l => l.LobbyName == lobbyName);
            if (lobby != null)
            {
                lobby.Players.Remove(playerName);
                _games[lobbyName].Players.RemoveAll(p => p.Name == playerName);

                if (lobby.Players.Count(p => !p.Contains("ChadBot")) == 0)
                {
                    _lobbies.Remove(lobby);
                    _games.Remove(lobbyName);
                }
            }
        }
    }
}