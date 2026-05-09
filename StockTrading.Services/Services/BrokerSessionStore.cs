using System.Text.Json;
using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class BrokerSessionStore(
    ICacheService cacheService,
    IBrokerSessionRepository brokerSessionRepository,
    IStringEncryptionService encryptionService) : IBrokerSessionStore
{
    public async Task<BrokerSession?> GetAsync(
        string brokerName,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(brokerName, userId);
        var cachedSessionJson = cacheService.GetValue(cacheKey);
        if (!string.IsNullOrWhiteSpace(cachedSessionJson))
        {
            return JsonSerializer.Deserialize<BrokerSession>(cachedSessionJson);
        }

        var record = await brokerSessionRepository.GetActiveAsync(brokerName, userId, cancellationToken);
        if (record == null)
        {
            return null;
        }

        var session = ToSession(record);
        CacheSession(cacheKey, session);
        return session;
    }

    public async Task SaveAsync(BrokerSession session, CancellationToken cancellationToken = default)
    {
        await brokerSessionRepository.SaveAsync(ToRecord(session), cancellationToken);
        CacheSession(GetCacheKey(session.BrokerName, session.UserId), session);
    }

    public async Task ClearAsync(
        string brokerName,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        await brokerSessionRepository.ClearAsync(brokerName, userId, cancellationToken);
        cacheService.SetValue(GetCacheKey(brokerName, userId), "");
    }

    private BrokerSession ToSession(BrokerSessionRecord record)
    {
        return new BrokerSession
        {
            Id = record.Id,
            BrokerName = record.BrokerName,
            UserId = record.UserId,
            AccessToken = encryptionService.Decrypt(record.AccessTokenEncrypted),
            RefreshToken = encryptionService.Decrypt(record.RefreshTokenEncrypted),
            FeedToken = string.IsNullOrWhiteSpace(record.FeedTokenEncrypted)
                ? null
                : encryptionService.Decrypt(record.FeedTokenEncrypted),
            AccessTokenExpiresAtUtc = record.AccessTokenExpiresAtUtc,
            RefreshTokenExpiresAtUtc = record.RefreshTokenExpiresAtUtc,
            RawDataJson = record.RawDataJson,
            IsActive = record.IsActive,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private BrokerSessionRecord ToRecord(BrokerSession session)
    {
        return new BrokerSessionRecord
        {
            Id = session.Id,
            BrokerName = session.BrokerName,
            UserId = session.UserId,
            AccessTokenEncrypted = encryptionService.Encrypt(session.AccessToken),
            RefreshTokenEncrypted = encryptionService.Encrypt(session.RefreshToken),
            FeedTokenEncrypted = string.IsNullOrWhiteSpace(session.FeedToken)
                ? null
                : encryptionService.Encrypt(session.FeedToken),
            AccessTokenExpiresAtUtc = session.AccessTokenExpiresAtUtc,
            RefreshTokenExpiresAtUtc = session.RefreshTokenExpiresAtUtc,
            RawDataJson = session.RawDataJson,
            IsActive = session.IsActive,
            CreatedAtUtc = session.CreatedAtUtc,
            UpdatedAtUtc = session.UpdatedAtUtc
        };
    }

    private void CacheSession(string cacheKey, BrokerSession session)
    {
        cacheService.SetValue(cacheKey, JsonSerializer.Serialize(session));
    }

    private static string GetCacheKey(string brokerName, int? userId)
    {
        return $"BrokerSession:{brokerName}:{userId?.ToString() ?? "Global"}";
    }
}
