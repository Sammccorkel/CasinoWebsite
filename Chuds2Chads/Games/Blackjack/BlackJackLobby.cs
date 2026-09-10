using System.Collections.Generic;
using System.Linq;

namespace Chuds2Chads.Games.Blackjack
{
    public class BlackjackLobby
    {
        public string LobbyName { get; set; } = string.Empty;
        public List<string> Players { get; set; } = new();
        public int MaxPlayers { get; set; } = 4;

        public bool IsFull => Players.Count >= MaxPlayers;
    }

    public class LobbyService
    {
        private List<BlackjackLobby> _lobbies = new();

        public List<BlackjackLobby> GetAvailableLobbies()
        {
            return _lobbies.Where(l => !l.IsFull).ToList();
        }

        public BlackjackLobby CreateLobby(string lobbyName, string creator)
        {
            var lobby = new BlackjackLobby
            {
                LobbyName = lobbyName
            };
            lobby.Players.Add(creator);
            _lobbies.Add(lobby);
            return lobby;
        }

        public bool JoinLobby(string lobbyName, string playerName)
        {
            var lobby = _lobbies.FirstOrDefault(l => l.LobbyName == lobbyName);
            if (lobby != null && !lobby.IsFull)
            {
                lobby.Players.Add(playerName);
                return true;
            }
            return false;
        }
    }
}
