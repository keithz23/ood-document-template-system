import axios from "axios";
import {
  clearStoredSession,
  getStoredSession,
  notifyUnauthorized,
} from "@/features/auth/auth-storage";

export const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api",
  headers: {
    "Content-Type": "application/json",
  },
});

apiClient.interceptors.request.use((config) => {
  const session = getStoredSession();
  if (session) {
    config.headers.Authorization = `Bearer ${session.accessToken}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      clearStoredSession();
      const isLoginRequest = error.config?.url?.endsWith("/auth/login");

      if (!isLoginRequest && typeof window !== "undefined") {
        notifyUnauthorized(`${window.location.pathname}${window.location.search}`);
      }
    }

    return Promise.reject(error);
  },
);
