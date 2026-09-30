import { apiClient } from "@/services/api-client";
import type {
  AdminCategoryDto,
  AdminTemplateDetailDto,
  AdminTemplateSummaryDto,
  CreateCategoryRequestDto,
  CreateTemplateRequestDto,
  UpdateCategoryRequestDto,
  UpdateDraftTemplateRequestDto,
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
