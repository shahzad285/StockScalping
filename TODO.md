# TODO

## Planned

- Check the best free or low-cost API/source for Indian stock fundamental data before implementation.
- Add fundamentals data for chart details such as market cap, P/E ratio, dividend yield, and quarterly dividend amount.
- Add chart-derived details below the stock chart: open, high, low, close, 52-week high, and 52-week low.
- Improve stock chart readability and hover details as needed.
- Decide whether chart should also be available from Trade Plan rows.
- Get details of stocks created or recommended by reputable stock market investors/retailers such as Jhunjhunwala.

## Later

- Add realtime or near-realtime price updates for watchlists and trade plans.
- Add backend monitoring for trade plans so planned trades can be checked even when the browser is closed.
- Consider SignalR for live frontend updates after backend monitoring exists.
- Improve stock search with a local instrument master or fallback search strategy.
- Track Angel One JWT and refresh token expiry times in broker session storage.
- Research an API/source for identifying Islamically allowed/Shariah-compliant stocks in India.

## Done

- Added stock search API with NSE/BSE only and NSE as default.
- Added stock search to Watchlist and Trade Plan forms.
- Made stock identity fields view-only after selecting a search result.
- Added Watchlist chart popup using Angel One historical candles.
- Added 1-day app login JWT expiry.
- Made Watchlist buy/sell target prices optional.
