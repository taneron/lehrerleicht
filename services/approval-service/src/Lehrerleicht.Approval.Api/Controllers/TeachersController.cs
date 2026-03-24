using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lehrerleicht.Approval.Core.DTOs;
using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeachersController : ControllerBase
{
    private readonly UserManager<Teacher> _userManager;

    public TeachersController(UserManager<Teacher> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<List<TeacherDto>>> GetAll()
    {
        var teachers = await _userManager.Users
            .Include(t => t.School)
            .Where(t => t.IsActive)
            .ToListAsync();

        return Ok(teachers.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TeacherDto>> GetById(string id)
    {
        var teacher = await _userManager.Users
            .Include(t => t.School)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher is null) return NotFound();

        return Ok(MapToDto(teacher));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TeacherDto>> Update(string id, [FromBody] UpdateTeacherDto dto)
    {
        var currentUserId = User.FindFirst("sub")?.Value;
        if (currentUserId != id) return Forbid();

        var teacher = await _userManager.Users
            .Include(t => t.School)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher is null) return NotFound();

        if (dto.FirstName is not null) teacher.FirstName = dto.FirstName;
        if (dto.LastName is not null) teacher.LastName = dto.LastName;
        if (dto.Title is not null) teacher.Title = dto.Title;
        if (dto.ProfileImageUrl is not null) teacher.ProfileImageUrl = dto.ProfileImageUrl;
        if (dto.Subjects is not null) teacher.Subjects = dto.Subjects;
        if (dto.Classes is not null) teacher.Classes = dto.Classes;
        if (dto.PreferredLanguage is not null) teacher.PreferredLanguage = dto.PreferredLanguage;
        if (dto.Timezone is not null) teacher.Timezone = dto.Timezone;
        teacher.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(teacher);

        return Ok(MapToDto(teacher));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var teacher = await _userManager.FindByIdAsync(id);
        if (teacher is null) return NotFound();

        teacher.IsActive = false;
        teacher.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(teacher);

        return NoContent();
    }

    private static TeacherDto MapToDto(Teacher t) => new(
        t.Id, t.Email!, t.FirstName, t.LastName, t.Title, t.ProfileImageUrl,
        t.SchoolId, t.School?.Name, t.Subjects, t.Classes, t.IsActive,
        t.PreferredLanguage, t.CreatedAt, t.LastLoginAt
    );
}
