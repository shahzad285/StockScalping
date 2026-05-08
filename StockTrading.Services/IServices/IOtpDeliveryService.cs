using StockTrading.Models;

namespace StockTrading.IServices;

public interface IOtpDeliveryService
{
    Task<OtpDeliveryResult> SendLoginOtpAsync(
        ApplicationUser user,
        LoginMethod loginMethod,
        string otp,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);
}

public sealed record OtpDeliveryResult(bool IsSuccess, string? ErrorMessage = null)
{
    public static OtpDeliveryResult Success()
    {
        return new OtpDeliveryResult(true);
    }

    public static OtpDeliveryResult Failed(string errorMessage)
    {
        return new OtpDeliveryResult(false, errorMessage);
    }
}
