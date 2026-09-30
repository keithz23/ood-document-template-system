import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createCategory,
  createTemplate,
  getAdminCategories,
  getAdminTemplate,
  getAdminTemplates,
  setCategoryActive,
  setTemplateActive,
  updateCategory,
  updateDraftTemplate,
} from "@/services/admin-api";
import type {
  CreateCategoryRequestDto,
  CreateTemplateRequestDto,
  UpdateCategoryRequestDto,
  UpdateDraftTemplateRequestDto,
} from "@/types/api";

export const adminKeys = {
  categories: ["admin", "categories"] as const,
  templates: ["admin", "templates"] as const,
  template: (templateId: string) => ["admin", "templates", templateId] as const,
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
