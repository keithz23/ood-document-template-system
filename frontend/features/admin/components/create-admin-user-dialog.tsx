"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { LoaderCircle, UserPlus } from "lucide-react";
import { useState, type ReactNode } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useCreateAdminUser } from "@/features/admin/api/admin-queries";
import { getApiErrorMessage } from "@/services/api-errors";
import type { AdminUserDto, UserRole } from "@/types/api";

const createUserSchema = z.object({
  username: z.string().trim().min(1, "Enter a username."),
  fullName: z.string().trim().min(1, "Enter the user’s full name."),
  email: z.email("Enter a valid email address."),
  initialPassword: z.string().min(1, "Enter an initial password."),
  role: z.enum(["Admin", "User"]),
});

type CreateUserValues = z.infer<typeof createUserSchema>;

export function CreateAdminUserDialog({
  onCreated,
}: {
  onCreated: (user: AdminUserDto) => void;
}) {
  const [open, setOpen] = useState(false);
  const createMutation = useCreateAdminUser();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreateUserValues>({
    resolver: zodResolver(createUserSchema),
    defaultValues: {
      username: "",
      fullName: "",
      email: "",
      initialPassword: "",
      role: "User",
    },
  });

  const submit = handleSubmit((values) => {
    createMutation.mutate(values, {
      onSuccess: (user) => {
        onCreated(user);
        reset();
        setOpen(false);
      },
    });
  });

  function setDialogOpen(nextOpen: boolean) {
    if (!nextOpen && createMutation.isPending) return;
    setOpen(nextOpen);
    if (!nextOpen) createMutation.reset();
  }

  return (
    <Dialog open={open} onOpenChange={setDialogOpen}>
      <DialogTrigger render={<Button type="button" />}>
        <UserPlus aria-hidden="true" />
        Create user
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create user</DialogTitle>
          <DialogDescription>
            Create an active account with an initial role and password.
          </DialogDescription>
        </DialogHeader>

        {createMutation.isError ? (
          <Alert variant="destructive" role="alert">
            <AlertTitle>User could not be created</AlertTitle>
            <AlertDescription>
              {getApiErrorMessage(
                createMutation.error,
                "Review the account details and try again.",
              )}
            </AlertDescription>
          </Alert>
        ) : null}

        <form id="create-admin-user-form" className="space-y-4" onSubmit={submit} noValidate>
          <Field
            id="admin-user-full-name"
            label="Full name"
            error={errors.fullName?.message}
          >
            <Input
              id="admin-user-full-name"
              autoComplete="name"
              aria-invalid={Boolean(errors.fullName)}
              aria-describedby={errors.fullName ? "admin-user-full-name-error" : undefined}
              {...register("fullName")}
            />
          </Field>
          <Field id="admin-user-username" label="Username" error={errors.username?.message}>
            <Input
              id="admin-user-username"
              autoComplete="off"
              autoCapitalize="none"
              aria-invalid={Boolean(errors.username)}
              aria-describedby={errors.username ? "admin-user-username-error" : undefined}
              {...register("username")}
            />
          </Field>
          <Field id="admin-user-email" label="Email" error={errors.email?.message}>
            <Input
              id="admin-user-email"
              type="email"
              autoComplete="off"
              autoCapitalize="none"
              aria-invalid={Boolean(errors.email)}
              aria-describedby={errors.email ? "admin-user-email-error" : undefined}
              {...register("email")}
            />
          </Field>
          <Field
            id="admin-user-password"
            label="Initial password"
            error={errors.initialPassword?.message}
          >
            <Input
              id="admin-user-password"
              type="password"
              autoComplete="new-password"
              aria-invalid={Boolean(errors.initialPassword)}
              aria-describedby={errors.initialPassword ? "admin-user-password-error" : undefined}
              {...register("initialPassword")}
            />
          </Field>
          <div className="space-y-2">
            <Label htmlFor="admin-user-role">Role</Label>
            <select
              id="admin-user-role"
              className="h-10 w-full rounded-lg border border-input bg-background px-3 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
              {...register("role")}
            >
              {(["User", "Admin"] as UserRole[]).map((role) => (
                <option key={role} value={role}>{role}</option>
              ))}
            </select>
          </div>
        </form>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => setDialogOpen(false)} disabled={createMutation.isPending}>
            Cancel
          </Button>
          <Button type="submit" form="create-admin-user-form" disabled={createMutation.isPending}>
            {createMutation.isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" /> : <UserPlus aria-hidden="true" />}
            {createMutation.isPending ? "Creating…" : "Create user"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function Field({
  id,
  label,
  error,
  children,
}: {
  id: string;
  label: string;
  error?: string;
  children: ReactNode;
}) {
  return (
    <div className="space-y-2">
      <Label htmlFor={id}>{label}</Label>
      {children}
      {error ? (
        <p id={`${id}-error`} className="text-xs font-medium text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  );
}
