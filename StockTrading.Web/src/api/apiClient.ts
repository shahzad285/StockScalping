import { getToken } from "../auth/authStorage";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "/api";

export async function apiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();
  const headers = new Headers(options.headers);

  if (!headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(formatApiError(errorText, response.status));
  }

  return response.json() as Promise<T>;
}

function formatApiError(errorText: string, status: number): string {
  if (!errorText) {
    return `Request failed with status ${status}`;
  }

  try {
    const error = JSON.parse(errorText) as { message?: string; errors?: string[] };
    if (error.errors?.length) {
      return error.errors.join(" ");
    }

    return error.message || errorText;
  } catch {
    return errorText;
  }
}
