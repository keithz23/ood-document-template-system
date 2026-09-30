import { apiClient } from "@/services/api-client";
import type { LoginRequestDto, LoginResponseDto, UserDto } from "@/types/api";

export async function login(request: LoginRequestDto) {
  const response = await apiClient.post<LoginResponseDto>("/auth/login", request);
  return response.data;
}

export async function getCurrentUser() {
  const response = await apiClient.get<UserDto>("/auth/me");
  return response.data;
}
