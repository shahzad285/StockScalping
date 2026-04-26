namespace StockScalping.Models;

public class StockProfile
{
    public required string Symbol { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal SalesRate { get; set; }
}