using Chuds2Chads.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace Chuds2Chads.Data;

public enum UserPresenceStatus
{
    Offline = 0,
    Online = 1,
    InLobby = 2,
    AtTable = 3
}

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string FriendCode { get; set; } = string.Empty;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    public UserPresenceStatus PresenceStatus { get; set; } = UserPresenceStatus.Offline;
    public Guid? ActiveRoomId { get; set; }

    public UserAvatarLoadout? AvatarLoadout { get; set; }
    public ICollection<UserCosmeticItem> OwnedCosmeticItems { get; set; } = new List<UserCosmeticItem>();
}
