using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lehrerleicht.Approval.Core.DTOs;
using Lehrerleicht.Approval.Core.Entities;
using Lehrerleicht.Approval.Core.Interfaces;

namespace Lehrerleicht.Approval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SchoolsController : ControllerBase
{
    private readonly ISchoolRepository _schoolRepo;

    public SchoolsController(ISchoolRepository schoolRepo)
    {
        _schoolRepo = schoolRepo;
    }

    [HttpGet]
    public async Task<ActionResult<List<SchoolDto>>> GetAll()
    {
        var schools = await _schoolRepo.GetAllAsync();
        return Ok(schools.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SchoolDto>> GetById(Guid id)
    {
        var school = await _schoolRepo.GetByIdAsync(id);
        if (school is null) return NotFound();
        return Ok(MapToDto(school));
    }

    [HttpPost]
    public async Task<ActionResult<SchoolDto>> Create([FromBody] CreateSchoolDto dto)
    {
        var school = new School
        {
            Name = dto.Name,
            ShortName = dto.ShortName,
            SchoolCode = dto.SchoolCode,
            Type = dto.Type,
            Street = dto.Street,
            PostalCode = dto.PostalCode,
            City = dto.City,
            State = dto.State,
            Phone = dto.Phone,
            Email = dto.Email,
            Website = dto.Website
        };

        var created = await _schoolRepo.CreateAsync(school);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SchoolDto>> Update(Guid id, [FromBody] UpdateSchoolDto dto)
    {
        var school = await _schoolRepo.GetByIdAsync(id);
        if (school is null) return NotFound();

        if (dto.Name is not null) school.Name = dto.Name;
        if (dto.ShortName is not null) school.ShortName = dto.ShortName;
        if (dto.Phone is not null) school.Phone = dto.Phone;
        if (dto.Email is not null) school.Email = dto.Email;
        if (dto.Website is not null) school.Website = dto.Website;
        if (dto.SchoolFoxSchoolId is not null) school.SchoolFoxSchoolId = dto.SchoolFoxSchoolId;
        if (dto.WebUntisSchoolName is not null) school.WebUntisSchoolName = dto.WebUntisSchoolName;
        if (dto.WebUntisServer is not null) school.WebUntisServer = dto.WebUntisServer;
        if (dto.DefaultApprovalExpiryHours.HasValue) school.DefaultApprovalExpiryHours = dto.DefaultApprovalExpiryHours.Value;
        school.UpdatedAt = DateTime.UtcNow;

        await _schoolRepo.UpdateAsync(school);
        return Ok(MapToDto(school));
    }

    private static SchoolDto MapToDto(School s) => new(
        s.Id, s.Name, s.ShortName, s.SchoolCode, s.Type,
        s.Street, s.PostalCode, s.City, s.State,
        s.Phone, s.Email, s.Website,
        s.SubscriptionTier, s.IsActive,
        s.Teachers?.Count ?? 0,
        s.CreatedAt
    );
}
