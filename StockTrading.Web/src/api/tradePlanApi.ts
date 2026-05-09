import { apiRequest } from "./apiClient";

export type TradePlan = {
  id?: number;
  stockId?: number;
  watchlistItemId?: number | null;
  buyPrice: number;
  sellPrice: number;
  quantity: number;
  maxBudget?: number | null;
  status?: string;
  isActive: boolean;
  repeatEnabled: boolean;
  buyTriggerCount?: number;
  sellTriggerCount?: number;
  lastBuyTriggeredAtUtc?: string | null;
  lastSellTriggeredAtUtc?: string | null;
  symbol: string;
  exchange: string;
  symbolToken: string;
  tradingSymbol: string;
};

export async function getTradePlans(): Promise<{ tradePlans: TradePlan[] }> {
  return apiRequest<{ tradePlans: TradePlan[] }>("/TradePlan");
}

export async function saveTradePlan(tradePlan: TradePlan): Promise<{ tradePlan: TradePlan }> {
  return apiRequest<{ tradePlan: TradePlan }>("/TradePlan", {
    method: "POST",
    body: JSON.stringify(tradePlan)
  });
}

export async function deleteTradePlan(id: number): Promise<void> {
  await apiRequest<void>(`/TradePlan/${id}`, {
    method: "DELETE"
  });
}
