"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { AlertCircle, FileText, LoaderCircle, LogIn } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/features/auth/auth-provider";
import { getApiErrorMessage } from "@/services/api-errors";
import { login } from "@/services/auth-api";

const loginSchema = z.object({
  username: z.string().trim().min(1, "Enter your username or email."),
  password: z.string().min(1, "Enter your password."),
});

type LoginFormValues = z.infer<typeof loginSchema>;

function safeReturnTo(value: string | null) {
  return value?.startsWith("/") && !value.startsWith("//")
    ? value
    : "/templates";
}

export function LoginForm() {
  const { session, isReady, establishSession } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnTo = safeReturnTo(searchParams.get("returnTo"));
  const sessionExpired = searchParams.get("reason") === "session-expired";
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { username: "", password: "" },
  });
  const loginMutation = useMutation({
    mutationFn: login,
    onSuccess: (response) => {
      establishSession(response);
      router.replace(returnTo);
    },
  });

  useEffect(() => {
    if (isReady && session) router.replace(returnTo);
  }, [isReady, returnTo, router, session]);

  const submit = handleSubmit((values) => loginMutation.mutate(values));

  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-4 py-10 sm:px-6">
      <div className="w-full max-w-md">
        <div className="mb-6 flex items-center gap-3">
          <span className="flex size-10 items-center justify-center rounded-lg bg-primary text-primary-foreground">
            <FileText aria-hidden="true" className="size-5" />
          </span>
          <div>
            <p className="font-semibold">Document Workspace</p>
            <p className="text-sm text-muted-foreground">Sign in to continue</p>
          </div>
        </div>

        <section aria-labelledby="login-heading" className="rounded-xl border bg-card p-6 sm:p-8">
          <h1 id="login-heading" className="text-2xl font-semibold tracking-[-0.02em]">
            Sign in
          </h1>
          <p className="mt-1.5 text-sm leading-6 text-muted-foreground">
            Access active templates and create documents in your workspace.
          </p>

          {sessionExpired ? (
            <Alert className="mt-5">
              <AlertCircle aria-hidden="true" />
              <AlertTitle>Your session ended</AlertTitle>
              <AlertDescription>Sign in again to continue where you left off.</AlertDescription>
            </Alert>
          ) : null}

          {loginMutation.isError ? (
            <Alert variant="destructive" className="mt-5" role="alert">
              <AlertCircle aria-hidden="true" />
              <AlertTitle>Sign-in failed</AlertTitle>
              <AlertDescription>
                {getApiErrorMessage(
                  loginMutation.error,
                  "Check your connection and try again.",
                )}
              </AlertDescription>
            </Alert>
          ) : null}

          <form className="mt-6 space-y-5" onSubmit={submit} noValidate>
            <div className="space-y-2">
              <Label htmlFor="username">Username or email</Label>
              <Input
                id="username"
                autoComplete="username"
                autoCapitalize="none"
                aria-invalid={Boolean(errors.username)}
                aria-describedby={errors.username ? "username-error" : undefined}
                {...register("username")}
              />
              {errors.username ? (
                <p id="username-error" className="text-xs font-medium text-destructive">
                  {errors.username.message}
                </p>
              ) : null}
            </div>

            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                aria-invalid={Boolean(errors.password)}
                aria-describedby={errors.password ? "password-error" : undefined}
                {...register("password")}
              />
              {errors.password ? (
                <p id="password-error" className="text-xs font-medium text-destructive">
                  {errors.password.message}
                </p>
              ) : null}
            </div>

            <Button type="submit" className="w-full" size="lg" disabled={loginMutation.isPending}>
              {loginMutation.isPending ? (
                <LoaderCircle aria-hidden="true" className="animate-spin" />
              ) : (
                <LogIn aria-hidden="true" />
              )}
              {loginMutation.isPending ? "Signing in…" : "Sign in"}
            </Button>
          </form>
        </section>
      </div>
    </main>
  );
}
