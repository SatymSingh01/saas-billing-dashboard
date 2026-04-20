using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaasBillingDashboard.Models;

public enum InvoiceStatus { Draft, Sent, Paid, Overdue, Cancelled }

public class Invoice
{
    public int Id { get; set; }

    [StringLength(20)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount => Amount + TaxAmount;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [StringLength(500)]
    public string? Notes { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
