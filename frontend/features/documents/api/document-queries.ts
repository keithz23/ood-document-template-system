import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createDraftDocument,
  downloadDocument,
  finalizeDocument,
  getDocument,
  getDocumentHistory,
  previewDocument,
  updateDraftDocument,
} from "@/services/document-api";
import type {
  CreateDraftDocumentRequestDto,
  FinalizeDocumentRequestDto,
  PreviewDocumentRequestDto,
  UpdateDraftDocumentRequestDto,
} from "@/types/api";

export const documentKeys = {
  all: ["documents"] as const,
  history: () => [...documentKeys.all, "history"] as const,
  detail: (documentId: string) => [...documentKeys.all, "detail", documentId] as const,
};

export function useDocument(documentId: string) {
  return useQuery({
    queryKey: documentKeys.detail(documentId),
    queryFn: () => getDocument(documentId),
    enabled: Boolean(documentId),
  });
}

export function useDocumentHistory() {
  return useQuery({
    queryKey: documentKeys.history(),
    queryFn: getDocumentHistory,
  });
}

type InitializeDraftRequest = Readonly<{
  create: CreateDraftDocumentRequestDto;
  update: UpdateDraftDocumentRequestDto;
}>;

export function useCreateDraftDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ create, update }: InitializeDraftRequest) => {
      const document = await createDraftDocument(create);
      return updateDraftDocument(document.id, update);
    },
    onSuccess: (document) => {
      queryClient.setQueryData(documentKeys.detail(document.id), document);
      void queryClient.invalidateQueries({ queryKey: documentKeys.history() });
    },
  });
}

export function useUpdateDraftDocument(documentId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateDraftDocumentRequestDto) =>
      updateDraftDocument(documentId, request),
    onSuccess: (document) => {
      queryClient.setQueryData(documentKeys.detail(documentId), document);
      void queryClient.invalidateQueries({ queryKey: documentKeys.history() });
    },
  });
}

export function usePreviewDocument(documentId: string) {
  return useMutation({
    mutationFn: (request: PreviewDocumentRequestDto) =>
      previewDocument(documentId, request),
  });
}

export function useFinalizeDocument(documentId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: FinalizeDocumentRequestDto) =>
      finalizeDocument(documentId, request),
    onSuccess: (document) => {
      queryClient.setQueryData(documentKeys.detail(documentId), document);
      void queryClient.invalidateQueries({ queryKey: documentKeys.history() });
    },
  });
}

export function useDownloadDocument(documentId: string, title: string) {
  return useMutation({
    mutationFn: () => downloadDocument(documentId, title),
  });
}
