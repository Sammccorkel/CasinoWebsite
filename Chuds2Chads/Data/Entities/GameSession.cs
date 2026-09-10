namespace Chuds2Chads.Data.Entities;

public class GameSession
{
    public Guid Id {get; set; } = Guid.NewGuid();
    public Guid RoomId {get; set; }
    public GameType Game {get; set; }
    public string StateJson {get; set; } = "{}";
    public DateTime StartedUtc {get; set; } = DateTime.UtcNow;
    public DateTime EndedUtc {get; set; }
    public long NetCoins {get; set; } 
}