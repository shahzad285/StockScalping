using StockTrading.Models;

namespace StockTrading.Apis.Authentication;

public interface IAppJwtService
{
    string CreateToken(ApplicationUser user, IReadOnlyCollection<string> roles);
}
