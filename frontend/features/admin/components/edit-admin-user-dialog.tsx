"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { LoaderCircle, Pencil } from "lucide-react";
import { useState } from "react";
import { useForm, type UseFormRegisterReturn } from "react-hook-form";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useUpdateAdminUser } from "@/features/admin/api/admin-queries";
import { getApiErrorMessage } from "@/services/api-errors";
import type { AdminUserDto } from "@/types/api";

const schema = z.object({
  username: z.string().trim().min(1, "Enter a username."),
  fullName: z.string().trim().min(1, "Enter a full name."),
  email: z.email("Enter a valid email address."),
});
type Values = z.infer<typeof schema>;

export function EditAdminUserDialog({
  user,
  onSaved,
}: {
  user: AdminUserDto;
  onSaved: (updated: AdminUserDto) => void;
}) {
  const [open, setOpen] = useState(false);
  const mutation = useUpdateAdminUser();
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      username: user.username,
      fullName: user.fullName,
      email: user.email,
    },
  });

  const submit = form.handleSubmit((values) => {
    mutation.mutate({ id: user.id, request: values }, {
      onSuccess: (updated) => {
        onSaved(updated);
        setOpen(false);
      },
    });
  });

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        setOpen(nextOpen);
        if (nextOpen) {
          form.reset({ username: user.username, fullName: user.fullName, email: user.email });
          mutation.reset();
        }
      }}
    >
      <DialogTrigger render={<Button type="button" variant="outline" size="sm" />}>
        <Pencil aria-hidden="true" />
        Edit user
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit user identity</DialogTitle>
          <DialogDescription>
            Update this user’s name, username, or email. Role, status, and password remain unchanged.
          </DialogDescription>
        </DialogHeader>

        {mutation.isError ? (
          <p className="text-sm font-medium text-destructive" role="alert">
            {getApiErrorMessage(
              mutation.error,
              "This identity could not be saved. Review the details and try again.",
            )}
          </p>
        ) : null}

        <form id="edit-admin-user-form" className="space-y-4" onSubmit={submit} noValidate>
          <EditField id="edit-user-full-name" label="Full name" autoComplete="name" error={form.formState.errors.fullName?.message} inputProps={form.register("fullName")} />
          <EditField id="edit-user-username" label="Username" autoComplete="username" error={form.formState.errors.username?.message} inputProps={form.register("username")} />
          <EditField id="edit-user-email" label="Email" type="email" autoComplete="email" error={form.formState.errors.email?.message} inputProps={form.register("email")} />
        </form>

        <DialogFooter>
          <DialogClose render={<Button type="button" variant="outline" />}>Cancel</DialogClose>
          <Button type="submit" form="edit-admin-user-form" disabled={mutation.isPending || !form.formState.isDirty}>
            {mutation.isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" /> : null}
            {mutation.isPending ? "Saving…" : "Save changes"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function EditField({ id, label, type = "text", autoComplete, error, inputProps }: {
  id: string;
  label: string;
  type?: string;
  autoComplete: string;
  error?: string;
  inputProps: UseFormRegisterReturn;
}) {
  const errorId = `${id}-error`;
  return (
    <div className="space-y-2">
      <Label htmlFor={id}>{label}</Label>
      <Input id={id} type={type} autoComplete={autoComplete} aria-invalid={Boolean(error)} aria-describedby={error ? errorId : undefined} {...inputProps} />
      {error ? <p id={errorId} className="text-xs font-medium text-destructive">{error}</p> : null}
    </div>
  );
}
