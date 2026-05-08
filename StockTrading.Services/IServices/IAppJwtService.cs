using StockTrading.Models;

namespace StockTrading.IServices;

public interface IAppJwtService
{
    string CreateToken(ApplicationUser user, IReadOnlyCollection<string> roles);
}
