using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RunClub.Application.Abstractions;
using RunClub.Domain;
using RunClub.Domain.Entities;
using RunClub.Infrastructure.Identity;
using RunClub.Infrastructure.Persistence;

namespace RunClub.Api.Hubs;

[Authorize]
public class ClubChatHub : Hub
{
    private readonly AppDbContext _db;
    private readonly IClubAuthorizationService _auth;

    public ClubChatHub(AppDbContext db, IClubAuthorizationService auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task JoinClub(Guid clubId)
    {
        var userId = Context.UserIdentifier ?? throw new HubException("Unauthorized");
        await _auth.EnsureClubMemberAsync(userId, clubId);
        await Groups.AddToGroupAsync(Context.ConnectionId, ClubGroup(clubId));
    }

    public async Task SendMessage(Guid clubId, string body)
    {
        var userId = Context.UserIdentifier ?? throw new HubException("Unauthorized");
        await _auth.EnsureClubMemberAsync(userId, clubId);
        if (string.IsNullOrWhiteSpace(body)) throw new HubException("Empty message");

        var msg = new ChatMessage
        {
            ClubId = clubId,
            SenderUserId = userId,
            Body = body.Trim()
        };
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();

        var profile = await _db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        await Clients.Group(ClubGroup(clubId)).SendAsync("messageReceived", new
        {
            msg.Id,
            msg.ClubId,
            msg.SenderUserId,
            SenderName = profile is null ? "Member" : $"{profile.FirstName} {profile.LastName}",
            msg.Body,
            msg.CreatedAtUtc,
            msg.IsDeleted,
            msg.IsFlagged
        });
    }

    public async Task ModerateMessage(Guid clubId, Guid messageId, bool delete, bool flag)
    {
        var userId = Context.UserIdentifier ?? throw new HubException("Unauthorized");
        await _auth.EnsureClubAdminAsync(userId, clubId);
        var msg = await _db.ChatMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.ClubId == clubId)
            ?? throw new HubException("Not found");
        msg.IsDeleted = delete || msg.IsDeleted;
        msg.IsFlagged = flag;
        await _db.SaveChangesAsync();
        await Clients.Group(ClubGroup(clubId)).SendAsync("messageModerated", new { msg.Id, msg.IsDeleted, msg.IsFlagged });
    }

    private static string ClubGroup(Guid clubId) => $"club-{clubId}";
}
