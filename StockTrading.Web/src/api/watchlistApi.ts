import { apiRequest } from "./apiClient";
import { StockExchange, StockSearchResult } from "./stockApi";

export type WatchlistStock = {
  watchlistItemId?: number;
  watchlistId?: number;
  stockId?: number;
  symbol: string;
  exchange: string;
  symbolToken: string;
  tradingSymbol: string;
  purchaseRate?: number | null;
  salesRate?: number | null;
};

export type Watchlist = {
  id: number;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export async function getWatchlist(): Promise<{ stocks: WatchlistStock[] }> {
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

export async function getWatchlists(): Promise<{ watchlists: Watchlist[] }> {
  return apiRequest<{ watchlists: Watchlist[] }>("/Watchlist");
}

export async function createWatchlist(name: string): Promise<{ watchlist: Watchlist }> {
  return apiRequest<{ watchlist: Watchlist }>("/Watchlist", {
    method: "POST",
    body: JSON.stringify({ name })
  });
}

export async function deleteWatchlist(id: number): Promise<void> {
  await apiRequest<void>(`/Watchlist/${id}`, {
    method: "DELETE"
  });
}

export async function getWatchlistStocks(watchlistId: number): Promise<{ stocks: WatchlistStock[] }> {
  return apiRequest<{ stocks: WatchlistStock[] }>(`/Watchlist/${watchlistId}/stocks`);
}

export async function saveStockToWatchlist(
  watchlistId: number,
  stock: WatchlistStock
): Promise<{ stock: WatchlistStock }> {
  return apiRequest<{ stock: WatchlistStock }>(`/Watchlist/${watchlistId}/stocks`, {
    method: "POST",
    body: JSON.stringify(stock)
  });
}

export async function deleteStockFromWatchlist(watchlistId: number, watchlistItemId: number): Promise<void> {
  await apiRequest<void>(`/Watchlist/${watchlistId}/stocks/${watchlistItemId}`, {
    method: "DELETE"
  });
}
