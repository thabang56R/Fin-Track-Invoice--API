using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinTrack.Application.DTOs;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;
using FinTrack.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;

    public AuthController(AppDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterRequest req)
    {
        var role = req.Role is "Admin" or "Finance" or "Viewer" ? req.Role : "Viewer";
        var email = req.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == email);
        if (exists) return BadRequest("Email already exists.");

        var user = new AppUser
        {
            Email = email,
            PasswordHash = PasswordHasher.Hash(req.Password),
            Role = role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Email, user.Role });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return Unauthorized("Invalid credentials.");

        if (!PasswordHasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        var token = CreateToken(user);
        return Ok(new AuthResponse(token, user.Email, user.Role));
    }

    private string CreateToken(AppUser user)
    {
        var jwt = _cfg.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
