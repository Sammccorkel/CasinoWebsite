namespace Chuds2Chads.Data.Entities;

public class Friendship
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid FriendUserId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
