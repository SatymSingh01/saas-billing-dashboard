using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SaasBillingDashboard.Data;
using SaasBillingDashboard.Models;

namespace SaasBillingDashboard.Controllers;

[Authorize]
public class SubscriptionsController : Controller
{
    private readonly ApplicationDbContext _db;

    public SubscriptionsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var subs = await _db.Subscriptions
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();
        return View(subs);
    }

    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create()
    {
        ViewBag.Customers = new SelectList(_db.Customers.Where(c => c.IsActive).OrderBy(c => c.Name), "Id", "Name");
        ViewBag.Plans = new SelectList(_db.SubscriptionPlans.Where(p => p.IsActive).OrderBy(p => p.Name), "Id", "Name");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(Subscription subscription)
    {
        if (ModelState.IsValid)
        {
            var plan = await _db.SubscriptionPlans.FindAsync(subscription.SubscriptionPlanId);
            subscription.NextBillingDate = plan?.BillingCycle == BillingCycle.Annual
                ? subscription.StartDate.AddYears(1)
                : subscription.StartDate.AddMonths(1);

            _db.Subscriptions.Add(subscription);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Customers = new SelectList(_db.Customers.Where(c => c.IsActive), "Id", "Name");
        ViewBag.Plans = new SelectList(_db.SubscriptionPlans.Where(p => p.IsActive), "Id", "Name");
        return View(subscription);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Cancel(int id)
    {
        var sub = await _db.Subscriptions.FindAsync(id);
        if (sub != null)
        {
            sub.Status = SubscriptionStatus.Cancelled;
            sub.EndDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
