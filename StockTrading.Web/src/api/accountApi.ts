import { apiRequest } from "./apiClient";

export type LoginResponse = {
  message: string;
  token: string;
};

export type AccountProfile = {
  clientCode: string;
  name: string;
  email: string;
  mobileNo: string;
  broker: string;
  exchanges: string[];
  products: string[];
};

export async function login(totp: string): Promise<LoginResponse> {
  return apiRequest<LoginResponse>(`/Account/login?totp=${encodeURIComponent(totp)}`);
}

export async function getProfile(): Promise<{ profile: AccountProfile }> {
  return apiRequest<{ profile: AccountProfile }>("/Account/profile");
}
