import { apiRequest } from "./apiClient";
import { WatchlistStock } from "./watchlistApi";

export type HoldingStock = {
  stockName: string;
  tradingSymbol: string;
  exchange: string;
  symbolToken: string;
  purchasePrice: number;
  totalStocks: number;
  currentPrice: number;
  totalGainOrLoss: number;
};

export type HoldingsResponse = {
  stocks: HoldingStock[];
  totalProfitLoss: number;
};

export type StockPrice = {
  symbol: string;
  tradingSymbol: string;
  exchange: string;
  symbolToken: string;
  lastTradedPrice: number;
  isFetched: boolean;
  message: string;
};

export type StockExchange = "NSE" | "BSE";

export type StockSearchResult = {
  symbol: string;
  tradingSymbol: string;
  exchange: StockExchange;
  symbolToken: string;
  name?: string | null;
};

export type StockMaster = {
  id?: number;
  symbol: string;
  name?: string | null;
  exchange: StockExchange;
  symbolToken: string;
  tradingSymbol: string;
  createdAtUtc?: string;
  updatedAtUtc?: string | null;
};

export type StockChartRange = "OneDay" | "OneWeek" | "OneMonth" | "SixMonths" | "OneYear";

export type StockCandle = {
  time: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
};

export async function getHoldings(): Promise<HoldingsResponse> {
  return apiRequest<HoldingsResponse>("/Stock/holdings");
}

export async function getStocks(): Promise<{ stocks: WatchlistStock[] }> {
  return apiRequest<{ stocks: WatchlistStock[] }>("/Stock/stocks");
}

export async function saveStock(stock: StockMaster): Promise<{ stock: StockMaster }> {
  return apiRequest<{ stock: StockMaster }>("/Stock/stocks", {
    method: "POST",
    body: JSON.stringify(stock)
  });
}

export async function deleteStock(stockId: number): Promise<void> {
  await apiRequest<void>(`/Stock/stocks/${stockId}`, {
    method: "DELETE"
  });
}

export async function getPrices(): Promise<{ prices: StockPrice[] }> {
  return apiRequest<{ prices: StockPrice[] }>("/Stock/prices");
}

export async function searchStocks(query: string, exchange: StockExchange = "NSE"): Promise<{ stocks: StockSearchResult[] }> {
  const params = new URLSearchParams({
    query,
    exchange
  });

  return apiRequest<{ stocks: StockSearchResult[] }>(`/Common/StockSearch?${params.toString()}`);
}

export async function getStockChart(
  symbolToken: string,
  exchange: StockExchange = "NSE",
  range: StockChartRange = "OneMonth"
): Promise<{ candles: StockCandle[] }> {
  const params = new URLSearchParams({
    symbolToken,
    exchange,
    range
  });

  return apiRequest<{ candles: StockCandle[] }>(`/Stock/chart?${params.toString()}`);
}
