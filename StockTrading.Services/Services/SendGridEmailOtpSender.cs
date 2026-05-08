using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StockTrading.Common.Settings;
using StockTrading.IServices;

namespace StockTrading.Services;

public sealed class SendGridEmailOtpSender(
    HttpClient httpClient,
    IOptions<SendGridSettings> settings) : IEmailOtpSender
{
    private readonly SendGridSettings _settings = settings.Value;

    public async Task<OtpDeliveryResult> SendAsync(
        string email,
        string otp,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            return OtpDeliveryResult.Failed("SendGrid email OTP settings are missing.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "v3/mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var payload = new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[] { new { email } }
                }
            },
            from = new
            {
                email = _settings.FromEmail,
                name = _settings.FromName
            },
            subject = _settings.Subject,
            content = new[]
            {
                new
                {
                    type = "text/plain",
                    value = BuildMessage(otp, expiresAtUtc)
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? OtpDeliveryResult.Success()
            : OtpDeliveryResult.Failed($"SendGrid failed with status code {(int)response.StatusCode}.");
    }

    private static string BuildMessage(string otp, DateTime expiresAtUtc)
    {
        return $"Your Stock Trading login OTP is {otp}. It expires at {expiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC.";
    }
}
