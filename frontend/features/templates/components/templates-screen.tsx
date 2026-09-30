"use client";

import {
  EmptyState,
  ErrorState,
  PageLoadingState,
} from "@/components/shared/data-state";
import { PageHeader } from "@/components/shared/page-header";
import { useActiveTemplates } from "@/features/templates/api/template-queries";
import { TemplateGallery } from "@/features/templates/components/template-gallery";
import { getApiErrorMessage } from "@/services/api-errors";

export function TemplatesScreen() {
  const templatesQuery = useActiveTemplates();

  return (
    <div className="space-y-7">
      <PageHeader
        title="Templates"
        description="Choose an active template to start a new document. You can review its fields and source version before you begin."
      />
      {templatesQuery.isPending ? (
        <PageLoadingState label="Loading templates" />
      ) : null}
      {templatesQuery.isError ? (
        <ErrorState
          title="Templates are unavailable"
          description={getApiErrorMessage(
            templatesQuery.error,
            "We couldn’t load the template library. Check your connection and try again.",
          )}
          onRetry={() => templatesQuery.refetch()}
        />
      ) : null}
      {templatesQuery.data?.length === 0 ? (
        <EmptyState
          title="No active templates yet"
          description="Active templates will appear here when they are available."
        />
      ) : null}
      {templatesQuery.data && templatesQuery.data.length > 0 ? (
        <TemplateGallery templates={templatesQuery.data} />
      ) : null}
    </div>
  );
}
