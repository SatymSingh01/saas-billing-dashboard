using System.ComponentModel.DataAnnotations;

namespace SaasBillingDashboard.Models;

public enum SubscriptionStatus { Active, Cancelled, Paused, Expired }

public class Subscription
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [Required]
    public int SubscriptionPlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime? NextBillingDate { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
