"use client";

import Link from "next/link";
import { ArrowLeft, CheckCircle2, ShieldCheck, UserRound } from "lucide-react";
import { useState } from "react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { ErrorState, PageLoadingState } from "@/components/shared/data-state";
import { PageHeader } from "@/components/shared/page-header";
import {
  useAdminUser,
  useSetUserActive,
  useUpdateUserRole,
} from "@/features/admin/api/admin-queries";
import { AdminStatusBadge } from "@/features/admin/components/admin-status-badge";
import { useAuth } from "@/features/auth/auth-provider";
import { cn } from "@/lib/utils";
import { getApiErrorMessage } from "@/services/api-errors";
import type { UserRole } from "@/types/api";

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "long",
    day: "numeric",
  }).format(new Date(value));
}

export function AdminUserDetailScreen({ userId }: { userId: string }) {
  const { session, hasPermission } = useAuth();
  const userQuery = useAdminUser(userId);
  const stateMutation = useSetUserActive();
  const roleMutation = useUpdateUserRole();
  const [selectedRole, setSelectedRole] = useState<UserRole | null>(null);
  const [confirmStateOpen, setConfirmStateOpen] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  if (userQuery.isPending) return <PageLoadingState label="Loading user details" />;
  if (userQuery.isError || !userQuery.data) {
    return (
      <ErrorState
        title="User details are unavailable"
        description={getApiErrorMessage(
          userQuery.error,
          "This user could not be loaded. Return to the user list and try again.",
        )}
        onRetry={() => userQuery.refetch()}
      />
    );
  }

  const user = userQuery.data;
  const isSelf = session?.user.id === user.id;
  const canManageUsers = hasPermission("Users.Manage");
  const effectiveRole = selectedRole ?? user.role;
  const roleChanged = effectiveRole !== user.role;

  async function saveRole() {
    const result = await roleMutation.mutateAsync({ id: user.id, role: effectiveRole });
    setSelectedRole(null);
    setSuccessMessage(`${result.fullName} now has the ${result.role} role.`);
  }

  async function changeState() {
    const active = !user.isActive;
    const result = await stateMutation.mutateAsync({ id: user.id, active });
    setSuccessMessage(`${result.fullName} is now ${active ? "active" : "inactive"}.`);
    setConfirmStateOpen(false);
  }

  const mutationError = roleMutation.error ?? stateMutation.error;

  return (
    <div className="space-y-7">
      <Link
        href="/admin/users"
        className={cn(buttonVariants({ variant: "ghost", size: "sm" }), "-ml-2")}
      >
        <ArrowLeft aria-hidden="true" />
        Back to users
      </Link>

      <PageHeader
        title={user.fullName}
        description="Review this account’s identity and control its application access. Historical records remain unchanged."
      />

      {successMessage ? (
        <Alert role="status">
          <CheckCircle2 aria-hidden="true" />
          <AlertTitle>Changes saved</AlertTitle>
          <AlertDescription>{successMessage}</AlertDescription>
        </Alert>
      ) : null}

      {mutationError ? (
        <Alert variant="destructive" role="alert">
          <AlertTitle>User could not be updated</AlertTitle>
          <AlertDescription>
            {getApiErrorMessage(mutationError, "Review the selection and try again.")}
          </AlertDescription>
        </Alert>
      ) : null}

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <section aria-labelledby="identity-heading" className="rounded-xl border bg-card p-5 sm:p-6">
          <div className="flex items-start gap-4">
            <span className="flex size-11 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground">
              <UserRound aria-hidden="true" className="size-5" />
            </span>
            <div>
              <h2 id="identity-heading" className="text-base font-semibold">Account details</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                Identity fields are read-only in this phase.
              </p>
            </div>
          </div>

          <dl className="mt-6 grid gap-x-8 gap-y-5 sm:grid-cols-2">
            <div>
              <dt className="text-xs font-medium uppercase tracking-[0.06em] text-muted-foreground">Full name</dt>
              <dd className="mt-1 text-sm font-medium">{user.fullName}</dd>
            </div>
            <div>
              <dt className="text-xs font-medium uppercase tracking-[0.06em] text-muted-foreground">Username</dt>
              <dd className="mt-1 text-sm">@{user.username}</dd>
            </div>
            <div>
              <dt className="text-xs font-medium uppercase tracking-[0.06em] text-muted-foreground">Email</dt>
              <dd className="mt-1 break-all text-sm">{user.email}</dd>
            </div>
            <div>
              <dt className="text-xs font-medium uppercase tracking-[0.06em] text-muted-foreground">Created</dt>
              <dd className="mt-1 text-sm tabular-nums">{formatDate(user.createdAt)}</dd>
            </div>
            <div>
              <dt className="text-xs font-medium uppercase tracking-[0.06em] text-muted-foreground">Role</dt>
              <dd className="mt-1">
                <AdminStatusBadge label={user.role} tone={user.role === "Admin" ? "warning" : "neutral"} />
              </dd>
            </div>
            <div>
              <dt className="text-xs font-medium uppercase tracking-[0.06em] text-muted-foreground">Status</dt>
              <dd className="mt-1">
                <AdminStatusBadge label={user.isActive ? "Active" : "Inactive"} tone={user.isActive ? "positive" : "neutral"} />
              </dd>
            </div>
          </dl>
        </section>

        <section aria-labelledby="access-heading" className="rounded-xl border bg-card p-5 sm:p-6">
          <div className="flex items-center gap-3">
            <span className="flex size-9 items-center justify-center rounded-lg bg-muted text-muted-foreground">
              <ShieldCheck aria-hidden="true" className="size-4" />
            </span>
            <div>
              <h2 id="access-heading" className="font-semibold">Access controls</h2>
              <p className="mt-0.5 text-xs text-muted-foreground">Role and sign-in availability</p>
            </div>
          </div>

          <div className="mt-6 space-y-6">
            <div className="space-y-2">
              <Label htmlFor="user-role">Role</Label>
              <select
                id="user-role"
                value={effectiveRole}
                onChange={(event) => setSelectedRole(event.target.value as UserRole)}
                disabled={!canManageUsers || isSelf || roleMutation.isPending}
                className="h-10 w-full rounded-lg border border-input bg-background px-3 text-sm text-foreground outline-none disabled:cursor-not-allowed disabled:opacity-50 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
              >
                <option value="Admin">Admin</option>
                <option value="User">User</option>
              </select>
              <p className="text-xs leading-5 text-muted-foreground">
                {isSelf
                  ? "You cannot change your own role while signed in."
                  : "Admins can access administration tools; Users use author workflows."}
              </p>
              <Button
                type="button"
                className="w-full"
                disabled={!canManageUsers || isSelf || !roleChanged || roleMutation.isPending}
                onClick={saveRole}
              >
                {roleMutation.isPending ? "Saving role…" : "Save role"}
              </Button>
            </div>

            <div className="border-t pt-5">
              <p className="text-sm font-medium">Account status</p>
              <p className="mt-1 text-xs leading-5 text-muted-foreground">
                {user.isActive
                  ? "Deactivation prevents future sign-ins without deleting history."
                  : "Activation restores the user’s ability to sign in."}
              </p>
              <Button
                type="button"
                variant={user.isActive ? "outline" : "default"}
                className="mt-3 w-full"
                disabled={!canManageUsers || isSelf || stateMutation.isPending}
                onClick={() => setConfirmStateOpen(true)}
              >
                {user.isActive ? "Deactivate account" : "Activate account"}
              </Button>
              {isSelf ? (
                <p className="mt-2 text-xs text-muted-foreground">
                  You cannot deactivate your own account.
                </p>
              ) : null}
            </div>
          </div>
        </section>
      </div>

      <Dialog open={confirmStateOpen} onOpenChange={setConfirmStateOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {user.isActive ? "Deactivate account?" : "Activate account?"}
            </DialogTitle>
            <DialogDescription>
              {user.isActive
                ? `${user.fullName} will no longer be able to sign in. Their documents and audit history will remain available.`
                : `${user.fullName} will be able to sign in again with their existing credentials.`}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose render={<Button type="button" variant="outline" />}>Cancel</DialogClose>
            <Button
              type="button"
              variant={user.isActive ? "destructive" : "default"}
              disabled={stateMutation.isPending}
              onClick={changeState}
            >
              {stateMutation.isPending
                ? "Saving…"
                : user.isActive
                  ? "Deactivate"
                  : "Activate"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
