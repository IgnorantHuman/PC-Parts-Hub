using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using PCPartsHub.Models;

namespace PCShop.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatHub(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task SendMessage(string message)
    {
        message = message?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new HubException(
                "Message cannot be empty.");
        }

        if (message.Length > 500)
        {
            throw new HubException(
                "Message cannot exceed 500 characters.");
        }

        ApplicationUser? user = null;

        if (Context.User != null)
        {
            user = await _userManager.GetUserAsync(
                Context.User);
        }

        string senderName =
            user?.FullName
            ?? Context.User?.Identity?.Name
            ?? "PCShop User";

        string senderRole = GetUserRole();

        string sentTime =
            DateTime.UtcNow
                .AddHours(8)
                .ToString("dd MMM yyyy, hh:mm tt");

        await Clients.All.SendAsync(
            "ReceiveMessage",
            senderName,
            senderRole,
            message,
            sentTime);
    }

    public override async Task OnConnectedAsync()
    {
        ApplicationUser? user = null;

        if (Context.User != null)
        {
            user = await _userManager.GetUserAsync(
                Context.User);
        }

        string senderName =
            user?.FullName
            ?? Context.User?.Identity?.Name
            ?? "A user";

        await Clients.Others.SendAsync(
            "ReceiveSystemMessage",
            $"{senderName} joined the live chat.");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(
        Exception? exception)
    {
        ApplicationUser? user = null;

        if (Context.User != null)
        {
            user = await _userManager.GetUserAsync(
                Context.User);
        }

        string senderName =
            user?.FullName
            ?? Context.User?.Identity?.Name
            ?? "A user";

        await Clients.Others.SendAsync(
            "ReceiveSystemMessage",
            $"{senderName} left the live chat.");

        await base.OnDisconnectedAsync(exception);
    }

    private string GetUserRole()
    {
        ClaimsPrincipal? user = Context.User;

        if (user?.IsInRole("Admin") == true)
        {
            return "Admin";
        }

        if (user?.IsInRole("Seller") == true)
        {
            return "Seller";
        }

        if (user?.IsInRole("Buyer") == true)
        {
            return "Buyer";
        }

        return "User";
    }
}