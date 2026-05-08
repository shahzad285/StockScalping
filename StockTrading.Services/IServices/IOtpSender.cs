namespace StockTrading.IServices;

public interface IEmailOtpSender
{
    Task<OtpDeliveryResult> SendAsync(
        string email,
        string otp,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);
}

public interface IMobileOtpSender
{
    Task<OtpDeliveryResult> SendAsync(
        string phoneNumber,
        string otp,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);
}
