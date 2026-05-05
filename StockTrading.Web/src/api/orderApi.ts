import { apiRequest } from "./apiClient";

export type OrderDetails = {
  orderId: string;
  tradingSymbol: string;
  exchange: string;
  transactionType: string;
  orderType: string;
  productType: string;
  status: string;
  statusCategory: string;
  quantity: number;
  filledShares: number;
  averagePrice: number;
  updateTime: string;
};

export async function getOrders(): Promise<{ orders: OrderDetails[] }> {
  return apiRequest<{ orders: OrderDetails[] }>("/Order");
}
