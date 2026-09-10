using Chuds2Chads.Data;

namespace Chuds2Chads.Data.Entities;

public class UserAvatarLoadout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    public Guid? HeadObjectId { get; set; }
    public Guid? FaceObjectId { get; set; }
    public Guid? BodyObjectId { get; set; }
    public Guid? TorsoObjectId { get; set; }
    public Guid? LegsObjectId { get; set; }
    public Guid? ShoeObjectId { get; set; }
    public Guid? PetObjectId { get; set; }

    public ApplicationUser? User { get; set; }
}