namespace Chuds2Chads.Data.Entities;

public enum FriendRequestStatus
{
    Pending = 1,
    Accepted = 2,
    Ignored = 3
}

public class FriendRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequesterUserId { get; set; }
    public Guid RequesteeUserId { get; set; }
    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedUtc { get; set; }
}
