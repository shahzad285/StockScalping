namespace StockScalping.Models;

/// <summary>
/// User profile details returned by Angel One SmartAPI for the logged-in account.
/// </summary>
public class AccountProfile
{
    public string ClientCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string MobileNo { get; set; } = "";
    public string Broker { get; set; } = "";
    public List<string> Exchanges { get; set; } = new();
    public List<string> Products { get; set; } = new();
}
