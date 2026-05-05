import { apiRequest } from "./apiClient";

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

export async function getHoldings(): Promise<HoldingsResponse> {
  return apiRequest<HoldingsResponse>("/Stock/holdings");
}

export async function getPrices(): Promise<{ prices: StockPrice[] }> {
  return apiRequest<{ prices: StockPrice[] }>("/Stock/prices");
}
