namespace StockTrading.Models;

public class OrderDetails
{
    public string OrderId { get; set; } = "";
    public string TradingSymbol { get; set; } = "";
    public string Exchange { get; set; } = "";
    public string TransactionType { get; set; } = "";
    public string OrderType { get; set; } = "";
    public string ProductType { get; set; } = "";
    public string Duration { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusCategory { get; set; } = "";
    public string RejectionReason { get; set; } = "";
    public int Quantity { get; set; }
    public int FilledShares { get; set; }
    public int UnfilledShares { get; set; }
    public int CancelledShares { get; set; }
    public decimal Price { get; set; }
    public decimal TriggerPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public string UpdateTime { get; set; } = "";
    public string ExchangeTime { get; set; } = "";
    public string ParentOrderId { get; set; } = "";
}
