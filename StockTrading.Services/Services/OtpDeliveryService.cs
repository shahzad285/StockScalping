using StockTrading.Common.Enums;
using StockTrading.IServices;
using StockTrading.Models;

namespace StockTrading.Services;

public sealed class OtpDeliveryService(
    IEmailOtpSender emailOtpSender,
    IMobileOtpSender mobileOtpSender) : IOtpDeliveryService
{
    public Task<OtpDeliveryResult> SendLoginOtpAsync(
        ApplicationUser user,
        LoginMethod loginMethod,
        string otp,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (loginMethod == LoginMethod.EmailOtp)
        {
            return string.IsNullOrWhiteSpace(user.Email)
                ? Task.FromResult(OtpDeliveryResult.Failed("Email address is not available for this user."))
                : emailOtpSender.SendAsync(user.Email, otp, expiresAtUtc, cancellationToken);
        }

        if (loginMethod == LoginMethod.PhoneOtp)
        {
            return string.IsNullOrWhiteSpace(user.PhoneNumber)
                ? Task.FromResult(OtpDeliveryResult.Failed("Phone number is not available for this user."))
                : mobileOtpSender.SendAsync(user.PhoneNumber, otp, expiresAtUtc, cancellationToken);
        }

        return Task.FromResult(OtpDeliveryResult.Failed("Unsupported OTP login method."));
    }
}
