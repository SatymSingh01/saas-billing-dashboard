using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaasBillingDashboard.Data;
using SaasBillingDashboard.Models;
using SaasBillingDashboard.ViewModels;

namespace SaasBillingDashboard.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    [AllowAnonymous]
    public IActionResult Error([FromServices] IWebHostEnvironment env)
    {
        var feature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (env.IsDevelopment() || feature != null)
            return Content($"Error: {feature?.Error?.Message}\n\n{feature?.Error?.StackTrace}", "text/plain");
        return StatusCode(500, "An unexpected error occurred.");
    }

    public async Task<IActionResult> Index()
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        // Fetch raw values into memory to avoid SQLite decimal translation issues
        var recentPaymentAmounts = await _db.Payments
            .Where(p => p.PaidAt >= cutoff)
            .Select(p => p.Amount)
            .ToListAsync();

        var outstandingInvoices = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
            .Select(i => new { i.Amount, i.TaxAmount })
            .ToListAsync();

        var recentInvoices = await _db.Invoices
            .Include(i => i.Customer)
            .OrderByDescending(i => i.IssuedDate)
            .Take(10)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            TotalCustomers = await _db.Customers.CountAsync(c => c.IsActive),
            ActiveSubscriptions = await _db.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.Active),
            MonthlyRevenue = recentPaymentAmounts.Sum(),
            OutstandingBalance = outstandingInvoices.Sum(i => i.Amount + i.TaxAmount),
            OverdueInvoices = await _db.Invoices.CountAsync(i => i.Status == InvoiceStatus.Overdue),
            RecentInvoices = recentInvoices.Select(i => new RecentInvoiceRow
            {
                InvoiceNumber = i.InvoiceNumber,
                CustomerName = i.Customer.Name,
                Total = i.Amount + i.TaxAmount,
                Status = i.Status.ToString(),
                DueDate = i.DueDate
            }).ToList()
        };

        return View(vm);
    }
}
