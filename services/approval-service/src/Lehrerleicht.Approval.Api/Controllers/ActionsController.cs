using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lehrerleicht.Approval.Core.DTOs;
using Lehrerleicht.Approval.Core.Services;

namespace Lehrerleicht.Approval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActionsController : ControllerBase
{
    private readonly ActionHistoryService _historyService;

    public ActionsController(ActionHistoryService historyService)
    {
        _historyService = historyService;
    }

    [HttpGet("history")]
    public async Task<ActionResult<PagedResult<ActionHistoryDto>>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var teacherId = User.FindFirst("sub")!.Value;
        var result = await _historyService.GetHistoryAsync(teacherId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("history/{id:guid}")]
    public async Task<ActionResult<ActionHistoryDto>> GetById(Guid id)
    {
        var history = await _historyService.GetByIdAsync(id);
        if (history is null) return NotFound();
        return Ok(history);
    }

    [HttpGet("by-student/{studentId}")]
    public async Task<ActionResult<List<ActionHistoryDto>>> GetByStudent(string studentId)
    {
        var result = await _historyService.GetByStudentIdAsync(studentId);
        return Ok(result);
    }
}
