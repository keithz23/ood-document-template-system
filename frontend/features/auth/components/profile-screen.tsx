"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { AlertCircle, CheckCircle2, KeyRound, LoaderCircle, UserRound } from "lucide-react";
import { useEffect, useState } from "react";
import { useForm, type UseFormRegisterReturn } from "react-hook-form";
import { z } from "zod";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PageHeader } from "@/components/shared/page-header";
import { useAuth } from "@/features/auth/auth-provider";
import { getApiErrorMessage } from "@/services/api-errors";
import { changePassword, updateOwnProfile } from "@/services/auth-api";

const profileSchema = z.object({
  fullName: z.string().trim().min(1, "Enter your full name."),
  email: z.email("Enter a valid email address."),
});

const passwordSchema = z
  .object({
    currentPassword: z.string().min(1, "Enter your current password."),
    newPassword: z.string().min(1, "Enter a new password."),
    confirmPassword: z.string().min(1, "Confirm your new password."),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: "Passwords must match.",
    path: ["confirmPassword"],
  });

type ProfileValues = z.infer<typeof profileSchema>;
type PasswordValues = z.infer<typeof passwordSchema>;

export function ProfileScreen() {
  const { session, updateCurrentUser, logout } = useAuth();
  const user = session?.user;
  const [profileSaved, setProfileSaved] = useState(false);
  const profileForm = useForm<ProfileValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: { fullName: user?.fullName ?? "", email: user?.email ?? "" },
  });
  const passwordForm = useForm<PasswordValues>({
    resolver: zodResolver(passwordSchema),
    defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" },
  });

  useEffect(() => {
    if (user && !profileForm.formState.isDirty) {
      profileForm.reset({ fullName: user.fullName, email: user.email });
    }
  }, [profileForm, profileForm.formState.isDirty, user]);

  const profileMutation = useMutation({
    mutationFn: updateOwnProfile,
    onSuccess: (updatedUser) => {
      updateCurrentUser(updatedUser);
      profileForm.reset({ fullName: updatedUser.fullName, email: updatedUser.email });
      setProfileSaved(true);
    },
  });
  const passwordMutation = useMutation({
    mutationFn: changePassword,
    onSuccess: () => logout("/login?passwordChanged=1"),
  });

  const submitProfile = profileForm.handleSubmit((values) => {
    setProfileSaved(false);
    profileMutation.mutate(values);
  });
  const submitPassword = passwordForm.handleSubmit((values) => {
    passwordMutation.mutate({
      currentPassword: values.currentPassword,
      newPassword: values.newPassword,
    });
  });

  return (
    <div className="space-y-7">
      <PageHeader
        title="Profile"
        description="Manage the identity details shown in your workspace and update your password."
      />

      <div className="grid gap-6 xl:grid-cols-2">
        <section aria-labelledby="profile-details-heading" className="rounded-xl border bg-card p-5 sm:p-6">
          <div className="flex items-start gap-3">
            <span className="flex size-9 items-center justify-center rounded-lg bg-muted text-muted-foreground">
              <UserRound aria-hidden="true" className="size-4" />
            </span>
            <div>
              <h2 id="profile-details-heading" className="font-semibold">Profile details</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                Your username is fixed. You can update your name and email.
              </p>
            </div>
          </div>

          {profileSaved && !profileForm.formState.isDirty ? (
            <Alert className="mt-5" role="status">
              <CheckCircle2 aria-hidden="true" />
              <AlertTitle>Profile saved</AlertTitle>
              <AlertDescription>Your workspace identity is up to date.</AlertDescription>
            </Alert>
          ) : null}
          {profileMutation.isError ? (
            <Alert variant="destructive" className="mt-5" role="alert">
              <AlertCircle aria-hidden="true" />
              <AlertTitle>Profile could not be saved</AlertTitle>
              <AlertDescription>
                {getApiErrorMessage(profileMutation.error, "Review your details and try again.")}
              </AlertDescription>
            </Alert>
          ) : null}

          <form className="mt-6 space-y-5" onSubmit={submitProfile} noValidate>
            <div className="space-y-2">
              <Label htmlFor="profile-username">Username</Label>
              <Input id="profile-username" value={user?.username ?? ""} readOnly aria-describedby="profile-username-help" />
              <p id="profile-username-help" className="text-xs text-muted-foreground">Usernames cannot be changed from your profile.</p>
            </div>
            <div className="space-y-2">
              <Label htmlFor="profile-role">Role</Label>
              <Input id="profile-role" value={user?.role ?? ""} readOnly aria-describedby="profile-role-help" />
              <p id="profile-role-help" className="text-xs text-muted-foreground">Roles are managed by an administrator.</p>
            </div>
            <ProfileField
              id="profile-full-name"
              label="Full name"
              autoComplete="name"
              error={profileForm.formState.errors.fullName?.message}
              inputProps={profileForm.register("fullName")}
            />
            <ProfileField
              id="profile-email"
              label="Email"
              type="email"
              autoComplete="email"
              error={profileForm.formState.errors.email?.message}
              inputProps={profileForm.register("email")}
            />
            <Button type="submit" disabled={profileMutation.isPending || !profileForm.formState.isDirty}>
              {profileMutation.isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" /> : null}
              {profileMutation.isPending ? "Saving…" : "Save profile"}
            </Button>
          </form>
        </section>

        <section aria-labelledby="password-heading" className="rounded-xl border bg-card p-5 sm:p-6">
          <div className="flex items-start gap-3">
            <span className="flex size-9 items-center justify-center rounded-lg bg-muted text-muted-foreground">
              <KeyRound aria-hidden="true" className="size-4" />
            </span>
            <div>
              <h2 id="password-heading" className="font-semibold">Change password</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                You’ll be signed out after a successful password change.
              </p>
            </div>
          </div>

          {passwordMutation.isError ? (
            <Alert variant="destructive" className="mt-5" role="alert">
              <AlertCircle aria-hidden="true" />
              <AlertTitle>Password could not be changed</AlertTitle>
              <AlertDescription>
                {getApiErrorMessage(passwordMutation.error, "Review your password and try again.")}
              </AlertDescription>
            </Alert>
          ) : null}

          <form className="mt-6 space-y-5" onSubmit={submitPassword} noValidate>
            <ProfileField
              id="current-password"
              label="Current password"
              type="password"
              autoComplete="current-password"
              error={passwordForm.formState.errors.currentPassword?.message}
              inputProps={passwordForm.register("currentPassword")}
            />
            <ProfileField
              id="new-password"
              label="New password"
              type="password"
              autoComplete="new-password"
              error={passwordForm.formState.errors.newPassword?.message}
              inputProps={passwordForm.register("newPassword")}
            />
            <ProfileField
              id="confirm-password"
              label="Confirm new password"
              type="password"
              autoComplete="new-password"
              error={passwordForm.formState.errors.confirmPassword?.message}
              inputProps={passwordForm.register("confirmPassword")}
            />
            <Button type="submit" variant="outline" disabled={passwordMutation.isPending}>
              {passwordMutation.isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" /> : null}
              {passwordMutation.isPending ? "Changing…" : "Change password"}
            </Button>
          </form>
        </section>
      </div>
    </div>
  );
}

function ProfileField({
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
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        {...inputProps}
      />
      {error ? <p id={errorId} className="text-xs font-medium text-destructive">{error}</p> : null}
    </div>
  );
}
