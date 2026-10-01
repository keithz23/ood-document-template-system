"use client";

import { CheckCircle2, Pencil, Plus, Search } from "lucide-react";
import { useMemo, useState } from "react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  EmptyState,
  ErrorState,
  PageLoadingState,
} from "@/components/shared/data-state";
import { PageHeader } from "@/components/shared/page-header";
import {
  useAdminCategories,
  useCreateCategory,
  useSetCategoryActive,
  useUpdateCategory,
} from "@/features/admin/api/admin-queries";
import { CategoryFormDialog } from "@/features/admin/components/admin-form-dialogs";
import { AdminStatusBadge } from "@/features/admin/components/admin-status-badge";
import { getApiErrorMessage } from "@/services/api-errors";
import type { AdminCategoryDto } from "@/types/api";

type StatusFilter = "All" | "Active" | "Inactive";

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  }).format(new Date(value));
}

export function AdminCategoriesScreen() {
  const categoriesQuery = useAdminCategories();
  const createMutation = useCreateCategory();
  const updateMutation = useUpdateCategory();
  const stateMutation = useSetCategoryActive();
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<StatusFilter>("All");
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<AdminCategoryDto | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const filteredCategories = useMemo(() => {
    const term = search.trim().toLocaleLowerCase();
    return (categoriesQuery.data ?? []).filter((category) => {
      const matchesSearch =
        !term || category.name.toLocaleLowerCase().includes(term);
      const matchesStatus =
        status === "All" ||
        (status === "Active" ? category.isActive : !category.isActive);
      return matchesSearch && matchesStatus;
    });
  }, [categoriesQuery.data, search, status]);

  function openCreate() {
    setEditing(null);
    createMutation.reset();
    updateMutation.reset();
    setDialogOpen(true);
  }

  function openEdit(category: AdminCategoryDto) {
    setEditing(category);
    createMutation.reset();
    updateMutation.reset();
    setDialogOpen(true);
  }

  async function saveCategory(values: { name: string }) {
    if (editing) {
      const result = await updateMutation.mutateAsync({
        id: editing.id,
        request: values,
      });
      setSuccessMessage(`Category “${result.name}” was updated.`);
    } else {
      const result = await createMutation.mutateAsync(values);
      setSuccessMessage(`Category “${result.name}” was created.`);
    }
    setDialogOpen(false);
  }

  async function changeState(category: AdminCategoryDto) {
    const active = !category.isActive;
    const result = await stateMutation.mutateAsync({ id: category.id, active });
    setSuccessMessage(
      `Category “${result.name}” is now ${active ? "active" : "inactive"}.`,
    );
  }

  return (
    <div className="space-y-7">
      <PageHeader
        title="Categories"
        description="Organize templates and control which categories remain available for administration. Deactivation keeps existing templates and documents intact."
        actions={
          <Button type="button" onClick={openCreate}>
            <Plus aria-hidden="true" />
            Create category
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
          <AlertTitle>Category status could not be changed</AlertTitle>
          <AlertDescription>
            {getApiErrorMessage(stateMutation.error, "Try the action again.")}
          </AlertDescription>
        </Alert>
      ) : null}

      {categoriesQuery.isPending ? (
        <PageLoadingState label="Loading categories" />
      ) : null}
      {categoriesQuery.isError ? (
        <ErrorState
          title="Categories are unavailable"
          description={getApiErrorMessage(
            categoriesQuery.error,
            "We couldn’t load categories. Check your connection and try again.",
          )}
          onRetry={() => categoriesQuery.refetch()}
        />
      ) : null}

      {categoriesQuery.data ? (
        <section
          aria-label="Category management"
          className="overflow-hidden rounded-xl border bg-card"
        >
          <div className="flex flex-col gap-3 border-b p-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="relative w-full sm:max-w-sm">
              <Search
                aria-hidden="true"
                className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
              />
              <Input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                className="pl-9"
                placeholder="Search categories"
                aria-label="Search categories"
              />
            </div>
            <label className="flex items-center gap-2 text-sm text-muted-foreground">
              <span>Status</span>
              <select
                value={status}
                onChange={(event) =>
                  setStatus(event.target.value as StatusFilter)
                }
                className="h-9 rounded-lg border border-input bg-background px-3 text-sm text-foreground outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
              >
                <option>All</option>
                <option>Active</option>
                <option>Inactive</option>
              </select>
            </label>
          </div>

          {filteredCategories.length === 0 ? (
            <div className="p-4">
              <EmptyState
                title={
                  categoriesQuery.data.length === 0
                    ? "No categories yet"
                    : "No categories match"
                }
                description={
                  categoriesQuery.data.length === 0
                    ? "Create a category before adding templates."
                    : "Adjust your search or status filter."
                }
              />
            </div>
          ) : (
            <>
              <div className="hidden overflow-x-auto md:block">
                <table className="w-full text-left text-sm">
                  <thead className="bg-muted/55 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
                    <tr>
                      <th scope="col" className="px-5 py-3">
                        Name
                      </th>
                      <th scope="col" className="px-5 py-3">
                        Status
                      </th>
                      <th scope="col" className="px-5 py-3">
                        Created
                      </th>
                      <th scope="col" className="px-5 py-3 text-right">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {filteredCategories.map((category) => (
                      <tr key={category.id} className="hover:bg-muted/30">
                        <td className="px-5 py-4 font-medium">
                          {category.name}
                        </td>
                        <td className="px-5 py-4">
                          <AdminStatusBadge
                            label={category.isActive ? "Active" : "Inactive"}
                            tone={category.isActive ? "positive" : "neutral"}
                          />
                        </td>
                        <td className="px-5 py-4 tabular-nums text-muted-foreground">
                          {formatDate(category.createdAt)}
                        </td>
                        <td className="px-5 py-4">
                          <div className="flex justify-end gap-2">
                            <Button
                              type="button"
                              size="sm"
                              variant="ghost"
                              onClick={() => openEdit(category)}
                            >
                              <Pencil aria-hidden="true" />
                              Edit
                            </Button>
                            <Button
                              type="button"
                              size="sm"
                              variant="outline"
                              disabled={stateMutation.isPending}
                              onClick={() => changeState(category)}
                            >
                              {category.isActive ? "Deactivate" : "Activate"}
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="divide-y md:hidden">
                {filteredCategories.map((category) => (
                  <article key={category.id} className="space-y-4 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <h2 className="font-medium">{category.name}</h2>
                        <p className="mt-1 text-xs tabular-nums text-muted-foreground">
                          Created {formatDate(category.createdAt)}
                        </p>
                      </div>
                      <AdminStatusBadge
                        label={category.isActive ? "Active" : "Inactive"}
                        tone={category.isActive ? "positive" : "neutral"}
                      />
                    </div>
                    <div className="flex gap-2">
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        onClick={() => openEdit(category)}
                      >
                        <Pencil aria-hidden="true" />
                        Edit
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        disabled={stateMutation.isPending}
                        onClick={() => changeState(category)}
                      >
                        {category.isActive ? "Deactivate" : "Activate"}
                      </Button>
                    </div>
                  </article>
                ))}
              </div>
            </>
          )}
        </section>
      ) : null}

      <CategoryFormDialog
        open={dialogOpen}
        initialName={editing?.name}
        isPending={createMutation.isPending || updateMutation.isPending}
        error={editing ? updateMutation.error : createMutation.error}
        onOpenChange={setDialogOpen}
        onSubmit={saveCategory}
      />
    </div>
  );
}
