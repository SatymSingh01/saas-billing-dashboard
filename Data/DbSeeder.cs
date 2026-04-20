using Microsoft.AspNetCore.Identity;
using SaasBillingDashboard.Models;

namespace SaasBillingDashboard.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager)
    {
        foreach (var role in new[] { "Admin", "Manager", "Viewer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (!userManager.Users.Any())
        {
            var admin = new IdentityUser { UserName = "admin@billing.dev", Email = "admin@billing.dev", EmailConfirmed = true };
            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, "Admin");

            var manager = new IdentityUser { UserName = "manager@billing.dev", Email = "manager@billing.dev", EmailConfirmed = true };
            await userManager.CreateAsync(manager, "Manager123!");
            await userManager.AddToRoleAsync(manager, "Manager");
        }

        if (!db.SubscriptionPlans.Any())
        {
            db.SubscriptionPlans.AddRange(
                new SubscriptionPlan { Name = "Starter", Price = 9.99m, BillingCycle = BillingCycle.Monthly, Description = "Up to 5 users, basic features" },
                new SubscriptionPlan { Name = "Professional", Price = 49.99m, BillingCycle = BillingCycle.Monthly, Description = "Up to 25 users, advanced analytics" },
                new SubscriptionPlan { Name = "Enterprise", Price = 199.99m, BillingCycle = BillingCycle.Monthly, Description = "Unlimited users, priority support" },
                new SubscriptionPlan { Name = "Enterprise Annual", Price = 1999.99m, BillingCycle = BillingCycle.Annual, Description = "Unlimited users, 2 months free" }
            );
            await db.SaveChangesAsync();
        }

        if (!db.Customers.Any())
        {
            var plans = db.SubscriptionPlans.ToList();
            var customers = new List<Customer>
            {
                new() { Name = "Acme Corp", Email = "billing@acme.com", Company = "Acme Corp", Phone = "555-0101" },
                new() { Name = "TechStart Inc", Email = "finance@techstart.io", Company = "TechStart Inc", Phone = "555-0102" },
                new() { Name = "DataFlow LLC", Email = "accounts@dataflow.com", Company = "DataFlow LLC" },
                new() { Name = "CloudNine Solutions", Email = "billing@cloudnine.io", Company = "CloudNine Solutions" },
                new() { Name = "Rapid Deploy", Email = "ops@rapiddeploy.dev", Company = "Rapid Deploy" },
            };
            db.Customers.AddRange(customers);
            await db.SaveChangesAsync();

            var seededCustomers = db.Customers.ToList();
            var proPlan = plans.First(p => p.Name == "Professional");
            var starterPlan = plans.First(p => p.Name == "Starter");

            var subscriptions = new List<Subscription>
            {
                new() { CustomerId = seededCustomers[0].Id, SubscriptionPlanId = proPlan.Id, StartDate = DateTime.UtcNow.AddMonths(-6), NextBillingDate = DateTime.UtcNow.AddMonths(1) },
                new() { CustomerId = seededCustomers[1].Id, SubscriptionPlanId = starterPlan.Id, StartDate = DateTime.UtcNow.AddMonths(-3), NextBillingDate = DateTime.UtcNow.AddDays(5) },
                new() { CustomerId = seededCustomers[2].Id, SubscriptionPlanId = proPlan.Id, StartDate = DateTime.UtcNow.AddMonths(-1), NextBillingDate = DateTime.UtcNow.AddMonths(1) },
            };
            db.Subscriptions.AddRange(subscriptions);
            await db.SaveChangesAsync();

            var seededSubs = db.Subscriptions.ToList();
            var invoices = new List<Invoice>
            {
                new() { InvoiceNumber = "INV-0001", CustomerId = seededCustomers[0].Id, SubscriptionId = seededSubs[0].Id, Amount = 49.99m, TaxAmount = 5.00m, DueDate = DateTime.UtcNow.AddDays(-10), Status = InvoiceStatus.Paid },
                new() { InvoiceNumber = "INV-0002", CustomerId = seededCustomers[1].Id, SubscriptionId = seededSubs[1].Id, Amount = 9.99m, TaxAmount = 1.00m, DueDate = DateTime.UtcNow.AddDays(5), Status = InvoiceStatus.Sent },
                new() { InvoiceNumber = "INV-0003", CustomerId = seededCustomers[2].Id, Amount = 49.99m, TaxAmount = 5.00m, DueDate = DateTime.UtcNow.AddDays(-30), Status = InvoiceStatus.Overdue },
                new() { InvoiceNumber = "INV-0004", CustomerId = seededCustomers[0].Id, Amount = 49.99m, TaxAmount = 5.00m, DueDate = DateTime.UtcNow.AddDays(15), Status = InvoiceStatus.Draft },
            };
            db.Invoices.AddRange(invoices);
            await db.SaveChangesAsync();

            var paidInvoice = db.Invoices.First(i => i.Status == InvoiceStatus.Paid);
            db.Payments.Add(new Payment
            {
                InvoiceId = paidInvoice.Id,
                Amount = 54.99m,
                Method = PaymentMethod.CreditCard,
                TransactionId = "TXN-ABC123"
            });
            await db.SaveChangesAsync();
        }
    }
}
