using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lehrerleicht.Approval.Core.DTOs;
using ApprovalSvc = Lehrerleicht.Approval.Core.Services.ApprovalService;

namespace Lehrerleicht.Approval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly ApprovalSvc _approvalService;

    public ApprovalsController(ApprovalSvc approvalService)
    {
        _approvalService = approvalService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ApprovalDto>>> GetPendingApprovals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null)
    {
        var teacherId = User.FindFirst("sub")!.Value;
        var result = await _approvalService.GetApprovalsAsync(teacherId, page, pageSize, status, priority);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApprovalDetailDto>> GetApproval(Guid id)
    {
        var approval = await _approvalService.GetApprovalByIdAsync(id);
        if (approval is null) return NotFound();
        return Ok(approval);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApprovalResultDto>> Approve(
        Guid id, [FromBody] ApprovalDecisionDto decision)
    {
        var teacherId = User.FindFirst("sub")!.Value;
        var result = await _approvalService.ApproveAsync(id, teacherId, decision);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApprovalResultDto>> Reject(
        Guid id, [FromBody] RejectionDto rejection)
    {
        var teacherId = User.FindFirst("sub")!.Value;
        var result = await _approvalService.RejectAsync(id, teacherId, rejection);
        return Ok(result);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _approvalService.MarkAsReadAsync(id);
        return NoContent();
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApprovalStatsDto>> GetStats()
    {
        var teacherId = User.FindFirst("sub")!.Value;
        var stats = await _approvalService.GetStatsAsync(teacherId);
        return Ok(stats);
    }
}
