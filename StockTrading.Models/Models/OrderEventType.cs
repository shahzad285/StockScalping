namespace StockTrading.Models;

public enum OrderEventType
{
    Unknown = 0,
    Placed = 1,
    Updated = 2,
    Executed = 3,
    Cancelled = 4,
    Rejected = 5,
    Failed = 6
}
