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

    public async Task<IActionResult> Index()
    {
        var vm = new DashboardViewModel
        {
            TotalCustomers = await _db.Customers.CountAsync(c => c.IsActive),
            ActiveSubscriptions = await _db.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.Active),
            MonthlyRevenue = await _db.Payments
                .Where(p => p.PaidAt >= DateTime.UtcNow.AddDays(-30))
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            OutstandingBalance = await _db.Invoices
                .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                .SumAsync(i => (decimal?)(i.Amount + i.TaxAmount)) ?? 0,
            OverdueInvoices = await _db.Invoices.CountAsync(i => i.Status == InvoiceStatus.Overdue),
            RecentInvoices = await _db.Invoices
                .Include(i => i.Customer)
                .OrderByDescending(i => i.IssuedDate)
                .Take(10)
                .Select(i => new RecentInvoiceRow
                {
                    InvoiceNumber = i.InvoiceNumber,
                    CustomerName = i.Customer.Name,
                    Total = i.Amount + i.TaxAmount,
                    Status = i.Status.ToString(),
                    DueDate = i.DueDate
                })
                .ToListAsync()
        };

        return View(vm);
    }
}
