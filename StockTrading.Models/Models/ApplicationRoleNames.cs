using StockTrading.Common.Enums;

namespace StockTrading.Models;

public static class ApplicationRoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string User = "User";

    public static int GetRoleId(string roleName)
    {
        return string.Equals(roleName, SuperAdmin, StringComparison.OrdinalIgnoreCase)
            ? (int)UserRole.SuperAdmin
            : (int)UserRole.User;
    }
}
