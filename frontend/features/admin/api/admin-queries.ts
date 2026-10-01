import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createCategory,
  createAdminPlaceholder,
  createDraftTemplateVersion,
  createTemplate,
  getAdminCategories,
  getAdminPlaceholders,
  getAdminTemplate,
  getAdminTemplateVersion,
  getAdminTemplateVersions,
  getAdminTemplates,
  publishTemplateVersion,
  removeAdminPlaceholder,
  setCategoryActive,
  setCurrentTemplateVersion,
  setTemplateActive,
  updateAdminPlaceholder,
  updateCategory,
  updateDraftTemplate,
  updateDraftTemplateVersion,
} from "@/services/admin-api";
import type {
  CreateCategoryRequestDto,
  CreatePlaceholderRequestDto,
  CreateTemplateRequestDto,
  UpdateCategoryRequestDto,
  UpdateDraftTemplateRequestDto,
  UpdateDraftTemplateVersionRequestDto,
  UpdatePlaceholderRequestDto,
} from "@/types/api";

export const adminKeys = {
  categories: ["admin", "categories"] as const,
  templates: ["admin", "templates"] as const,
  template: (templateId: string) => ["admin", "templates", templateId] as const,
  versions: (templateId: string) =>
    ["admin", "templates", templateId, "versions"] as const,
  version: (versionId: string) =>
    ["admin", "template-versions", versionId] as const,
  placeholders: (versionId: string) =>
    ["admin", "template-versions", versionId, "placeholders"] as const,
};

export function useAdminCategories() {
  return useQuery({ queryKey: adminKeys.categories, queryFn: getAdminCategories });
}

export function useCreateCategory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateCategoryRequestDto) => createCategory(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: adminKeys.categories }),
  });
}

export function useUpdateCategory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateCategoryRequestDto }) =>
      updateCategory(id, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: adminKeys.categories }),
  });
}

export function useSetCategoryActive() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      setCategoryActive(id, active),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: adminKeys.categories }),
  });
}

export function useAdminTemplates() {
  return useQuery({ queryKey: adminKeys.templates, queryFn: getAdminTemplates });
}

export function useAdminTemplate(templateId: string) {
  return useQuery({
    queryKey: adminKeys.template(templateId),
    queryFn: () => getAdminTemplate(templateId),
    enabled: Boolean(templateId),
  });
}

export function useCreateTemplate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateTemplateRequestDto) => createTemplate(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: adminKeys.templates }),
  });
}

export function useUpdateDraftTemplate(templateId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateDraftTemplateRequestDto) =>
      updateDraftTemplate(templateId, request),
    onSuccess: (data) => {
      queryClient.setQueryData(adminKeys.template(templateId), data);
      return queryClient.invalidateQueries({ queryKey: adminKeys.templates });
    },
  });
}

export function useSetTemplateActive(templateId?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      setTemplateActive(id, active),
    onSuccess: (data) => {
      queryClient.setQueryData(adminKeys.template(data.id), data);
      queryClient.invalidateQueries({ queryKey: adminKeys.templates });
      if (templateId) queryClient.invalidateQueries({ queryKey: adminKeys.template(templateId) });
    },
  });
}

export function useAdminTemplateVersions(templateId: string) {
  return useQuery({
    queryKey: adminKeys.versions(templateId),
    queryFn: () => getAdminTemplateVersions(templateId),
    enabled: Boolean(templateId),
  });
}

export function useCreateDraftTemplateVersion(templateId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => createDraftTemplateVersion(templateId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.template(templateId) });
      queryClient.invalidateQueries({ queryKey: adminKeys.versions(templateId) });
      queryClient.invalidateQueries({ queryKey: adminKeys.templates });
    },
  });
}

export function useAdminTemplateVersion(versionId: string) {
  return useQuery({
    queryKey: adminKeys.version(versionId),
    queryFn: () => getAdminTemplateVersion(versionId),
    enabled: Boolean(versionId),
  });
}

export function useUpdateDraftTemplateVersion(versionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateDraftTemplateVersionRequestDto) =>
      updateDraftTemplateVersion(versionId, request),
    onSuccess: (data) => {
      queryClient.setQueryData(adminKeys.version(versionId), data);
      queryClient.invalidateQueries({ queryKey: adminKeys.template(data.templateId) });
      queryClient.invalidateQueries({ queryKey: adminKeys.versions(data.templateId) });
    },
  });
}

function useVersionStateMutation(
  mutationFn: (versionId: string) => ReturnType<typeof publishTemplateVersion>,
) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: (data) => {
      queryClient.setQueryData(adminKeys.version(data.id), data);
      queryClient.invalidateQueries({ queryKey: adminKeys.template(data.templateId) });
      queryClient.invalidateQueries({ queryKey: adminKeys.versions(data.templateId) });
      queryClient.invalidateQueries({ queryKey: adminKeys.templates });
    },
  });
}

export function usePublishTemplateVersion() {
  return useVersionStateMutation(publishTemplateVersion);
}

export function useSetCurrentTemplateVersion() {
  return useVersionStateMutation(setCurrentTemplateVersion);
}

export function useAdminPlaceholders(versionId: string) {
  return useQuery({
    queryKey: adminKeys.placeholders(versionId),
    queryFn: () => getAdminPlaceholders(versionId),
    enabled: Boolean(versionId),
  });
}

export function useCreateAdminPlaceholder(versionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreatePlaceholderRequestDto) =>
      createAdminPlaceholder(versionId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.placeholders(versionId) });
      queryClient.invalidateQueries({ queryKey: adminKeys.version(versionId) });
    },
  });
}

export function useUpdateAdminPlaceholder(versionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      placeholderId,
      request,
    }: {
      placeholderId: string;
      request: UpdatePlaceholderRequestDto;
    }) => updateAdminPlaceholder(versionId, placeholderId, request),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: adminKeys.placeholders(versionId) }),
  });
}

export function useRemoveAdminPlaceholder(versionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (placeholderId: string) =>
      removeAdminPlaceholder(versionId, placeholderId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.placeholders(versionId) });
      queryClient.invalidateQueries({ queryKey: adminKeys.version(versionId) });
    },
  });
}
