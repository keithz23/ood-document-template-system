import { apiClient } from "@/services/api-client";
import type {
  LoginRequestDto,
  LoginResponseDto,
  ForgotPasswordRequestDto,
  ForgotPasswordResponseDto,
  RegisterRequestDto,
  ResetPasswordRequestDto,
  UpdateOwnProfileRequestDto,
  ChangePasswordRequestDto,
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

export async function updateOwnProfile(request: UpdateOwnProfileRequestDto) {
  const response = await apiClient.patch<UserDto>("/users/me", request);
  return response.data;
}

export async function changePassword(request: ChangePasswordRequestDto) {
  await apiClient.post("/users/me/change-password", request);
}

export async function forgotPassword(request: ForgotPasswordRequestDto) {
  const response = await apiClient.post<ForgotPasswordResponseDto>(
    "/auth/forgot-password",
    request,
  );
  return response.data;
}

export async function resetPassword(request: ResetPasswordRequestDto) {
  await apiClient.post("/auth/reset-password", request);
}
