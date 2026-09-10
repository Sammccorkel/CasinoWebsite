using Chuds2Chads.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Chuds2Chads.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<GameRoom> GameRooms => Set<GameRoom>();
    public DbSet<RoomPlayer> RoomPlayers => Set<RoomPlayer>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<GameRoomInvite> GameRoomInvites => Set<GameRoomInvite>();
    public DbSet<CosmeticDefinition> CosmeticDefinitions => Set<CosmeticDefinition>();
    public DbSet<UserCosmeticItem> UserCosmeticItems => Set<UserCosmeticItem>();
    public DbSet<UserAvatarLoadout> UserAvatarLoadouts => Set<UserAvatarLoadout>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GameRoom>()
            .HasIndex(r => r.Code)
            .IsUnique();

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.FriendCode)
            .IsUnique();

        modelBuilder.Entity<Wallet>()
            .HasIndex(w => w.UserId)
            .IsUnique();

        modelBuilder.Entity<Friendship>()
            .HasIndex(f => new { f.UserId, f.FriendUserId })
            .IsUnique();

        modelBuilder.Entity<FriendRequest>()
            .HasIndex(f => new { f.RequesterUserId, f.RequesteeUserId, f.Status });

        modelBuilder.Entity<GameRoomInvite>()
            .HasIndex(i => new { i.RoomId, i.InviteeUserId })
            .IsUnique();

        modelBuilder.Entity<GameRoom>()
            .HasMany(r => r.Players)
            .WithOne(p => p.Room!)
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RoomPlayer>()
            .HasIndex(p => new { p.RoomId, p.UserId })
            .IsUnique();

        modelBuilder.Entity<CosmeticDefinition>()
            .HasIndex(c => c.AssetKey)
            .IsUnique();

        modelBuilder.Entity<UserCosmeticItem>()
            .HasKey(i => i.ObjectId);

        modelBuilder.Entity<UserCosmeticItem>()
            .HasOne(i => i.User)
            .WithMany(u => u.OwnedCosmeticItems)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserCosmeticItem>()
            .HasOne(i => i.CosmeticDefinition)
            .WithMany(c => c.OwnedItems)
            .HasForeignKey(i => i.CosmeticDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserAvatarLoadout>()
            .HasIndex(a => a.UserId)
            .IsUnique();

        modelBuilder.Entity<UserAvatarLoadout>()
            .HasOne(a => a.User)
            .WithOne(u => u.AvatarLoadout)
            .HasForeignKey<UserAvatarLoadout>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
