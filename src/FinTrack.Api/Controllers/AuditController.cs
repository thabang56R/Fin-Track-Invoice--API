using FinTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _db;
    public AuditController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult> Latest()
    {
        var logs = await _db.AuditLogs
            .OrderByDescending(a => a.PerformedAtUtc)
            .Take(200)
            .ToListAsync();

        return Ok(logs);
    }
}
