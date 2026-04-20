using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaasBillingDashboard.Data;
using SaasBillingDashboard.Models;

namespace SaasBillingDashboard.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PaymentsApiController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? invoiceId)
    {
        var query = _db.Payments.Include(p => p.Invoice).AsQueryable();
        if (invoiceId.HasValue)
            query = query.Where(p => p.InvoiceId == invoiceId.Value);

        var payments = await query.OrderByDescending(p => p.PaidAt).Select(p => new
        {
            p.Id,
            p.InvoiceId,
            InvoiceNumber = p.Invoice.InvoiceNumber,
            p.Amount,
            p.Method,
            p.TransactionId,
            p.PaidAt
        }).ToListAsync();

        return Ok(payments);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _db.Payments.Include(p => p.Invoice).FirstOrDefaultAsync(p => p.Id == id);
        return payment == null ? NotFound() : Ok(payment);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var invoice = await _db.Invoices.FindAsync(request.InvoiceId);
        if (invoice == null) return NotFound(new { error = "Invoice not found" });

        var payment = new Payment
        {
            InvoiceId = request.InvoiceId,
            Amount = request.Amount,
            Method = request.Method,
            TransactionId = request.TransactionId,
            Notes = request.Notes
        };

        _db.Payments.Add(payment);

        var totalPaid = await _db.Payments.Where(p => p.InvoiceId == request.InvoiceId).SumAsync(p => p.Amount) + request.Amount;
        if (totalPaid >= invoice.Amount + invoice.TaxAmount)
            invoice.Status = InvoiceStatus.Paid;

        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var now = DateTime.UtcNow;
        return Ok(new
        {
            TotalRevenue = await _db.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0,
            Last30Days = await _db.Payments.Where(p => p.PaidAt >= now.AddDays(-30)).SumAsync(p => (decimal?)p.Amount) ?? 0,
            Last7Days = await _db.Payments.Where(p => p.PaidAt >= now.AddDays(-7)).SumAsync(p => (decimal?)p.Amount) ?? 0,
            TotalTransactions = await _db.Payments.CountAsync()
        });
    }
}

public record CreatePaymentRequest(
    int InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    string? TransactionId,
    string? Notes
);
