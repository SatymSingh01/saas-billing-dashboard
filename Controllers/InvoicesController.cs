using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SaasBillingDashboard.Data;
using SaasBillingDashboard.Models;

namespace SaasBillingDashboard.Controllers;

[Authorize]
public class InvoicesController : Controller
{
    private readonly ApplicationDbContext _db;

    public InvoicesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? status)
    {
        var query = _db.Invoices.Include(i => i.Customer).AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, out var parsed))
            query = query.Where(i => i.Status == parsed);

        ViewBag.StatusFilter = status;
        return View(await query.OrderByDescending(i => i.IssuedDate).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Subscription).ThenInclude(s => s!.Plan)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        return invoice == null ? NotFound() : View(invoice);
    }

    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create()
    {
        ViewBag.Customers = new SelectList(_db.Customers.Where(c => c.IsActive).OrderBy(c => c.Name), "Id", "Name");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(Invoice invoice)
    {
        if (ModelState.IsValid)
        {
            var count = await _db.Invoices.CountAsync() + 1;
            invoice.InvoiceNumber = $"INV-{count:D4}";
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = invoice.Id });
        }

        ViewBag.Customers = new SelectList(_db.Customers.Where(c => c.IsActive), "Id", "Name");
        return View(invoice);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateStatus(int id, InvoiceStatus status)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice != null)
        {
            invoice.Status = status;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RecordPayment(int invoiceId, decimal amount, PaymentMethod method, string? transactionId)
    {
        var invoice = await _db.Invoices.FindAsync(invoiceId);
        if (invoice != null)
        {
            _db.Payments.Add(new Payment
            {
                InvoiceId = invoiceId,
                Amount = amount,
                Method = method,
                TransactionId = transactionId
            });

            var totalPaid = await _db.Payments.Where(p => p.InvoiceId == invoiceId).SumAsync(p => p.Amount) + amount;
            if (totalPaid >= invoice.Amount + invoice.TaxAmount)
                invoice.Status = InvoiceStatus.Paid;

            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Details), new { id = invoiceId });
    }
}
