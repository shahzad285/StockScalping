import { apiRequest } from "./apiClient";
import { StockExchange, StockSearchResult } from "./stockApi";

export type WatchlistStock = {
  watchlistId?: number;
  stockId?: number;
  symbol: string;
  name?: string | null;
  exchange: string;
  symbolToken: string;
  tradingSymbol: string;
  assetType?: string;
  theme?: string | null;
  sector?: string | null;
  industry?: string | null;
  classificationReason?: string | null;
  confidenceScore?: number | null;
  description?: string | null;
  updatedByNse?: boolean;
  updatedByYahoo?: boolean;
  updatedByTapetide?: boolean;
  dividendYield?: number | null;
  growthRate?: number | null;
  debtToEquity?: number | null;
  peRatio?: number | null;
  earningsPerShare?: number | null;
  priceToBook?: number | null;
  totalRevenue?: number | null;
  netIncome?: number | null;
  totalDebt?: number | null;
  totalCash?: number | null;
  cashFlow?: number | null;
  marketCap?: number | null;
  stockCategory?: string | null;
  stockCategoryReason?: string | null;
  stockCategoryConfidence?: number | null;
  stockCategoryUpdatedAtUtc?: string | null;
  lastAnalyzedAtUtc?: string | null;
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

  return apiRequest<{ stocks: StockSearchResult[] }>(`/Common/StockSearch?${params.toString()}`);
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
