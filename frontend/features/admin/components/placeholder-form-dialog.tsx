"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { AlertCircle, LoaderCircle } from "lucide-react";
import { useEffect } from "react";
import { useForm, useWatch } from "react-hook-form";
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
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getApiErrorMessage } from "@/services/api-errors";
import type { PlaceholderDataType, PlaceholderDto } from "@/types/api";

const placeholderSchema = z
  .object({
    key: z.string().trim().min(1, "Enter a placeholder key."),
    label: z.string().trim().min(1, "Enter a label."),
    dataType: z.enum(["Text", "Number", "Date", "Email"]),
    isRequired: z.boolean(),
    defaultValue: z.string(),
  })
  .superRefine((value, context) => {
    const defaultValue = value.defaultValue.trim();
    if (!defaultValue) return;

    const valid =
      value.dataType === "Text" ||
      (value.dataType === "Number" && Number.isFinite(Number(defaultValue))) ||
      (value.dataType === "Date" && /^\d{4}-\d{2}-\d{2}$/.test(defaultValue)) ||
      (value.dataType === "Email" && z.email().safeParse(defaultValue).success);

    if (!valid) {
      context.addIssue({
        code: "custom",
        path: ["defaultValue"],
        message: `Enter a valid ${value.dataType.toLowerCase()} default.`,
      });
    }
  });

type PlaceholderFormValues = z.infer<typeof placeholderSchema>;

const emptyValues: PlaceholderFormValues = {
  key: "",
  label: "",
  dataType: "Text",
  isRequired: false,
  defaultValue: "",
};

export function PlaceholderFormDialog({
  open,
  placeholder,
  isPending,
  error,
  onOpenChange,
  onSubmit,
}: {
  open: boolean;
  placeholder: PlaceholderDto | null;
  isPending: boolean;
  error: unknown;
  onOpenChange: (open: boolean) => void;
  onSubmit: (values: {
    key: string;
    label: string;
    dataType: PlaceholderDataType;
    isRequired: boolean;
    defaultValue: string | null;
  }) => Promise<void>;
}) {
  const form = useForm<PlaceholderFormValues>({
    resolver: zodResolver(placeholderSchema),
    defaultValues: emptyValues,
  });
  const dataType = useWatch({ control: form.control, name: "dataType" });

  useEffect(() => {
    if (!open) return;
    form.reset(
      placeholder
        ? {
            key: placeholder.key,
            label: placeholder.label,
            dataType: placeholder.dataType,
            isRequired: placeholder.isRequired,
            defaultValue: placeholder.defaultValue ?? "",
          }
        : emptyValues,
    );
  }, [form, open, placeholder]);

  async function submit(values: PlaceholderFormValues) {
    await onSubmit({
      ...values,
      key: values.key.trim(),
      label: values.label.trim(),
      defaultValue: values.defaultValue.trim() || null,
    });
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {placeholder ? "Edit placeholder" : "Add placeholder"}
          </DialogTitle>
          <DialogDescription>
            Define the value collected when a document is created from this version.
          </DialogDescription>
        </DialogHeader>

        {error ? (
          <Alert variant="destructive" role="alert">
            <AlertCircle aria-hidden="true" />
            <AlertTitle>Placeholder could not be saved</AlertTitle>
            <AlertDescription>
              {getApiErrorMessage(error, "Check the placeholder details and try again.")}
            </AlertDescription>
          </Alert>
        ) : null}

        <form
          id="placeholder-form"
          className="grid gap-5 sm:grid-cols-2"
          onSubmit={form.handleSubmit(submit)}
          noValidate
        >
          <Field label="Key" error={form.formState.errors.key?.message}>
            <Input
              id="placeholder-key"
              autoFocus
              placeholder="client_name"
              aria-invalid={Boolean(form.formState.errors.key)}
              {...form.register("key")}
            />
          </Field>
          <Field label="Label" error={form.formState.errors.label?.message}>
            <Input
              id="placeholder-label"
              placeholder="Client name"
              aria-invalid={Boolean(form.formState.errors.label)}
              {...form.register("label")}
            />
          </Field>
          <Field label="Data type" error={form.formState.errors.dataType?.message}>
            <select
              id="placeholder-data-type"
              className="h-10 w-full rounded-lg border border-input bg-background px-3 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
              {...form.register("dataType")}
            >
              <option value="Text">Text</option>
              <option value="Number">Number</option>
              <option value="Date">Date</option>
              <option value="Email">Email</option>
            </select>
          </Field>
          <Field
            label="Default value"
            error={form.formState.errors.defaultValue?.message}
          >
            <Input
              id="placeholder-default"
              type={
                dataType === "Date"
                  ? "date"
                  : dataType === "Email"
                    ? "email"
                    : dataType === "Number"
                      ? "number"
                      : "text"
              }
              step={dataType === "Number" ? "any" : undefined}
              aria-invalid={Boolean(form.formState.errors.defaultValue)}
              {...form.register("defaultValue")}
            />
          </Field>
          <label className="flex min-h-10 items-center gap-3 text-sm font-medium sm:col-span-2">
            <input
              type="checkbox"
              className="size-4 rounded border-input accent-primary"
              {...form.register("isRequired")}
            />
            Required when a document is finalized
          </label>
        </form>

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            disabled={isPending}
            onClick={() => onOpenChange(false)}
          >
            Cancel
          </Button>
          <Button type="submit" form="placeholder-form" disabled={isPending}>
            {isPending ? (
              <LoaderCircle aria-hidden="true" className="animate-spin" />
            ) : null}
            {isPending ? "Saving…" : placeholder ? "Save changes" : "Add placeholder"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-2">
      <Label>{label}</Label>
      {children}
      {error ? <p className="text-xs font-medium text-destructive">{error}</p> : null}
    </div>
  );
}
