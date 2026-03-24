using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Lehrerleicht.Approval.Core.DTOs;

namespace Lehrerleicht.Approval.Api.Hubs;

[Authorize]
public class ApprovalHub : Hub
{
    private readonly ILogger<ApprovalHub> _logger;

    public ApprovalHub(ILogger<ApprovalHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var teacherId = Context.User?.FindFirst("sub")?.Value;
        if (teacherId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"teacher:{teacherId}");
            _logger.LogInformation("Teacher {TeacherId} connected to ApprovalHub", teacherId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var teacherId = Context.User?.FindFirst("sub")?.Value;
        if (teacherId is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"teacher:{teacherId}");
            _logger.LogInformation("Teacher {TeacherId} disconnected from ApprovalHub", teacherId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToSchool(string schoolId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"school:{schoolId}");
    }
}
