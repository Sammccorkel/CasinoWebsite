using System.Collections.Generic;
using System.Linq;
using Chuds2Chads.Games.Blackjack;

namespace Chuds2Chads.Games.Blackjack
{
    public class BlackjackLobbyService
    {
        // Change the List type here
        private List<BlackjackTableInfo> _lobbies = new();
        private Dictionary<string, PlayBlackjack> _games = new();

        // Change the return type here
        public List<BlackjackTableInfo> GetAvailableLobbies() => _lobbies;

        public void CreateLobby(string lobbyName, string hostName)
        {
            if (!_lobbies.Any(l => l.LobbyName == lobbyName))
            {
                // Change the instantiation here
                _lobbies.Add(new BlackjackTableInfo { LobbyName = lobbyName });
                _games[lobbyName] = new PlayBlackjack(lobbyName);
            }
        }

        // ... Keep JoinLobby, GetGameInstance, and LeaveLobby exactly the same ...
        public bool JoinLobby(string lobbyName, string playerName)
        {
            var lobby = _lobbies.FirstOrDefault(l => l.LobbyName == lobbyName);
            if (lobby != null && !lobby.IsFull && !lobby.Players.Contains(playerName))
            {
                lobby.Players.Add(playerName);
                _games[lobbyName].Players.Add(new Player { Name = playerName });
                return true;
            }
            return lobby != null && lobby.Players.Contains(playerName);
        }

        public PlayBlackjack? GetGameInstance(string lobbyName)
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

                if (lobby.Players.Count == 0)
                {
                    _lobbies.Remove(lobby);
                    _games.Remove(lobbyName);
                }
            }
        }
    }
}