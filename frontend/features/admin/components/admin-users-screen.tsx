"use client";

import Link from "next/link";
import { CheckCircle2, Search, UserRound } from "lucide-react";
import { useMemo, useState } from "react";
import { buttonVariants } from "@/components/ui/button";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Input } from "@/components/ui/input";
import {
  EmptyState,
  ErrorState,
  PageLoadingState,
} from "@/components/shared/data-state";
import { PageHeader } from "@/components/shared/page-header";
import { useAdminUsers } from "@/features/admin/api/admin-queries";
import { AdminStatusBadge } from "@/features/admin/components/admin-status-badge";
import { CreateAdminUserDialog } from "@/features/admin/components/create-admin-user-dialog";
import { useAuth } from "@/features/auth/auth-provider";
import { cn } from "@/lib/utils";
import { getApiErrorMessage } from "@/services/api-errors";

type RoleFilter = "All" | "Admin" | "User";
type StatusFilter = "All" | "Active" | "Inactive";

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  }).format(new Date(value));
}

export function AdminUsersScreen() {
  const { hasPermission } = useAuth();
  const usersQuery = useAdminUsers();
  const [search, setSearch] = useState("");
  const [role, setRole] = useState<RoleFilter>("All");
  const [status, setStatus] = useState<StatusFilter>("All");
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const filteredUsers = useMemo(() => {
    const term = search.trim().toLocaleLowerCase();
    return (usersQuery.data ?? []).filter((user) => {
      const matchesSearch =
        !term ||
        user.username.toLocaleLowerCase().includes(term) ||
        user.fullName.toLocaleLowerCase().includes(term) ||
        user.email.toLocaleLowerCase().includes(term);
      const matchesRole = role === "All" || user.role === role;
      const matchesStatus =
        status === "All" ||
        (status === "Active" ? user.isActive : !user.isActive);
      return matchesSearch && matchesRole && matchesStatus;
    });
  }, [role, search, status, usersQuery.data]);

  return (
    <div className="space-y-7">
      <PageHeader
        title="Users"
        description="Review account access, activity status, and assigned roles. User records remain available to preserve document and audit history."
        actions={hasPermission("Users.Manage") ? (
          <CreateAdminUserDialog
            onCreated={(user) => setSuccessMessage(`${user.fullName} was created and can sign in.`)}
          />
        ) : undefined}
      />

      {successMessage ? (
        <Alert role="status">
          <CheckCircle2 aria-hidden="true" />
          <AlertTitle>User created</AlertTitle>
          <AlertDescription>{successMessage}</AlertDescription>
        </Alert>
      ) : null}

      {usersQuery.isPending ? <PageLoadingState label="Loading users" /> : null}
      {usersQuery.isError ? (
        <ErrorState
          title="Users are unavailable"
          description={getApiErrorMessage(
            usersQuery.error,
            "We couldn’t load users. Check your connection and try again.",
          )}
          onRetry={() => usersQuery.refetch()}
        />
      ) : null}

      {usersQuery.data ? (
        <section
          aria-label="User management"
          className="overflow-hidden rounded-xl border bg-card"
        >
          <div className="flex flex-col gap-3 border-b p-4 lg:flex-row lg:items-center lg:justify-between">
            <div className="relative w-full lg:max-w-sm">
              <Search
                aria-hidden="true"
                className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
              />
              <Input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                className="pl-9"
                placeholder="Search name, username, or email"
                aria-label="Search users"
              />
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <label className="flex items-center justify-between gap-2 text-sm text-muted-foreground">
                <span>Role</span>
                <select
                  value={role}
                  onChange={(event) => setRole(event.target.value as RoleFilter)}
                  className="h-9 rounded-lg border border-input bg-background px-3 text-sm text-foreground outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                >
                  <option>All</option>
                  <option>Admin</option>
                  <option>User</option>
                </select>
              </label>
              <label className="flex items-center justify-between gap-2 text-sm text-muted-foreground">
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
          </div>

          {filteredUsers.length === 0 ? (
            <div className="p-4">
              <EmptyState
                title={usersQuery.data.length === 0 ? "No users yet" : "No users match"}
                description={
                  usersQuery.data.length === 0
                    ? "User accounts will appear here when they are available."
                    : "Adjust your search, role, or status filter."
                }
              />
            </div>
          ) : (
            <>
              <div className="hidden overflow-x-auto md:block">
                <table className="w-full text-left text-sm">
                  <thead className="bg-muted/55 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
                    <tr>
                      <th scope="col" className="px-5 py-3">User</th>
                      <th scope="col" className="px-5 py-3">Role</th>
                      <th scope="col" className="px-5 py-3">Status</th>
                      <th scope="col" className="px-5 py-3">Created</th>
                      <th scope="col" className="px-5 py-3 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {filteredUsers.map((user) => (
                      <tr key={user.id} className="hover:bg-muted/30">
                        <td className="px-5 py-4">
                          <div className="flex items-center gap-3">
                            <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                              <UserRound aria-hidden="true" className="size-4" />
                            </span>
                            <div className="min-w-0">
                              <p className="font-medium">{user.fullName}</p>
                              <p className="text-xs text-muted-foreground">
                                @{user.username} · {user.email}
                              </p>
                            </div>
                          </div>
                        </td>
                        <td className="px-5 py-4">
                          <AdminStatusBadge
                            label={user.role}
                            tone={user.role === "Admin" ? "warning" : "neutral"}
                          />
                        </td>
                        <td className="px-5 py-4">
                          <AdminStatusBadge
                            label={user.isActive ? "Active" : "Inactive"}
                            tone={user.isActive ? "positive" : "neutral"}
                          />
                        </td>
                        <td className="px-5 py-4 tabular-nums text-muted-foreground">
                          {formatDate(user.createdAt)}
                        </td>
                        <td className="px-5 py-4 text-right">
                          <Link
                            href={`/admin/users/${user.id}`}
                            className={cn(buttonVariants({ size: "sm", variant: "outline" }))}
                          >
                            View user
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="divide-y md:hidden">
                {filteredUsers.map((user) => (
                  <article key={user.id} className="space-y-4 p-4">
                    <div className="flex items-start gap-3">
                      <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                        <UserRound aria-hidden="true" className="size-4" />
                      </span>
                      <div className="min-w-0 flex-1">
                        <h2 className="font-medium">{user.fullName}</h2>
                        <p className="truncate text-sm text-muted-foreground">{user.email}</p>
                        <p className="mt-1 text-xs tabular-nums text-muted-foreground">
                          @{user.username} · Created {formatDate(user.createdAt)}
                        </p>
                      </div>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <AdminStatusBadge
                        label={user.role}
                        tone={user.role === "Admin" ? "warning" : "neutral"}
                      />
                      <AdminStatusBadge
                        label={user.isActive ? "Active" : "Inactive"}
                        tone={user.isActive ? "positive" : "neutral"}
                      />
                    </div>
                    <Link
                      href={`/admin/users/${user.id}`}
                      className={cn(buttonVariants({ size: "sm", variant: "outline" }), "w-full")}
                    >
                      View user
                    </Link>
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
