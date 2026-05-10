import { apiRequest } from "./apiClient";
import { StockExchange, StockSearchResult } from "./stockApi";

export type WatchlistStock = {
  watchlistId?: number;
  stockId?: number;
  symbol: string;
  exchange: string;
  symbolToken: string;
  tradingSymbol: string;
  purchaseRate?: number | null;
  salesRate?: number | null;
  assetType?: string;
  theme?: string | null;
  sector?: string | null;
  industry?: string | null;
  classificationReason?: string | null;
  confidenceScore?: number | null;
};

export async function getWatchlistStocks(): Promise<{ stocks: WatchlistStock[] }> {
  return apiRequest<{ stocks: WatchlistStock[] }>("/Watchlist/stocks");
}

export async function searchWatchlistStocks(
  query: string,
  exchange: StockExchange = "NSE"
): Promise<{ stocks: StockSearchResult[] }> {
  const params = new URLSearchParams({
    query,
    exchange
  });

  return apiRequest<{ stocks: StockSearchResult[] }>(`/Watchlist/stocks/search?${params.toString()}`);
}

export async function saveWatchlistStock(stock: WatchlistStock): Promise<{ stock: WatchlistStock }> {
  return apiRequest<{ stock: WatchlistStock }>("/Watchlist/stocks", {
    method: "POST",
    body: JSON.stringify(stock)
  });
}

export async function deleteWatchlistStock(symbol: string): Promise<void> {
  await apiRequest<void>(`/Watchlist/stocks/${encodeURIComponent(symbol)}`, {
    method: "DELETE"
  });
}

export async function deleteWatchlistStockById(watchlistId: number): Promise<void> {
  await apiRequest<void>(`/Watchlist/stocks/by-id/${watchlistId}`, {
    method: "DELETE"
  });
}
