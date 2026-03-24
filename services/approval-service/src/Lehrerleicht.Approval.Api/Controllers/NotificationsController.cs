using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lehrerleicht.Approval.Core.DTOs;
using Lehrerleicht.Approval.Core.Services;

namespace Lehrerleicht.Approval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationsController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferenceDto>> GetPreferences()
    {
        var teacherId = User.FindFirst("sub")!.Value;
        var prefs = await _notificationService.GetPreferencesAsync(teacherId);
        if (prefs is null) return NotFound();
        return Ok(prefs);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<NotificationPreferenceDto>> UpdatePreferences(
        [FromBody] UpdateNotificationPreferenceDto dto)
    {
        var teacherId = User.FindFirst("sub")!.Value;
        var result = await _notificationService.UpdatePreferencesAsync(teacherId, dto);
        return Ok(result);
    }

    [HttpPost("register-device")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceDto dto)
    {
        var teacherId = User.FindFirst("sub")!.Value;
        await _notificationService.RegisterDeviceAsync(teacherId, dto);
        return NoContent();
    }

    [HttpDelete("unregister-device")]
    public async Task<IActionResult> UnregisterDevice()
    {
        var teacherId = User.FindFirst("sub")!.Value;
        await _notificationService.UnregisterDeviceAsync(teacherId);
        return NoContent();
    }
}
