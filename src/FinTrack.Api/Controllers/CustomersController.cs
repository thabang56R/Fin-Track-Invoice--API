using FinTrack.Application.DTOs;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Admin,Finance,Viewer")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomersController(AppDbContext db) => _db = db;

    [HttpPost]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ActionResult<Customer>> Create(CreateCustomerRequest req)
    {
        var c = new Customer
        {
            Name = req.Name,
            Email = req.Email,
            Phone = req.Phone,
            Address = req.Address
        };

        _db.Customers.Add(c);
        await _db.SaveChangesAsync();
        return Ok(c);
    }

    [HttpGet]
    public async Task<ActionResult> List()
    {
        var items = await _db.Customers
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        return Ok(items);
    }
}
