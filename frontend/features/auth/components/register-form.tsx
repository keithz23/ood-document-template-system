"use client";

import Link from "next/link";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { AlertCircle, FileText, LoaderCircle, UserPlus } from "lucide-react";
import { useRouter } from "next/navigation";
import { useForm, type UseFormRegisterReturn } from "react-hook-form";
import { z } from "zod";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getApiErrorMessage } from "@/services/api-errors";
import { registerUser } from "@/services/auth-api";

const registerSchema = z
  .object({
    username: z.string().trim().min(1, "Enter a username."),
    fullName: z.string().trim().min(1, "Enter your full name."),
    email: z.email("Enter a valid email address."),
    password: z.string().min(1, "Enter a password."),
    confirmPassword: z.string().min(1, "Confirm your password."),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: "Passwords must match.",
    path: ["confirmPassword"],
  });

type RegisterFormValues = z.infer<typeof registerSchema>;

export function RegisterForm() {
  const router = useRouter();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      username: "",
      fullName: "",
      email: "",
      password: "",
      confirmPassword: "",
    },
  });
  const registerMutation = useMutation({
    mutationFn: registerUser,
    onSuccess: () => router.replace("/login?registered=1"),
  });

  const submit = handleSubmit((values) => {
    registerMutation.mutate({
      username: values.username,
      fullName: values.fullName,
      email: values.email,
      password: values.password,
    });
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
            <p className="text-sm text-muted-foreground">Create an author account</p>
          </div>
        </div>

        <section aria-labelledby="register-heading" className="rounded-xl border bg-card p-6 sm:p-8">
          <h1 id="register-heading" className="text-2xl font-semibold tracking-[-0.02em]">
            Create account
          </h1>
          <p className="mt-1.5 text-sm leading-6 text-muted-foreground">
            Register as a document author. You’ll sign in after your account is created.
          </p>

          {registerMutation.isError ? (
            <Alert variant="destructive" className="mt-5" role="alert">
              <AlertCircle aria-hidden="true" />
              <AlertTitle>Account could not be created</AlertTitle>
              <AlertDescription>
                {getApiErrorMessage(
                  registerMutation.error,
                  "Review your details and try again.",
                )}
              </AlertDescription>
            </Alert>
          ) : null}

          <form className="mt-6 space-y-5" onSubmit={submit} noValidate>
            <RegisterField
              id="fullName"
              label="Full name"
              autoComplete="name"
              error={errors.fullName?.message}
              inputProps={register("fullName")}
            />
            <RegisterField
              id="username"
              label="Username"
              autoComplete="username"
              error={errors.username?.message}
              inputProps={register("username")}
            />
            <RegisterField
              id="email"
              label="Email"
              type="email"
              autoComplete="email"
              error={errors.email?.message}
              inputProps={register("email")}
            />
            <RegisterField
              id="password"
              label="Password"
              type="password"
              autoComplete="new-password"
              error={errors.password?.message}
              inputProps={register("password")}
            />
            <RegisterField
              id="confirmPassword"
              label="Confirm password"
              type="password"
              autoComplete="new-password"
              error={errors.confirmPassword?.message}
              inputProps={register("confirmPassword")}
            />

            <Button type="submit" className="w-full" size="lg" disabled={registerMutation.isPending}>
              {registerMutation.isPending ? (
                <LoaderCircle aria-hidden="true" className="animate-spin" />
              ) : (
                <UserPlus aria-hidden="true" />
              )}
              {registerMutation.isPending ? "Creating account…" : "Create account"}
            </Button>
          </form>

          <p className="mt-6 text-center text-sm text-muted-foreground">
            Already have an account?{" "}
            <Link href="/login" className="font-medium text-foreground underline underline-offset-4">
              Sign in
            </Link>
          </p>
        </section>
      </div>
    </main>
  );
}

function RegisterField({
  id,
  label,
  type = "text",
  autoComplete,
  error,
  inputProps,
}: {
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
      <Input
        id={id}
        type={type}
        autoComplete={autoComplete}
        autoCapitalize={id === "username" || id === "email" ? "none" : undefined}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        {...inputProps}
      />
      {error ? (
        <p id={errorId} className="text-xs font-medium text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  );
}
