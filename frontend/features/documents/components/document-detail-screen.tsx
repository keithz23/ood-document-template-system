"use client";

import { ErrorState, PageLoadingState } from "@/components/shared/data-state";
import { useDocument } from "@/features/documents/api/document-queries";
import { DocumentEditor, type EditorPlaceholder } from "@/features/documents/components/document-editor";
import { useTemplateVersionPlaceholders } from "@/features/templates/api/template-queries";
import { getApiErrorMessage } from "@/services/api-errors";

export function DocumentDetailScreen({ documentId }: { documentId: string }) {
  const documentQuery = useDocument(documentId);
  const placeholdersQuery = useTemplateVersionPlaceholders(
    documentQuery.data?.source.templateVersionId,
  );

  if (documentQuery.isPending) {
    return <PageLoadingState label="Loading document" />;
  }

  if (documentQuery.isError) {
    return (
      <ErrorState
        title="The document is unavailable"
        description={getApiErrorMessage(
          documentQuery.error,
          "We couldn’t load this document. Check your access and connection, then try again.",
        )}
        onRetry={() => documentQuery.refetch()}
      />
    );
  }

  const document = documentQuery.data;
  const savedPlaceholders: EditorPlaceholder[] = document.placeholderValues.map((value) => ({
    id: value.placeholderId,
    key: value.placeholderKeySnapshot,
    label: value.labelSnapshot,
    dataType: value.dataTypeSnapshot,
    isRequired: false,
  }));
  const placeholders: readonly EditorPlaceholder[] = placeholdersQuery.data?.map((placeholder) => ({
    id: placeholder.id,
    key: placeholder.key,
    label: placeholder.label,
    dataType: placeholder.dataType,
    isRequired: placeholder.isRequired,
    defaultValue: placeholder.defaultValue,
  })) ?? savedPlaceholders;

  return (
    <DocumentEditor
      document={document}
      template={{
        id: document.source.templateId,
        name: document.source.templateName,
        versionNumber: document.source.versionNumber,
        templateVersionId: document.source.templateVersionId,
        content: document.content,
        contentFormat: document.contentFormat,
        placeholders,
      }}
    />
  );
}
