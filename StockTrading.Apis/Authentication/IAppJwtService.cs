namespace StockTrading.Apis.Authentication;

public interface IAppJwtService
{
    string CreateToken(string subject);
}
