"use client";

import { ErrorState, PageLoadingState } from "@/components/shared/data-state";
import { DocumentEditor } from "@/features/documents/components/document-editor";
import {
  useCurrentTemplateVersion,
  useTemplateDetail,
  useTemplateVersionPlaceholders,
} from "@/features/templates/api/template-queries";
import { getApiErrorMessage } from "@/services/api-errors";

export function NewDocumentScreen({ templateId }: { templateId: string }) {
  const templateQuery = useTemplateDetail(templateId);
  const versionQuery = useCurrentTemplateVersion(templateId);
  const placeholdersQuery = useTemplateVersionPlaceholders(versionQuery.data?.id);

  if (templateQuery.isError || versionQuery.isError || placeholdersQuery.isError) {
    const error = templateQuery.error ?? versionQuery.error ?? placeholdersQuery.error;
    return (
      <ErrorState
        title="The document editor is unavailable"
        description={getApiErrorMessage(
          error,
          "We couldn’t prepare this template. Check your connection and try again.",
        )}
        onRetry={() => {
          void templateQuery.refetch();
          void versionQuery.refetch();
          void placeholdersQuery.refetch();
        }}
      />
    );
  }

  if (
    templateQuery.isPending ||
    versionQuery.isPending ||
    (Boolean(versionQuery.data) && placeholdersQuery.isPending)
  ) {
    return <PageLoadingState label="Preparing document editor" />;
  }

  return (
    <DocumentEditor
      template={{
        id: templateQuery.data.id,
        name: templateQuery.data.name,
        versionNumber: versionQuery.data.versionNumber,
        templateVersionId: versionQuery.data.id,
        content: versionQuery.data.content,
        contentFormat: versionQuery.data.contentFormat,
        placeholders: placeholdersQuery.data ?? [],
      }}
    />
  );
}
