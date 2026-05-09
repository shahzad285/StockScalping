using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StockTrading.Common.Settings;
using StockTrading.IServices;

namespace StockTrading.Services;

public sealed class BrevoEmailOtpSender(
    HttpClient httpClient,
    IOptions<BrevoSettings> settings) : IEmailOtpSender
{
    private readonly BrevoSettings _settings = settings.Value;

    public async Task<OtpDeliveryResult> SendAsync(
        string email,
        string otp,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            return OtpDeliveryResult.Failed("Brevo email OTP settings are missing.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email");
        request.Headers.Add("api-key", _settings.ApiKey);

        var payload = new
        {
            sender = new
            {
                email = _settings.FromEmail,
                name = _settings.FromName
            },
            to = new[] { new { email } },
            subject = _settings.Subject,
            textContent = BuildMessage(otp, expiresAtUtc)
        };

        var json = JsonSerializer.Serialize(payload);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return OtpDeliveryResult.Success();
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var errorMessage = string.IsNullOrWhiteSpace(responseBody)
            ? $"Brevo failed with status code {(int)response.StatusCode}."
            : $"Brevo failed with status code {(int)response.StatusCode}: {responseBody}";

        return OtpDeliveryResult.Failed(errorMessage);
    }

    private static string BuildMessage(string otp, DateTime expiresAtUtc)
    {
        return $"Your Stock Trading login OTP is {otp}. It expires at {expiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC.";
    }
}
