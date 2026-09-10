namespace Chuds2Chads.Data.Entities;

public class GameRoomInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public Guid InviteeUserId { get; set; }
    public Guid InvitedByUserId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public GameRoom? Room { get; set; }
}
