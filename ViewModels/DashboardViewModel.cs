namespace SaasBillingDashboard.ViewModels;

public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int OverdueInvoices { get; set; }
    public List<RecentInvoiceRow> RecentInvoices { get; set; } = new();
}

public class RecentInvoiceRow
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}
