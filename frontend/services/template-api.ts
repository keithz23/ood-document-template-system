import { apiClient } from "@/services/api-client";
import type {
  PlaceholderDto,
  TemplateDetailDto,
  TemplateGalleryItemDto,
  TemplateVersionDetailDto,
} from "@/types/api";

export async function getActiveTemplates() {
  const response = await apiClient.get<TemplateGalleryItemDto[]>("/templates");
  return response.data;
}

export async function getTemplateDetail(templateId: string) {
  const response = await apiClient.get<TemplateDetailDto>(`/templates/${templateId}`);
  return response.data;
}

export async function getCurrentTemplateVersion(templateId: string) {
  const response = await apiClient.get<TemplateVersionDetailDto>(
    `/templates/${templateId}/current-version`,
  );
  return response.data;
}

export async function getTemplateVersionPlaceholders(templateVersionId: string) {
  const response = await apiClient.get<PlaceholderDto[]>(
    `/template-versions/${templateVersionId}/placeholders`,
  );
  return response.data;
}
