import axios from "axios";
import type { ErrorResponseDto } from "@/types/api";

export function getApiError(error: unknown): ErrorResponseDto | null {
  if (!axios.isAxiosError<ErrorResponseDto>(error)) return null;
  return error.response?.data ?? null;
}

export function getApiErrorMessage(error: unknown, fallback: string) {
  const apiError = getApiError(error);
  return apiError?.detail || apiError?.title || fallback;
}
