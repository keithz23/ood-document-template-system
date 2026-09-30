import { useQuery } from "@tanstack/react-query";
import {
  getActiveTemplates,
  getCurrentTemplateVersion,
  getTemplateDetail,
  getTemplateVersionPlaceholders,
} from "@/services/template-api";

export const templateKeys = {
  all: ["templates"] as const,
  detail: (templateId: string) => ["templates", templateId] as const,
  currentVersion: (templateId: string) =>
    ["templates", templateId, "current-version"] as const,
  placeholders: (templateVersionId: string) =>
    ["template-versions", templateVersionId, "placeholders"] as const,
};

export function useActiveTemplates() {
  return useQuery({ queryKey: templateKeys.all, queryFn: getActiveTemplates });
}

export function useTemplateDetail(templateId: string) {
  return useQuery({
    queryKey: templateKeys.detail(templateId),
    queryFn: () => getTemplateDetail(templateId),
    enabled: Boolean(templateId),
  });
}

export function useCurrentTemplateVersion(templateId: string) {
  return useQuery({
    queryKey: templateKeys.currentVersion(templateId),
    queryFn: () => getCurrentTemplateVersion(templateId),
    enabled: Boolean(templateId),
  });
}

export function useTemplateVersionPlaceholders(templateVersionId?: string) {
  return useQuery({
    queryKey: templateKeys.placeholders(templateVersionId ?? "pending"),
    queryFn: () => getTemplateVersionPlaceholders(templateVersionId!),
    enabled: Boolean(templateVersionId),
  });
}
