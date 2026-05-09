import { apiRequest } from "./apiClient";

export type LoginResponse = {
  message: string;
  token: string;
  roles: string[];
};

export enum LoginMethod {
  EmailOtp = 1,
  PhoneOtp = 2,
  GoogleOAuth = 3
}

export type RequestLoginOtpRequest = {
  loginMethod: LoginMethod;
  email?: string;
  phoneNumber?: string;
};

export type RequestLoginOtpResponse = {
  message: string;
  otp?: string | null;
  expiresAtUtc: string;
};

export type LoginRequest = RequestLoginOtpRequest & {
  otp: string;
  googleIdToken?: string;
};

export type SmartApiLoginRequest = {
  totp?: string;
};

export type SmartApiLoginResponse = {
  message: string;
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

export async function requestLoginOtp(request: RequestLoginOtpRequest): Promise<RequestLoginOtpResponse> {
  return apiRequest<RequestLoginOtpResponse>("/Account/login/request-otp", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export async function login(request: LoginRequest): Promise<LoginResponse> {
  return apiRequest<LoginResponse>("/Account/login", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export async function getProfile(): Promise<{ profile: AccountProfile }> {
  return apiRequest<{ profile: AccountProfile }>("/Account/profile");
}

export async function smartApiLogin(request: SmartApiLoginRequest): Promise<SmartApiLoginResponse> {
  return apiRequest<SmartApiLoginResponse>("/Account/smartapi/login", {
    method: "POST",
    body: JSON.stringify(request)
  });
}
