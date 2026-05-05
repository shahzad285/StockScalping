namespace StockTrading.Common.DTOs;

public class HoldingsResponse
{
    public List<HoldingStock> Stocks { get; set; } = new();

    public decimal TotalProfitLoss => Stocks.Sum(stock => stock.TotalGainOrLoss);
}
