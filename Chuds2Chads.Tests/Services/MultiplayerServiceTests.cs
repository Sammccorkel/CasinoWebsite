using Chuds2Chads.Data;
using Chuds2Chads.Data.Entities;
using Chuds2Chads.Services;
using Chuds2Chads.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Chuds2Chads.Tests.Services;

public class MultiplayerServiceTests : IDisposable
{
    private readonly SqliteTestHarness _harness = new();

    [Fact]
    public async Task FriendRequestLifecycle_AcceptsRequestAndShowsFriendsOnDashboard()
    {
        var requester = await _harness.CreateUserAsync("requester", 1_000, "C2C-REQ1-0001");
        var requestee = await _harness.CreateUserAsync("requestee", 1_000, "C2C-REQ2-0002");
        var multiplayer = _harness.GetRequiredService<MultiplayerService>();

        var sendResult = await multiplayer.SendFriendRequestAsync(requester.Id, "C2C-REQ2-0002");
        Assert.True(sendResult.Ok);

        var requesteeDashboard = await multiplayer.GetDashboardAsync(requestee.Id);
        Assert.NotNull(requesteeDashboard);
        Assert.Single(requesteeDashboard!.PendingRequests);
        Assert.Equal("requester", requesteeDashboard.PendingRequests[0].RequesterName);

        var respondResult = await multiplayer.RespondToFriendRequestAsync(
            requestee.Id,
            requesteeDashboard.PendingRequests[0].RequestId,
            accept: true);

        Assert.True(respondResult.Ok);

        var requesterDashboard = await multiplayer.GetDashboardAsync(requester.Id);
        requesteeDashboard = await multiplayer.GetDashboardAsync(requestee.Id);

        Assert.Contains(requesterDashboard!.Friends, friend => friend.FriendUserId == requestee.Id && friend.Name == "requestee");
        Assert.Contains(requesteeDashboard!.Friends, friend => friend.FriendUserId == requester.Id && friend.Name == "requester");
        Assert.Empty(requesteeDashboard.PendingRequests);
    }

    [Fact]
    public async Task FriendsOnlyRoom_IsVisibleToFriendsAndHiddenFromNonFriends()
    {
        var host = await _harness.CreateUserAsync("host", 5_000, "C2C-HOST-0001");
        var friend = await _harness.CreateUserAsync("friend", 5_000, "C2C-FRND-0002");
        var stranger = await _harness.CreateUserAsync("stranger", 5_000, "C2C-STRN-0003");

        await _harness.AddFriendshipAsync(host.Id, friend.Id);

        var multiplayer = _harness.GetRequiredService<MultiplayerService>();

        var created = await multiplayer.CreateRoomAsync(host.Id, new CreateRoomRequest
        {
            Name = "Friends Blackjack",
            Game = GameType.Blackjack,
            Visibility = RoomVisibility.FriendsOnly,
            MinBet = 100
        });

        Assert.True(created.Ok);
        Assert.NotNull(created.RoomId);

        var friendDashboard = await multiplayer.GetDashboardAsync(friend.Id);
        var strangerDashboard = await multiplayer.GetDashboardAsync(stranger.Id);

        Assert.Contains(friendDashboard!.JoinableRooms, room => room.RoomId == created.RoomId);
        Assert.DoesNotContain(strangerDashboard!.JoinableRooms, room => room.RoomId == created.RoomId);

        var friendJoin = await multiplayer.JoinRoomAsync(friend.Id, created.RoomId!.Value);
        var strangerJoin = await multiplayer.JoinRoomAsync(stranger.Id, created.RoomId.Value);

        Assert.True(friendJoin.Ok);
        Assert.False(strangerJoin.Ok);
    }

    [Fact]
    public async Task RequestRoomDeletionAsync_ClosesLobbyRoomCreatedByHost()
    {
        var host = await _harness.CreateUserAsync("deleter", 2_000, "C2C-DEL1-0001");
        var multiplayer = _harness.GetRequiredService<MultiplayerService>();

        var created = await multiplayer.CreateRoomAsync(host.Id, new CreateRoomRequest
        {
            Name = "Delete Me",
            Game = GameType.Poker,
            Visibility = RoomVisibility.Private,
            MinBet = 100
        });

        Assert.True(created.Ok);
        Assert.NotNull(created.RoomId);

        var deletion = await multiplayer.RequestRoomDeletionAsync(host.Id, created.RoomId!.Value);
        Assert.True(deletion.Ok);

        await using var scope = _harness.ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var room = await db.GameRooms.FirstAsync(r => r.Id == created.RoomId.Value);
        var roomPlayers = await db.RoomPlayers.Where(rp => rp.RoomId == created.RoomId.Value).ToListAsync();
        var updatedHost = await db.Users.FirstAsync(u => u.Id == host.Id);

        Assert.Equal(RoomStatus.Closed, room.Status);
        Assert.True(room.CloseRequested);
        Assert.Empty(roomPlayers);
        Assert.Null(updatedHost.ActiveRoomId);
    }

    public void Dispose()
    {
        _harness.Dispose();
    }
}
