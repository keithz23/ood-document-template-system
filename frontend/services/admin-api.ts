import { apiClient } from "@/services/api-client";
import type {
  AdminAuditLogDto,
  AdminCategoryDto,
  AdminTemplateDetailDto,
  AdminTemplateSummaryDto,
  AdminTemplateVersionDetailDto,
  AdminTemplateVersionSummaryDto,
  AdminUserDto,
  CreateAdminUserRequestDto,
  CreateCategoryRequestDto,
  CreatePlaceholderRequestDto,
  CreateTemplateRequestDto,
  PlaceholderDto,
  UpdateCategoryRequestDto,
  UpdateAdminUserRequestDto,
  UpdateDraftTemplateRequestDto,
  UpdateDraftTemplateVersionRequestDto,
  UpdatePlaceholderRequestDto,
  UpdateUserRoleRequestDto,
} from "@/types/api";

export async function getAdminCategories() {
  const response = await apiClient.get<AdminCategoryDto[]>("/admin/categories");
  return response.data;
}

export async function createCategory(request: CreateCategoryRequestDto) {
  const response = await apiClient.post<AdminCategoryDto>("/admin/categories", request);
  return response.data;
}

export async function updateCategory(
  categoryId: string,
  request: UpdateCategoryRequestDto,
) {
  const response = await apiClient.patch<AdminCategoryDto>(
    `/admin/categories/${categoryId}`,
    request,
  );
  return response.data;
}

export async function setCategoryActive(categoryId: string, active: boolean) {
  const response = await apiClient.post<AdminCategoryDto>(
    `/admin/categories/${categoryId}/${active ? "activate" : "deactivate"}`,
  );
  return response.data;
}

export async function getAdminTemplates() {
  const response = await apiClient.get<AdminTemplateSummaryDto[]>("/admin/templates");
  return response.data;
}

export async function getAdminTemplate(templateId: string) {
  const response = await apiClient.get<AdminTemplateDetailDto>(
    `/admin/templates/${templateId}`,
  );
  return response.data;
}

export async function createTemplate(request: CreateTemplateRequestDto) {
  const response = await apiClient.post<AdminTemplateDetailDto>(
    "/admin/templates",
    request,
  );
  return response.data;
}

export async function updateDraftTemplate(
  templateId: string,
  request: UpdateDraftTemplateRequestDto,
) {
  const response = await apiClient.patch<AdminTemplateDetailDto>(
    `/admin/templates/${templateId}`,
    request,
  );
  return response.data;
}

export async function setTemplateActive(templateId: string, active: boolean) {
  const response = await apiClient.post<AdminTemplateDetailDto>(
    `/admin/templates/${templateId}/${active ? "activate" : "deactivate"}`,
  );
  return response.data;
}

export async function getAdminTemplateVersions(templateId: string) {
  const response = await apiClient.get<AdminTemplateVersionSummaryDto[]>(
    `/admin/templates/${templateId}/versions`,
  );
  return response.data;
}

export async function createDraftTemplateVersion(templateId: string) {
  const response = await apiClient.post<AdminTemplateVersionDetailDto>(
    `/admin/templates/${templateId}/versions`,
  );
  return response.data;
}

export async function getAdminTemplateVersion(versionId: string) {
  const response = await apiClient.get<AdminTemplateVersionDetailDto>(
    `/admin/template-versions/${versionId}`,
  );
  return response.data;
}

export async function updateDraftTemplateVersion(
  versionId: string,
  request: UpdateDraftTemplateVersionRequestDto,
) {
  const response = await apiClient.patch<AdminTemplateVersionDetailDto>(
    `/admin/template-versions/${versionId}`,
    request,
  );
  return response.data;
}

export async function publishTemplateVersion(versionId: string) {
  const response = await apiClient.post<AdminTemplateVersionDetailDto>(
    `/admin/template-versions/${versionId}/publish`,
  );
  return response.data;
}

export async function setCurrentTemplateVersion(versionId: string) {
  const response = await apiClient.post<AdminTemplateVersionDetailDto>(
    `/admin/template-versions/${versionId}/set-current`,
  );
  return response.data;
}

export async function getAdminPlaceholders(versionId: string) {
  const response = await apiClient.get<PlaceholderDto[]>(
    `/admin/template-versions/${versionId}/placeholders`,
  );
  return response.data;
}

export async function createAdminPlaceholder(
  versionId: string,
  request: CreatePlaceholderRequestDto,
) {
  const response = await apiClient.post<PlaceholderDto>(
    `/admin/template-versions/${versionId}/placeholders`,
    request,
  );
  return response.data;
}

export async function updateAdminPlaceholder(
  versionId: string,
  placeholderId: string,
  request: UpdatePlaceholderRequestDto,
) {
  const response = await apiClient.patch<PlaceholderDto>(
    `/admin/template-versions/${versionId}/placeholders/${placeholderId}`,
    request,
  );
  return response.data;
}

export async function removeAdminPlaceholder(
  versionId: string,
  placeholderId: string,
) {
  await apiClient.delete(
    `/admin/template-versions/${versionId}/placeholders/${placeholderId}`,
  );
}

export async function getAdminUsers() {
  const response = await apiClient.get<AdminUserDto[]>("/admin/users");
  return response.data;
}

export async function createAdminUser(request: CreateAdminUserRequestDto) {
  const response = await apiClient.post<AdminUserDto>("/admin/users", request);
  return response.data;
}

export async function getAdminUser(userId: string) {
  const response = await apiClient.get<AdminUserDto>(`/admin/users/${userId}`);
  return response.data;
}

export async function updateAdminUser(
  userId: string,
  request: UpdateAdminUserRequestDto,
) {
  const response = await apiClient.patch<AdminUserDto>(
    `/admin/users/${userId}`,
    request,
  );
  return response.data;
}

export async function setUserActive(userId: string, active: boolean) {
  const response = await apiClient.post<AdminUserDto>(
    `/admin/users/${userId}/${active ? "activate" : "deactivate"}`,
  );
  return response.data;
}

export async function updateUserRole(
  userId: string,
  request: UpdateUserRoleRequestDto,
) {
  const response = await apiClient.patch<AdminUserDto>(
    `/admin/users/${userId}/role`,
    request,
  );
  return response.data;
}

export async function getAdminAuditLogs() {
  const response = await apiClient.get<AdminAuditLogDto[]>(
    "/admin/audit-logs",
  );
  return response.data;
}
