"use client";

import { Search, ScrollText } from "lucide-react";
import { useMemo, useState } from "react";
import { Input } from "@/components/ui/input";
import {
  EmptyState,
  ErrorState,
  PageLoadingState,
} from "@/components/shared/data-state";
import { PageHeader } from "@/components/shared/page-header";
import { useAdminAuditLogs } from "@/features/admin/api/admin-queries";
import { AdminStatusBadge } from "@/features/admin/components/admin-status-badge";
import { getApiErrorMessage } from "@/services/api-errors";

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(value));
}

function formatAction(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

export function AdminAuditLogsScreen() {
  const logsQuery = useAdminAuditLogs();
  const [search, setSearch] = useState("");
  const [entityType, setEntityType] = useState("All");

  const entityTypes = useMemo(
    () =>
      Array.from(new Set((logsQuery.data ?? []).map((log) => log.entityType))).sort(),
    [logsQuery.data],
  );

  const filteredLogs = useMemo(() => {
    const term = search.trim().toLocaleLowerCase();
    return (logsQuery.data ?? []).filter((log) => {
      const matchesSearch =
        !term ||
        log.performedBy.fullName.toLocaleLowerCase().includes(term) ||
        log.performedBy.username.toLocaleLowerCase().includes(term) ||
        log.actionType.toLocaleLowerCase().includes(term) ||
        log.entityType.toLocaleLowerCase().includes(term) ||
        log.entityId.toLocaleLowerCase().includes(term) ||
        log.description.toLocaleLowerCase().includes(term);
      return matchesSearch && (entityType === "All" || log.entityType === entityType);
    });
  }, [entityType, logsQuery.data, search]);

  return (
    <div className="space-y-7">
      <PageHeader
        title="Audit logs"
        description="Review a read-only record of important administrative actions, including who performed each action and when it occurred."
      />

      {logsQuery.isPending ? <PageLoadingState label="Loading audit logs" /> : null}
      {logsQuery.isError ? (
        <ErrorState
          title="Audit logs are unavailable"
          description={getApiErrorMessage(
            logsQuery.error,
            "We couldn’t load the audit record. Check your connection and try again.",
          )}
          onRetry={() => logsQuery.refetch()}
        />
      ) : null}

      {logsQuery.data ? (
        <section
          aria-label="Audit log"
          className="overflow-hidden rounded-xl border bg-card"
        >
          <div className="flex flex-col gap-3 border-b p-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="relative w-full sm:max-w-md">
              <Search
                aria-hidden="true"
                className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
              />
              <Input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                className="pl-9"
                placeholder="Search actor, action, entity, or description"
                aria-label="Search audit logs"
              />
            </div>
            <label className="flex items-center justify-between gap-2 text-sm text-muted-foreground">
              <span>Entity</span>
              <select
                value={entityType}
                onChange={(event) => setEntityType(event.target.value)}
                className="h-9 rounded-lg border border-input bg-background px-3 text-sm text-foreground outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
              >
                <option>All</option>
                {entityTypes.map((type) => <option key={type}>{type}</option>)}
              </select>
            </label>
          </div>

          {filteredLogs.length === 0 ? (
            <div className="p-4">
              <EmptyState
                title={logsQuery.data.length === 0 ? "No audit entries yet" : "No audit entries match"}
                description={
                  logsQuery.data.length === 0
                    ? "Important administrative changes will appear here."
                    : "Adjust your search or entity filter."
                }
              />
            </div>
          ) : (
            <>
              <div className="hidden overflow-x-auto lg:block">
                <table className="w-full text-left text-sm">
                  <thead className="bg-muted/55 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
                    <tr>
                      <th scope="col" className="px-5 py-3">When</th>
                      <th scope="col" className="px-5 py-3">Performed by</th>
                      <th scope="col" className="px-5 py-3">Action</th>
                      <th scope="col" className="px-5 py-3">Entity</th>
                      <th scope="col" className="px-5 py-3">Description</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {filteredLogs.map((log) => (
                      <tr key={log.id} className="align-top hover:bg-muted/30">
                        <td className="whitespace-nowrap px-5 py-4 tabular-nums text-muted-foreground">
                          {formatDateTime(log.createdAt)}
                        </td>
                        <td className="px-5 py-4">
                          <p className="font-medium">{log.performedBy.fullName}</p>
                          <p className="mt-0.5 text-xs text-muted-foreground">@{log.performedBy.username}</p>
                        </td>
                        <td className="px-5 py-4">
                          <AdminStatusBadge label={formatAction(log.actionType)} tone="neutral" />
                        </td>
                        <td className="px-5 py-4">
                          <p className="font-medium">{log.entityType}</p>
                          <p className="mt-1 max-w-44 truncate font-mono text-xs text-muted-foreground" title={log.entityId}>
                            {log.entityId}
                          </p>
                        </td>
                        <td className="max-w-md px-5 py-4 leading-6 text-muted-foreground">
                          {log.description}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="divide-y lg:hidden">
                {filteredLogs.map((log) => (
                  <article key={log.id} className="space-y-4 p-4">
                    <div className="flex items-start gap-3">
                      <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                        <ScrollText aria-hidden="true" className="size-4" />
                      </span>
                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <h2 className="font-medium">{formatAction(log.actionType)}</h2>
                          <AdminStatusBadge label={log.entityType} tone="neutral" />
                        </div>
                        <p className="mt-1 text-xs tabular-nums text-muted-foreground">
                          {formatDateTime(log.createdAt)}
                        </p>
                      </div>
                    </div>
                    <p className="text-sm leading-6 text-muted-foreground">{log.description}</p>
                    <dl className="grid gap-3 text-sm sm:grid-cols-2">
                      <div>
                        <dt className="text-xs text-muted-foreground">Performed by</dt>
                        <dd className="mt-0.5 font-medium">
                          {log.performedBy.fullName} <span className="font-normal text-muted-foreground">@{log.performedBy.username}</span>
                        </dd>
                      </div>
                      <div>
                        <dt className="text-xs text-muted-foreground">Entity ID</dt>
                        <dd className="mt-0.5 truncate font-mono text-xs" title={log.entityId}>{log.entityId}</dd>
                      </div>
                    </dl>
                  </article>
                ))}
              </div>
            </>
          )}
        </section>
      ) : null}
    </div>
  );
}
