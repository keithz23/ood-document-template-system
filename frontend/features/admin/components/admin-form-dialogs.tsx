"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { AlertCircle, LoaderCircle } from "lucide-react";
import { useEffect } from "react";
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
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getApiErrorMessage } from "@/services/api-errors";
import type { AdminCategoryDto } from "@/types/api";

const categorySchema = z.object({
  name: z.string().trim().min(1, "Enter a category name."),
});

type CategoryFormValues = z.infer<typeof categorySchema>;

export function CategoryFormDialog({
  open,
  initialName = "",
  isPending,
  error,
  onOpenChange,
  onSubmit,
}: {
  open: boolean;
  initialName?: string;
  isPending: boolean;
  error: unknown;
  onOpenChange: (open: boolean) => void;
  onSubmit: (values: CategoryFormValues) => Promise<void>;
}) {
  const editing = Boolean(initialName);
  const form = useForm<CategoryFormValues>({
    resolver: zodResolver(categorySchema),
    defaultValues: { name: initialName },
  });

  useEffect(() => {
    if (open) form.reset({ name: initialName });
  }, [form, initialName, open]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{editing ? "Edit category" : "Create category"}</DialogTitle>
          <DialogDescription>
            {editing
              ? "Update the category name without changing its active state."
              : "Add an active category for organizing templates."}
          </DialogDescription>
        </DialogHeader>
        {error ? (
          <Alert variant="destructive" role="alert">
            <AlertCircle aria-hidden="true" />
            <AlertTitle>Category could not be saved</AlertTitle>
            <AlertDescription>
              {getApiErrorMessage(error, "Check the category details and try again.")}
            </AlertDescription>
          </Alert>
        ) : null}
        <form
          id="category-form"
          className="space-y-2"
          onSubmit={form.handleSubmit(onSubmit)}
          noValidate
        >
          <Label htmlFor="category-name">Name</Label>
          <Input
            id="category-name"
            autoFocus
            aria-invalid={Boolean(form.formState.errors.name)}
            aria-describedby={form.formState.errors.name ? "category-name-error" : undefined}
            {...form.register("name")}
          />
          {form.formState.errors.name ? (
            <p id="category-name-error" className="text-xs font-medium text-destructive">
              {form.formState.errors.name.message}
            </p>
          ) : null}
        </form>
        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            Cancel
          </Button>
          <Button type="submit" form="category-form" disabled={isPending}>
            {isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" /> : null}
            {isPending ? "Saving…" : editing ? "Save changes" : "Create category"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

const templateSchema = z.object({
  name: z.string().trim().min(1, "Enter a template name."),
  categoryId: z.string().min(1, "Choose a category."),
});

export type TemplateFormValues = z.infer<typeof templateSchema>;

export function TemplateFormDialog({
  open,
  title,
  description,
  submitLabel,
  categories,
  initialValues,
  isPending,
  error,
  onOpenChange,
  onSubmit,
}: {
  open: boolean;
  title: string;
  description: string;
  submitLabel: string;
  categories: readonly AdminCategoryDto[];
  initialValues: TemplateFormValues;
  isPending: boolean;
  error: unknown;
  onOpenChange: (open: boolean) => void;
  onSubmit: (values: TemplateFormValues) => Promise<void>;
}) {
  const form = useForm<TemplateFormValues>({
    resolver: zodResolver(templateSchema),
    defaultValues: initialValues,
  });

  useEffect(() => {
    if (open) {
      form.reset({
        name: initialValues.name,
        categoryId: initialValues.categoryId,
      });
    }
  }, [form, initialValues.categoryId, initialValues.name, open]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        {error ? (
          <Alert variant="destructive" role="alert">
            <AlertCircle aria-hidden="true" />
            <AlertTitle>Template could not be saved</AlertTitle>
            <AlertDescription>
              {getApiErrorMessage(error, "Check the template details and try again.")}
            </AlertDescription>
          </Alert>
        ) : null}
        <form
          id="template-form"
          className="space-y-5"
          onSubmit={form.handleSubmit(onSubmit)}
          noValidate
        >
          <div className="space-y-2">
            <Label htmlFor="template-name">Name</Label>
            <Input
              id="template-name"
              autoFocus
              aria-invalid={Boolean(form.formState.errors.name)}
              aria-describedby={form.formState.errors.name ? "template-name-error" : undefined}
              {...form.register("name")}
            />
            {form.formState.errors.name ? (
              <p id="template-name-error" className="text-xs font-medium text-destructive">
                {form.formState.errors.name.message}
              </p>
            ) : null}
          </div>
          <div className="space-y-2">
            <Label htmlFor="template-category">Category</Label>
            <select
              id="template-category"
              className="h-9 w-full rounded-lg border border-input bg-background px-3 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
              aria-invalid={Boolean(form.formState.errors.categoryId)}
              aria-describedby={form.formState.errors.categoryId ? "template-category-error" : undefined}
              {...form.register("categoryId")}
            >
              <option value="">Choose a category</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}{category.isActive ? "" : " (Inactive)"}
                </option>
              ))}
            </select>
            {form.formState.errors.categoryId ? (
              <p id="template-category-error" className="text-xs font-medium text-destructive">
                {form.formState.errors.categoryId.message}
              </p>
            ) : null}
          </div>
        </form>
        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            Cancel
          </Button>
          <Button type="submit" form="template-form" disabled={isPending || categories.length === 0}>
            {isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" /> : null}
            {isPending ? "Saving…" : submitLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
