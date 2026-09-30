"use client";

import Link from "next/link";
import { CheckCircle2, ExternalLink, Plus, Search } from "lucide-react";
import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { EmptyState, ErrorState, PageLoadingState } from "@/components/shared/data-state";
import { PageHeader } from "@/components/shared/page-header";
import {
  useAdminCategories,
  useAdminTemplates,
  useCreateTemplate,
  useSetTemplateActive,
} from "@/features/admin/api/admin-queries";
import { TemplateFormDialog } from "@/features/admin/components/admin-form-dialogs";
import { AdminStatusBadge } from "@/features/admin/components/admin-status-badge";
import { cn } from "@/lib/utils";
import { getApiErrorMessage } from "@/services/api-errors";
import type { AdminTemplateSummaryDto, TemplateStatus } from "@/types/api";

type StatusFilter = "All" | TemplateStatus;

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  }).format(new Date(value));
}

function statusTone(status: TemplateStatus) {
  if (status === "Active") return "positive" as const;
  if (status === "Draft") return "warning" as const;
  return "neutral" as const;
}

export function AdminTemplatesScreen() {
  const router = useRouter();
  const templatesQuery = useAdminTemplates();
  const categoriesQuery = useAdminCategories();
  const createMutation = useCreateTemplate();
  const stateMutation = useSetTemplateActive();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<StatusFilter>("All");
  const [categoryId, setCategoryId] = useState("All");
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const categories = useMemo(() => categoriesQuery.data ?? [], [categoriesQuery.data]);
  const initialValues = useMemo(
    () => ({ name: "", categoryId: categories[0]?.id ?? "" }),
    [categories],
  );
  const filteredTemplates = useMemo(() => {
    const term = search.trim().toLocaleLowerCase();
    return (templatesQuery.data ?? []).filter((template) => {
      const matchesSearch = !term || template.name.toLocaleLowerCase().includes(term);
      const matchesStatus = status === "All" || template.status === status;
      const matchesCategory = categoryId === "All" || template.category.id === categoryId;
      return matchesSearch && matchesStatus && matchesCategory;
    });
  }, [categoryId, search, status, templatesQuery.data]);

  function openCreate() {
    createMutation.reset();
    setDialogOpen(true);
  }

  async function create(values: { name: string; categoryId: string }) {
    const result = await createMutation.mutateAsync(values);
    setDialogOpen(false);
    router.push(`/admin/templates/${result.id}`);
  }

  async function changeState(template: AdminTemplateSummaryDto) {
    const active = template.status !== "Active";
    const result = await stateMutation.mutateAsync({ id: template.id, active });
    setSuccessMessage(`Template “${result.name}” is now ${active ? "active" : "inactive"}.`);
  }

  return (
    <div className="space-y-7">
      <PageHeader
        title="Template management"
        description="Create template records, review their version state, and control availability. Content, publishing, and placeholders are managed in later phases."
        actions={
          <Button type="button" onClick={openCreate} disabled={categoriesQuery.isPending || categories.length === 0}>
            <Plus aria-hidden="true" />Create template
          </Button>
        }
      />

      {successMessage ? (
        <Alert role="status">
          <CheckCircle2 aria-hidden="true" />
          <AlertTitle>Changes saved</AlertTitle>
          <AlertDescription>{successMessage}</AlertDescription>
        </Alert>
      ) : null}

      {stateMutation.isError ? (
        <Alert variant="destructive" role="alert">
          <AlertTitle>Template status could not be changed</AlertTitle>
          <AlertDescription>
            {getApiErrorMessage(stateMutation.error, "Review the template version state and try again.")}
          </AlertDescription>
        </Alert>
      ) : null}

      {categoriesQuery.data?.length === 0 ? (
        <Alert>
          <AlertTitle>A category is required</AlertTitle>
          <AlertDescription>
            <Link href="/admin/categories" className="font-medium underline">Create a category</Link> before creating a template.
          </AlertDescription>
        </Alert>
      ) : null}

      {categoriesQuery.isError ? (
        <Alert variant="destructive" role="alert">
          <AlertTitle>Categories are unavailable</AlertTitle>
          <AlertDescription>
            {getApiErrorMessage(categoriesQuery.error, "Templates can be reviewed, but creation is unavailable until categories load.")}
          </AlertDescription>
        </Alert>
      ) : null}

      {templatesQuery.isPending ? <PageLoadingState label="Loading managed templates" /> : null}
      {templatesQuery.isError ? (
        <ErrorState
          title="Templates are unavailable"
          description={getApiErrorMessage(templatesQuery.error, "We couldn’t load managed templates. Check your connection and try again.")}
          onRetry={() => templatesQuery.refetch()}
        />
      ) : null}

      {templatesQuery.data ? (
        <section aria-label="Template management" className="overflow-hidden rounded-xl border bg-card">
          <div className="grid gap-3 border-b p-4 lg:grid-cols-[minmax(16rem,1fr)_auto_auto] lg:items-center">
            <div className="relative">
              <Search aria-hidden="true" className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input value={search} onChange={(event) => setSearch(event.target.value)} className="pl-9" placeholder="Search templates" aria-label="Search templates" />
            </div>
            <label className="flex items-center gap-2 text-sm text-muted-foreground">
              <span>Status</span>
              <select value={status} onChange={(event) => setStatus(event.target.value as StatusFilter)} className="h-9 rounded-lg border border-input bg-background px-3 text-sm text-foreground outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50">
                <option>All</option>
                <option>Draft</option>
                <option>Active</option>
                <option>Inactive</option>
              </select>
            </label>
            <label className="flex items-center gap-2 text-sm text-muted-foreground">
              <span>Category</span>
              <select value={categoryId} onChange={(event) => setCategoryId(event.target.value)} className="h-9 max-w-56 rounded-lg border border-input bg-background px-3 text-sm text-foreground outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50">
                <option value="All">All categories</option>
                {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
              </select>
            </label>
          </div>

          {filteredTemplates.length === 0 ? (
            <div className="p-4">
              <EmptyState
                title={templatesQuery.data.length === 0 ? "No templates yet" : "No templates match"}
                description={templatesQuery.data.length === 0 ? "Create a template to begin its Draft version." : "Adjust your search or filters."}
              />
            </div>
          ) : (
            <>
              <div className="hidden overflow-x-auto md:block">
                <table className="w-full text-left text-sm">
                  <thead className="bg-muted/55 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
                    <tr>
                      <th scope="col" className="px-5 py-3">Template</th>
                      <th scope="col" className="px-5 py-3">Category</th>
                      <th scope="col" className="px-5 py-3">Status</th>
                      <th scope="col" className="px-5 py-3">Versions</th>
                      <th scope="col" className="px-5 py-3 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {filteredTemplates.map((template) => (
                      <tr key={template.id} className="hover:bg-muted/30">
                        <td className="px-5 py-4">
                          <Link href={`/admin/templates/${template.id}`} className="font-medium hover:underline">{template.name}</Link>
                          <p className="mt-1 text-xs tabular-nums text-muted-foreground">Created {formatDate(template.createdAt)}</p>
                        </td>
                        <td className="px-5 py-4 text-muted-foreground">{template.category.name}</td>
                        <td className="px-5 py-4"><AdminStatusBadge label={template.status} tone={statusTone(template.status)} /></td>
                        <td className="px-5 py-4 tabular-nums text-muted-foreground">
                          {template.versionCount} · {template.currentVersionNumber ? `Current v${template.currentVersionNumber}` : "No current version"}
                        </td>
                        <td className="px-5 py-4">
                          <div className="flex justify-end gap-2">
                            <Link href={`/admin/templates/${template.id}`} className={cn(buttonVariants({ variant: "ghost", size: "sm" }))}>
                              <ExternalLink aria-hidden="true" />View
                            </Link>
                            <Button
                              type="button"
                              size="sm"
                              variant="outline"
                              disabled={stateMutation.isPending || (template.status !== "Active" && !template.currentVersionNumber)}
                              title={template.status !== "Active" && !template.currentVersionNumber ? "A current Published version is required before activation." : undefined}
                              onClick={() => changeState(template)}
                            >
                              {template.status === "Active" ? "Deactivate" : "Activate"}
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="divide-y md:hidden">
                {filteredTemplates.map((template) => (
                  <article key={template.id} className="space-y-4 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <h2 className="truncate font-medium">{template.name}</h2>
                        <p className="mt-1 text-sm text-muted-foreground">{template.category.name}</p>
                      </div>
                      <AdminStatusBadge label={template.status} tone={statusTone(template.status)} />
                    </div>
                    <p className="text-xs tabular-nums text-muted-foreground">
                      {template.versionCount} {template.versionCount === 1 ? "version" : "versions"} · {template.currentVersionNumber ? `Current v${template.currentVersionNumber}` : "No current version"}
                    </p>
                    <div className="flex gap-2">
                      <Link href={`/admin/templates/${template.id}`} className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>View details</Link>
                      <Button type="button" size="sm" variant="outline" disabled={stateMutation.isPending || (template.status !== "Active" && !template.currentVersionNumber)} onClick={() => changeState(template)}>
                        {template.status === "Active" ? "Deactivate" : "Activate"}
                      </Button>
                    </div>
                  </article>
                ))}
              </div>
            </>
          )}
        </section>
      ) : null}

      <TemplateFormDialog
        open={dialogOpen}
        title="Create template"
        description="Creates a Draft template with an empty initial HTML version. Content and publishing are handled separately."
        submitLabel="Create template"
        categories={categories}
        initialValues={initialValues}
        isPending={createMutation.isPending}
        error={createMutation.error}
        onOpenChange={setDialogOpen}
        onSubmit={create}
      />
    </div>
  );
}
