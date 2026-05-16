using Microsoft.Extensions.Options;
using StockTrading.Common.DTOs;
using StockTrading.Common.Settings;
using StockTrading.IServices;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class MarketScheduleService(
    IMarketJobDecisionRepository marketJobDecisionRepository,
    IOptions<MarketScheduleSettings> options) : IMarketScheduleService
{
    private readonly MarketScheduleSettings _settings = options.Value;

    public async Task<MarketJobDecisionResult> DecideAsync(
        string jobName,
        string exchange = "NSE",
        CancellationToken cancellationToken = default)
    {
        var timeZone = GetTimeZone();
        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);
        var decisionDate = DateOnly.FromDateTime(nowLocal);
        var localTime = TimeOnly.FromDateTime(nowLocal);
        var openTime = ParseTime(_settings.OpenTime, new TimeOnly(9, 0));
        var closeTime = ParseTime(_settings.CloseTime, new TimeOnly(15, 30));
        var isWeekend = nowLocal.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var isInMarketWindow = localTime >= openTime && localTime < closeTime;
        var isTradingDay = !isWeekend;
        var jobsEnabled = isTradingDay && isInMarketWindow;

        var decision = new MarketJobDecisionResult
        {
            DecisionDate = decisionDate,
            Exchange = string.IsNullOrWhiteSpace(exchange) ? "NSE" : exchange.Trim().ToUpperInvariant(),
            JobName = string.IsNullOrWhiteSpace(jobName) ? "Unknown" : jobName.Trim(),
            IsTradingDay = isTradingDay,
            JobsEnabled = jobsEnabled,
            MarketOpenTime = openTime,
            MarketCloseTime = closeTime,
            JobsDisabledUntilUtc = jobsEnabled ? null : GetNextOpenUtc(nowLocal, openTime, timeZone),
            DecisionReason = GetReason(isWeekend, localTime, openTime, closeTime)
        };

        await marketJobDecisionRepository.UpsertAsync(decision, cancellationToken);
        return decision;
    }

    private static string GetReason(bool isWeekend, TimeOnly localTime, TimeOnly openTime, TimeOnly closeTime)
    {
        if (isWeekend)
        {
            return "Weekend. Market jobs are disabled until the next working day.";
        }

        if (localTime < openTime)
        {
            return "Before market open. Market jobs start at the configured open time.";
        }

        if (localTime >= closeTime)
        {
            return "After market close. Market jobs are disabled until the next working day.";
        }

        return "Market is inside the configured trading window.";
    }

    private static DateTime GetNextOpenUtc(DateTime nowLocal, TimeOnly openTime, TimeZoneInfo timeZone)
    {
        var nextDate = DateOnly.FromDateTime(nowLocal);
        if (TimeOnly.FromDateTime(nowLocal) >= openTime)
        {
            nextDate = nextDate.AddDays(1);
        }

        while (nextDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            nextDate = nextDate.AddDays(1);
        }

        var nextOpenLocal = nextDate.ToDateTime(openTime);
        return TimeZoneInfo.ConvertTimeToUtc(nextOpenLocal, timeZone);
    }

    private TimeZoneInfo GetTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
    }

    private static TimeOnly ParseTime(string value, TimeOnly fallback)
    {
        return TimeOnly.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }
}
