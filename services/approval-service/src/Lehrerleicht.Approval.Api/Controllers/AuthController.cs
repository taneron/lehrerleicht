using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Lehrerleicht.Approval.Core.DTOs;
using Lehrerleicht.Approval.Core.Entities;

namespace Lehrerleicht.Approval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<Teacher> _userManager;
    private readonly SignInManager<Teacher> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<Teacher> userManager,
        SignInManager<Teacher> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<TokenDto>> Register([FromBody] RegisterTeacherDto dto)
    {
        var teacher = new Teacher
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Title = dto.Title,
            SchoolId = dto.SchoolId
        };

        var result = await _userManager.CreateAsync(teacher, dto.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        return Ok(GenerateTokens(teacher));
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenDto>> Login([FromBody] LoginDto dto)
    {
        var teacher = await _userManager.FindByEmailAsync(dto.Email);
        if (teacher is null)
            return Unauthorized(new { error = "Invalid credentials" });

        var result = await _signInManager.CheckPasswordSignInAsync(teacher, dto.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized(new { error = "Invalid credentials" });

        teacher.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(teacher);

        return Ok(GenerateTokens(teacher));
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult<TokenDto>> Refresh([FromBody] RefreshTokenDto dto)
    {
        // For simplicity, we re-issue tokens based on the current authenticated user.
        // A production system would validate the refresh token against a store.
        var teacherId = User.FindFirst("sub")?.Value;
        if (teacherId is null) return Unauthorized();

        var teacher = await _userManager.FindByIdAsync(teacherId);
        if (teacher is null) return Unauthorized();

        return Ok(GenerateTokens(teacher));
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // In a JWT-based system, logout is primarily client-side.
        // A production system would blacklist the token.
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<TeacherDto>> Me()
    {
        var teacherId = User.FindFirst("sub")?.Value;
        if (teacherId is null) return Unauthorized();

        var teacher = await _userManager.FindByIdAsync(teacherId);
        if (teacher is null) return NotFound();

        return Ok(new TeacherDto(
            teacher.Id,
            teacher.Email!,
            teacher.FirstName,
            teacher.LastName,
            teacher.Title,
            teacher.ProfileImageUrl,
            teacher.SchoolId,
            null,
            teacher.Subjects,
            teacher.Classes,
            teacher.IsActive,
            teacher.PreferredLanguage,
            teacher.CreatedAt,
            teacher.LastLoginAt
        ));
    }

    private TokenDto GenerateTokens(Teacher teacher)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddHours(
            double.Parse(_configuration["Jwt:ExpiryHours"] ?? "24"));

        var claims = new[]
        {
            new Claim("sub", teacher.Id),
            new Claim("email", teacher.Email!),
            new Claim("firstName", teacher.FirstName),
            new Claim("lastName", teacher.LastName),
            new Claim("schoolId", teacher.SchoolId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Simple refresh token — in production, use a secure random token stored in DB
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        return new TokenDto(accessToken, refreshToken, expires);
    }
}
