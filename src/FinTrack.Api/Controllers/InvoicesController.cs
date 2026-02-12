using System.Security.Claims;
using FinTrack.Application.DTOs;
using FinTrack.Application.Services;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = "Admin,Finance,Viewer")]
public class InvoicesController : ControllerBase
{
    private readonly AppDbContext _db;
    public InvoicesController(AppDbContext db) => _db = db;

    private static InvoiceStatus ComputeDisplayStatus(InvoiceStatus baseStatus, decimal total, decimal paid)
    {
        if (baseStatus == InvoiceStatus.Cancelled) return InvoiceStatus.Cancelled;
        if (baseStatus == InvoiceStatus.Draft) return InvoiceStatus.Draft;

        var outstanding = total - paid;
        if (outstanding <= 0) return InvoiceStatus.Paid;
        if (paid > 0) return InvoiceStatus.PartiallyPaid;

        return baseStatus; // Issued (or other)
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ActionResult> Create(CreateInvoiceRequest req)
    {
        var customer = await _db.Customers.FindAsync(req.CustomerId);
        if (customer is null) return BadRequest("Customer not found.");

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var lines = req.Items.Select(i => (i.Qty, i.UnitPrice, i.VatRate));
        var (subtotal, vatTotal, total) = InvoiceCalculator.Calculate(lines);

        var invoice = new Invoice
        {
            CustomerId = req.CustomerId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            IssueDate = req.IssueDate,
            DueDate = req.DueDate,
            Status = InvoiceStatus.Draft,
            Subtotal = subtotal,
            VatTotal = vatTotal,
            Total = total,
            CreatedByUserId = userId,
            Items = req.Items.Select(i =>
            {
                var lineTotal = i.Qty * i.UnitPrice;
                var vatAmount = lineTotal * i.VatRate;
                return new InvoiceItem
                {
                    Description = i.Description,
                    Qty = i.Qty,
                    UnitPrice = i.UnitPrice,
                    VatRate = i.VatRate,
                    LineTotal = lineTotal,
                    VatAmount = vatAmount
                };
            }).ToList()
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Status,
            invoice.Total,
            invoice.IssueDate,
            invoice.DueDate
        });
    }

    [HttpPost("{id:guid}/issue")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ActionResult> Issue(Guid id)
    {
        var inv = await _db.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inv is null) return NotFound();
        if (inv.Status != InvoiceStatus.Draft) return BadRequest("Only Draft invoices can be issued.");

        inv.Status = InvoiceStatus.Issued;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("This invoice was updated by another request. Refresh and try again.");
        }

        var paid = inv.Payments.Sum(p => p.Amount); // includes refunds (negative)
        var outstanding = inv.Total - paid;

        return Ok(new
        {
            inv.Id,
            inv.InvoiceNumber,
            inv.Status,
            inv.Total,
            Paid = paid,
            Outstanding = outstanding
        });
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ActionResult> Cancel(Guid id)
    {
        var inv = await _db.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inv is null) return NotFound();

        var paid = inv.Payments.Sum(p => p.Amount);
        if (paid > 0) return BadRequest("Cannot cancel an invoice that has payments.");

