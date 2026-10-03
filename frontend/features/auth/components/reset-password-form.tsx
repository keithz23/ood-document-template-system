"use client";

import Link from "next/link";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { AlertCircle, FileText, KeyRound, LoaderCircle } from "lucide-react";
import { useRouter } from "next/navigation";
import { useForm, type UseFormRegisterReturn } from "react-hook-form";
import { z } from "zod";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getApiErrorMessage } from "@/services/api-errors";
import { resetPassword } from "@/services/auth-api";

const schema = z
  .object({
    newPassword: z.string().min(1, "Enter a new password."),
    confirmPassword: z.string().min(1, "Confirm your new password."),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: "Passwords must match.",
    path: ["confirmPassword"],
  });
type Values = z.infer<typeof schema>;

export function ResetPasswordForm({ token }: { token: string }) {
  const router = useRouter();
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { newPassword: "", confirmPassword: "" },
  });
  const mutation = useMutation({
    mutationFn: resetPassword,
    onSuccess: () => router.replace("/login?passwordReset=1"),
  });

  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-4 py-10 sm:px-6">
      <div className="w-full max-w-md">
        <div className="mb-6 flex items-center gap-3">
          <span className="flex size-10 items-center justify-center rounded-lg bg-primary text-primary-foreground">
            <FileText aria-hidden="true" className="size-5" />
          </span>
          <div>
            <p className="font-semibold">Document Workspace</p>
            <p className="text-sm text-muted-foreground">Account recovery</p>
          </div>
        </div>

        <section aria-labelledby="reset-password-heading" className="rounded-xl border bg-card p-6 sm:p-8">
          <h1 id="reset-password-heading" className="text-2xl font-semibold tracking-[-0.02em]">Set a new password</h1>
          <p className="mt-1.5 text-sm leading-6 text-muted-foreground">
            Choose a new password for your account. You’ll return to sign in when it is saved.
          </p>

          {!token ? (
            <Alert variant="destructive" className="mt-5" role="alert">
              <AlertCircle aria-hidden="true" />
              <AlertTitle>Reset link is incomplete</AlertTitle>
              <AlertDescription>Request a new password reset link to continue.</AlertDescription>
            </Alert>
          ) : null}
          {mutation.isError ? (
            <Alert variant="destructive" className="mt-5" role="alert">
              <AlertCircle aria-hidden="true" />
              <AlertTitle>Password could not be reset</AlertTitle>
              <AlertDescription>{getApiErrorMessage(mutation.error, "Request a new reset link and try again.")}</AlertDescription>
            </Alert>
          ) : null}

          <form
            className="mt-6 space-y-5"
            onSubmit={form.handleSubmit((values) => mutation.mutate({ token, newPassword: values.newPassword }))}
            noValidate
          >
            <PasswordField id="reset-new-password" label="New password" error={form.formState.errors.newPassword?.message} inputProps={form.register("newPassword")} />
            <PasswordField id="reset-confirm-password" label="Confirm new password" error={form.formState.errors.confirmPassword?.message} inputProps={form.register("confirmPassword")} />
            <Button type="submit" className="w-full" size="lg" disabled={!token || mutation.isPending}>
              {mutation.isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" /> : <KeyRound aria-hidden="true" />}
              {mutation.isPending ? "Saving…" : "Reset password"}
            </Button>
          </form>

          <p className="mt-6 text-center text-sm text-muted-foreground">
            Need a new link?{" "}
            <Link href="/forgot-password" className="font-medium text-foreground underline underline-offset-4">Start again</Link>
          </p>
        </section>
      </div>
    </main>
  );
}

function PasswordField({ id, label, error, inputProps }: {
  id: string;
  label: string;
  error?: string;
  inputProps: UseFormRegisterReturn;
}) {
  const errorId = `${id}-error`;
  return (
    <div className="space-y-2">
      <Label htmlFor={id}>{label}</Label>
      <Input id={id} type="password" autoComplete="new-password" aria-invalid={Boolean(error)} aria-describedby={error ? errorId : undefined} {...inputProps} />
      {error ? <p id={errorId} className="text-xs font-medium text-destructive">{error}</p> : null}
    </div>
  );
}
