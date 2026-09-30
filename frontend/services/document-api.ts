import { apiClient } from "@/services/api-client";
import type {
  CreateDraftDocumentRequestDto,
  DocumentDetailDto,
  DocumentSummaryDto,
  FinalizeDocumentRequestDto,
  PreviewDocumentRequestDto,
  PreviewDocumentResponseDto,
  UpdateDraftDocumentRequestDto,
} from "@/types/api";

export async function createDraftDocument(request: CreateDraftDocumentRequestDto) {
  const response = await apiClient.post<DocumentDetailDto>("/documents", request);
  return response.data;
}

export async function getDocument(documentId: string) {
  const response = await apiClient.get<DocumentDetailDto>(`/documents/${documentId}`);
  return response.data;
}

export async function getDocumentHistory() {
  const response = await apiClient.get<DocumentSummaryDto[]>("/documents");
  return response.data;
}

export async function updateDraftDocument(
  documentId: string,
  request: UpdateDraftDocumentRequestDto,
) {
  const response = await apiClient.patch<DocumentDetailDto>(
    `/documents/${documentId}`,
    request,
  );
  return response.data;
}

export async function previewDocument(
  documentId: string,
  request: PreviewDocumentRequestDto,
) {
  const response = await apiClient.post<PreviewDocumentResponseDto>(
    `/documents/${documentId}/preview`,
    request,
  );
  return response.data;
}

export async function finalizeDocument(
  documentId: string,
  request: FinalizeDocumentRequestDto,
) {
  const response = await apiClient.post<DocumentDetailDto>(
    `/documents/${documentId}/finalize`,
    request,
  );
  return response.data;
}

function getDownloadFileName(header: string | undefined, fallbackTitle: string) {
  const utf8Name = header?.match(/filename\*=UTF-8''([^;]+)/i)?.[1];
  if (utf8Name) return decodeURIComponent(utf8Name.replaceAll('"', ""));

  const quotedName = header?.match(/filename="?([^";]+)"?/i)?.[1];
  if (quotedName) return quotedName;

  const safeTitle = fallbackTitle
    .trim()
    .replace(/[^\p{L}\p{N}._-]+/gu, "-")
    .replace(/^[._-]+|[._-]+$/g, "") || "document";
  return `${safeTitle}.html`;
}

export async function downloadDocument(documentId: string, title: string) {
  const response = await apiClient.get<Blob>(`/documents/${documentId}/download`, {
    responseType: "blob",
  });
  return {
    blob: response.data,
    fileName: getDownloadFileName(response.headers["content-disposition"], title),
  };
}
