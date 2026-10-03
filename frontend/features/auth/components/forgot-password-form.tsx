"use client";

import Link from "next/link";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { AlertCircle, CheckCircle2, FileText, LoaderCircle, Mail } from "lucide-react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getApiErrorMessage } from "@/services/api-errors";
import { forgotPassword } from "@/services/auth-api";

const schema = z.object({ email: z.email("Enter a valid email address.") });
type Values = z.infer<typeof schema>;

export function ForgotPasswordForm() {
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { email: "" },
  });
  const mutation = useMutation({ mutationFn: forgotPassword });

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

        <section aria-labelledby="forgot-password-heading" className="rounded-xl border bg-card p-6 sm:p-8">
          <h1 id="forgot-password-heading" className="text-2xl font-semibold tracking-[-0.02em]">Forgot password</h1>
          <p className="mt-1.5 text-sm leading-6 text-muted-foreground">
            Enter your account email. If an active account matches, we’ll send reset instructions.
          </p>

          {mutation.isSuccess ? (
            <Alert className="mt-5" role="status">
              <CheckCircle2 aria-hidden="true" />
              <AlertTitle>Check your email</AlertTitle>
              <AlertDescription>{mutation.data.message}</AlertDescription>
            </Alert>
          ) : null}
          {mutation.isError ? (
            <Alert variant="destructive" className="mt-5" role="alert">
              <AlertCircle aria-hidden="true" />
              <AlertTitle>Request could not be sent</AlertTitle>
              <AlertDescription>{getApiErrorMessage(mutation.error, "Check your connection and try again.")}</AlertDescription>
            </Alert>
          ) : null}

          <form className="mt-6 space-y-5" onSubmit={form.handleSubmit((values) => mutation.mutate(values))} noValidate>
            <div className="space-y-2">
              <Label htmlFor="recovery-email">Email</Label>
              <Input
                id="recovery-email"
                type="email"
                autoComplete="email"
                autoCapitalize="none"
                aria-invalid={Boolean(form.formState.errors.email)}
                aria-describedby={form.formState.errors.email ? "recovery-email-error" : undefined}
                {...form.register("email")}
              />
              {form.formState.errors.email ? (
                <p id="recovery-email-error" className="text-xs font-medium text-destructive">{form.formState.errors.email.message}</p>
              ) : null}
            </div>
            <Button type="submit" className="w-full" size="lg" disabled={mutation.isPending}>
              {mutation.isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" /> : <Mail aria-hidden="true" />}
              {mutation.isPending ? "Sending…" : "Send reset instructions"}
            </Button>
          </form>

          <p className="mt-6 text-center text-sm text-muted-foreground">
            Remember your password?{" "}
            <Link href="/login" className="font-medium text-foreground underline underline-offset-4">Return to sign in</Link>
          </p>
        </section>
      </div>
    </main>
  );
}