        inv.Status = InvoiceStatus.Cancelled;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("This invoice was updated by another request. Refresh and try again.");
        }

        return Ok(new
        {
            inv.Id,
            inv.InvoiceNumber,
            inv.Status
        });
    }

    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ActionResult> ApplyPayment(Guid id, ApplyPaymentRequest req)
    {
        if (req.Amount <= 0) return BadRequest("Amount must be > 0.");

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var inv = await _db.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inv is null) return NotFound();
        if (inv.Status == InvoiceStatus.Cancelled) return BadRequest("Cannot pay a cancelled invoice.");
        if (inv.Status == InvoiceStatus.Draft) return BadRequest("Issue the invoice before taking payments.");

        var alreadyPaid = inv.Payments.Sum(p => p.Amount);
        var outstanding = inv.Total - alreadyPaid;

        if (req.Amount > outstanding) return BadRequest("Payment exceeds outstanding amount.");

        if (!string.IsNullOrWhiteSpace(req.Reference) &&
            inv.Payments.Any(p => p.Reference == req.Reference))
        {
            return BadRequest("Duplicate payment reference for this invoice.");
        }

        var payment = new Payment
        {
            InvoiceId = inv.Id,
            Amount = req.Amount,
            Method = req.Method,
            Reference = req.Reference,
            CapturedByUserId = userId,
            CapturedAtUtc = DateTime.UtcNow,
            PaidAtUtc = DateTime.UtcNow
        };

        // ✅ only insert Payment (no invoice update)
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        var newPaid = alreadyPaid + req.Amount;
        var newOutstanding = inv.Total - newPaid;

        var displayStatus = ComputeDisplayStatus(inv.Status, inv.Total, newPaid);

        return Ok(new
        {
            inv.Id,
            inv.InvoiceNumber,
            Status = displayStatus,
            inv.Total,
            Paid = newPaid,
            Outstanding = newOutstanding,
            PaymentId = payment.Id
        });
    }

    [HttpPost("{invoiceId:guid}/payments/{paymentId:guid}/reverse")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ActionResult> ReversePayment(Guid invoiceId, Guid paymentId, ReversePaymentRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var inv = await _db.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (inv is null) return NotFound("Invoice not found.");
        if (inv.Status == InvoiceStatus.Cancelled) return BadRequest("Cannot reverse payments on a cancelled invoice.");
        if (inv.Status == InvoiceStatus.Draft) return BadRequest("Issue the invoice before reversing payments.");

        var original = inv.Payments.FirstOrDefault(p => p.Id == paymentId);
        if (original is null) return NotFound("Payment not found on this invoice.");

        if (original.Amount <= 0) return BadRequest("You can only reverse a positive payment.");

        // prevent reversing the same payment twice
        if (inv.Payments.Any(p => p.ReversedPaymentId == original.Id))
            return BadRequest("This payment was already reversed.");

        var paidSoFar = inv.Payments.Sum(p => p.Amount);
        if (paidSoFar <= 0) return BadRequest("Nothing has been paid to reverse.");

        var reversal = new Payment
        {
            InvoiceId = inv.Id,
            Amount = -original.Amount, // full reversal
            Method = req.Method,
            Reference = req.Reference,
            Reason = req.Reason,
            ReversedPaymentId = original.Id,
            CapturedByUserId = userId,
            CapturedAtUtc = DateTime.UtcNow,
            PaidAtUtc = DateTime.UtcNow
        };

        _db.Payments.Add(reversal);
        await _db.SaveChangesAsync();

        // recompute after reversal
        var newPaid = paidSoFar + reversal.Amount; // reversal.Amount is negative
        var outstanding = inv.Total - newPaid;
        var displayStatus = ComputeDisplayStatus(inv.Status, inv.Total, newPaid);

        return Ok(new
        {
            InvoiceId = inv.Id,
            OriginalPaymentId = original.Id,
            ReversalPaymentId = reversal.Id,
            Status = displayStatus,
            inv.Total,
            Paid = newPaid,
            Outstanding = outstanding
        });
    }

    [HttpPost("{invoiceId:guid}/refunds")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ActionResult> Refund(Guid invoiceId, RefundRequest req)
    {
        if (req.Amount <= 0) return BadRequest("Refund amount must be > 0.");

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var inv = await _db.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (inv is null) return NotFound("Invoice not found.");
        if (inv.Status == InvoiceStatus.Cancelled) return BadRequest("Cannot refund a cancelled invoice.");
        if (inv.Status == InvoiceStatus.Draft) return BadRequest("Issue the invoice before refunding.");

        var paid = inv.Payments.Sum(p => p.Amount);
        if (paid <= 0) return BadRequest("No paid amount to refund.");
        if (req.Amount > paid) return BadRequest("Refund exceeds paid amount.");

        var refund = new Payment
        {
            InvoiceId = inv.Id,
            Amount = -req.Amount, // negative
            Method = req.Method,
            Reference = req.Reference,
            Reason = req.Reason,
            CapturedByUserId = userId,
            CapturedAtUtc = DateTime.UtcNow,
            PaidAtUtc = DateTime.UtcNow
        };

        _db.Payments.Add(refund);
        await _db.SaveChangesAsync();

        var newPaid = paid - req.Amount;
        var outstanding = inv.Total - newPaid;
        var displayStatus = ComputeDisplayStatus(inv.Status, inv.Total, newPaid);

        return Ok(new
        {
            InvoiceId = inv.Id,
            RefundPaymentId = refund.Id,
            Status = displayStatus,
            inv.Total,
            Paid = newPaid,
            Outstanding = outstanding
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> Get(Guid id)
    {
        var inv = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inv is null) return NotFound();

        var paid = inv.Payments.Sum(p => p.Amount);
        var outstanding = inv.Total - paid;
        var displayStatus = ComputeDisplayStatus(inv.Status, inv.Total, paid);

        return Ok(new
        {
            inv.Id,
            inv.InvoiceNumber,
            Status = displayStatus,
            inv.IssueDate,
            inv.DueDate,
            inv.Subtotal,
            inv.VatTotal,
            inv.Total,
            Paid = paid,
            Outstanding = outstanding,
            Customer = inv.Customer is null ? null : new { inv.Customer.Id, inv.Customer.Name, inv.Customer.Email },
            Items = inv.Items.Select(x => new
            {
                x.Id,
                x.Description,
                x.Qty,
                x.UnitPrice,
                x.VatRate,
                x.LineTotal,
                x.VatAmount
            }),
            Payments = inv.Payments
                .OrderByDescending(p => p.CapturedAtUtc)
                .Select(p => new
                {
                    p.Id,
                    p.Amount,
                    p.Method,
                    p.Reference,
                    p.CapturedAtUtc,
                    p.PaidAtUtc,
                    p.CapturedByUserId,
                    p.ReversedPaymentId,
                    p.Reason,
                    IsRefund = p.Amount < 0
                })
        });
    }

    [HttpGet]
    public async Task<ActionResult> List()
    {
        var list = await _db.Invoices
            .Include(i => i.Customer)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                CustomerName = i.Customer!.Name,
                Status =
                    i.Status == InvoiceStatus.Cancelled ? InvoiceStatus.Cancelled :
                    i.Status == InvoiceStatus.Draft ? InvoiceStatus.Draft :
                    (i.Total - i.Payments.Sum(p => p.Amount)) <= 0 ? InvoiceStatus.Paid :
                    i.Payments.Sum(p => p.Amount) > 0 ? InvoiceStatus.PartiallyPaid :
                    i.Status,
                i.Total,
                Paid = i.Payments.Sum(p => p.Amount),
                Outstanding = i.Total - i.Payments.Sum(p => p.Amount),
                i.IssueDate,
                i.DueDate
            })
            .ToListAsync();

        return Ok(list);
    }
}




