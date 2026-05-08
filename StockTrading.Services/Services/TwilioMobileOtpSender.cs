using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using StockTrading.Common.Settings;
using StockTrading.IServices;

namespace StockTrading.Services;

public sealed class TwilioMobileOtpSender(
    HttpClient httpClient,
    IOptions<TwilioSettings> settings) : IMobileOtpSender
{
    private readonly TwilioSettings _settings = settings.Value;

    public async Task<OtpDeliveryResult> SendAsync(
        string phoneNumber,
        string otp,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AccountSid)
            || string.IsNullOrWhiteSpace(_settings.AuthToken)
            || string.IsNullOrWhiteSpace(_settings.FromPhoneNumber))
        {
            return OtpDeliveryResult.Failed("Twilio mobile OTP settings are missing.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"2010-04-01/Accounts/{_settings.AccountSid}/Messages.json");

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = phoneNumber,
            ["From"] = _settings.FromPhoneNumber,
            ["Body"] = BuildMessage(otp, expiresAtUtc)
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? OtpDeliveryResult.Success()
            : OtpDeliveryResult.Failed($"Twilio failed with status code {(int)response.StatusCode}.");
    }

    private static string BuildMessage(string otp, DateTime expiresAtUtc)
    {
        return $"Your Stock Trading login OTP is {otp}. It expires at {expiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC.";
    }
}
