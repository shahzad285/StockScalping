namespace StockTrading.Models;

public class StockProfile
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public string AssetType { get; set; } = "Unknown";
    public string? Theme { get; set; }
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? ClassificationReason { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? GrowthRate { get; set; }
    public decimal? DebtToEquity { get; set; }
    public decimal? PeRatio { get; set; }
    public decimal? EarningsPerShare { get; set; }
    public decimal? PriceToBook { get; set; }
    public decimal? TotalRevenue { get; set; }
    public decimal? NetIncome { get; set; }
    public decimal? TotalDebt { get; set; }
    public decimal? TotalCash { get; set; }
    public decimal? CashFlow { get; set; }
    public decimal? MarketCap { get; set; }
    public string StockCategory { get; set; } = "Unknown";
    public string? StockCategoryReason { get; set; }
    public decimal? StockCategoryConfidence { get; set; }
    public DateTime? StockCategoryUpdatedAtUtc { get; set; }
    public bool UpdatedByNse { get; set; }
    public bool UpdatedByYahoo { get; set; }
    public bool UpdatedByTapetide { get; set; }
    public DateTime? LastAnalyzedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
