namespace Chuds2Chads.Data.Entities;

public class RoomPlayer
{
    public Guid Id {get; set; } = Guid.NewGuid();
    public Guid RoomId {get; set; }
    public Guid UserId {get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Seat {get; set; }
    public bool IsReady {get; set; }
    public bool IsHost { get; set; }
    public bool IsConnected { get; set; } = true;
    public long Stack { get; set; }
    public long InitialStack { get; set; }
    public DateTime JoinedUtc {get; set; } = DateTime.UtcNow;
    public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DisconnectedUntilUtc { get; set; }
    public GameRoom? Room {get; set; }
}
