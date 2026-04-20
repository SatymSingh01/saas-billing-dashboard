using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaasBillingDashboard.Models;

public enum PaymentMethod { CreditCard, BankTransfer, PayPal, Check }

public class Payment
{
    public int Id { get; set; }

    [Required]
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public PaymentMethod Method { get; set; } = PaymentMethod.CreditCard;

    [StringLength(100)]
    public string? TransactionId { get; set; }

    [StringLength(200)]
    public string? Notes { get; set; }
}
