import { apiClient } from "@/services/api-client";
import type {
  LoginRequestDto,
  LoginResponseDto,
  RegisterRequestDto,
  UserDto,
} from "@/types/api";

export async function login(request: LoginRequestDto) {
  const response = await apiClient.post<LoginResponseDto>("/auth/login", request);
  return response.data;
}

export async function getCurrentUser() {
  const response = await apiClient.get<UserDto>("/auth/me");
  return response.data;
}

export async function registerUser(request: RegisterRequestDto) {
  const response = await apiClient.post<UserDto>("/auth/register", request);
  return response.data;
}
