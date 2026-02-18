using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Finance,Viewer")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue([FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var fromUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtcExclusive = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var payments = await _db.Payments
            .Where(p => p.CapturedAtUtc >= fromUtc && p.CapturedAtUtc < toUtcExclusive)
            .Select(p => new { p.Amount })
            .ToListAsync();

        var totalIn = payments.Where(x => x.Amount > 0).Sum(x => x.Amount);
        var totalOut = payments.Where(x => x.Amount < 0).Sum(x => x.Amount); 
        var net = totalIn + totalOut;

        return Ok(new
        {
            Range = new { From = fromDate, To = toDate },
            TotalPayments = totalIn,
            TotalRefundsAndReversals = totalOut, 
            NetRevenue = net
        });
    }

    
    [HttpGet("outstanding")]
    public async Task<IActionResult> Outstanding()
    {
        var list = await _db.Invoices
            .Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.Total,
                Paid = i.Payments.Sum(p => p.Amount),
                CustomerName = i.Customer != null ? i.Customer.Name : null,
                i.IssueDate,
                i.DueDate,
                i.Status
            })
            .ToListAsync();

        var rows = list.Select(x => new
        {
            x.Id,
            x.InvoiceNumber,
            x.CustomerName,
            x.Status,
            x.Total,
            x.Paid,
            Outstanding = x.Total - x.Paid,
            x.IssueDate,
            x.DueDate
        })
        .Where(x => x.Outstanding > 0)
        .OrderByDescending(x => x.Outstanding)
        .ToList();

        return Ok(new
        {
            Count = rows.Count,
            TotalOutstanding = rows.Sum(x => x.Outstanding),
            Top10 = rows.Take(10),
            Rows = rows // you can remove this later if too big
        });
    }

    
    [HttpGet("vat")]
    public async Task<IActionResult> Vat([FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);

        
        var items = await _db.InvoiceItems
            .Where(ii => ii.Invoice != null
                         && ii.Invoice.Status != InvoiceStatus.Cancelled
                         && ii.Invoice.IssueDate >= fromDate
                         && ii.Invoice.IssueDate <= toDate)
            .Select(ii => new
            {
                ii.VatRate,
                ii.VatAmount,
                ii.LineTotal
            })
            .ToListAsync();

        var byRate = items
            .GroupBy(x => x.VatRate)
            .Select(g => new
            {
                VatRate = g.Key,
                VatAmount = g.Sum(x => x.VatAmount),
                TaxableBase = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.VatRate)
            .ToList();

        return Ok(new
        {
            Range = new { From = fromDate, To = toDate },
            TotalVat = byRate.Sum(x => x.VatAmount),
            Breakdown = byRate
        });
    }

    
[HttpGet("aging")]
public async Task<IActionResult> Aging([FromQuery] DateOnly? asOf = null)
{
    var asOfDate = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

    
    var invoices = await _db.Invoices
        .Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
        .Select(i => new
        {
            i.Id,
            i.InvoiceNumber,
            i.Total,
            i.DueDate,
            CustomerName = i.Customer != null ? i.Customer.Name : null,
            Paid = i.Payments.Sum(p => p.Amount),
            i.Status
        })
        .ToListAsync();

    var rows = invoices
        .Select(x =>
        {
            var outstanding = x.Total - x.Paid;
            var daysOverdue = (asOfDate.ToDateTime(TimeOnly.MinValue) - x.DueDate.ToDateTime(TimeOnly.MinValue)).Days;

            var overdueDays = Math.Max(0, daysOverdue);

            return new
            {
                x.Id,
                x.InvoiceNumber,
                x.CustomerName,
                x.Status,
                x.Total,
                x.Paid,
                Outstanding = outstanding,
                x.DueDate,
                OverdueDays = overdueDays
            };
        })
        .Where(x => x.Outstanding > 0) 
        .ToList();

    // Buckets
    var bucket0_30 = rows.Where(r => r.OverdueDays >= 0 && r.OverdueDays <= 30).ToList();
    var bucket31_60 = rows.Where(r => r.OverdueDays >= 31 && r.OverdueDays <= 60).ToList();
    var bucket61_90 = rows.Where(r => r.OverdueDays >= 61 && r.OverdueDays <= 90).ToList();
    var bucket90Plus = rows.Where(r => r.OverdueDays >= 91).ToList();

    object Bucket(string name, List<dynamic> list) => new
    {
        Name = name,
        Count = list.Count,
        TotalOutstanding = list.Sum(x => (decimal)x.Outstanding)
    };

    var top10 = rows
        .OrderByDescending(r => r.OverdueDays)
        .ThenByDescending(r => r.Outstanding)
        .Take(10)
        .Select(r => new
        {
            r.Id,
            r.InvoiceNumber,
            r.CustomerName,
            r.DueDate,
            r.OverdueDays,
            r.Outstanding
        })
        .ToList();

    return Ok(new
    {
        AsOf = asOfDate,
        Summary = new
        {
            TotalInvoicesOutstanding = rows.Count,
            TotalOutstanding = rows.Sum(r => r.Outstanding)
        },
        Buckets = new object[]
        {
            Bucket("0-30 days overdue", bucket0_30.Cast<dynamic>().ToList()),
            Bucket("31-60 days overdue", bucket31_60.Cast<dynamic>().ToList()),
            Bucket("61-90 days overdue", bucket61_90.Cast<dynamic>().ToList()),
            Bucket("90+ days overdue", bucket90Plus.Cast<dynamic>().ToList())
        },
        Top10MostOverdue = top10
    });
}

}
