"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, Check, Download, Eye, FilePenLine, LockKeyhole, Save } from "lucide-react";
import { useMemo, useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { z } from "zod";
import { StatusBadge } from "@/components/shared/status-badge";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  useCreateDraftDocument,
  useDownloadDocument,
  useFinalizeDocument,
  usePreviewDocument,
  useUpdateDraftDocument,
} from "@/features/documents/api/document-queries";
import { cn } from "@/lib/utils";
import { getApiErrorMessage } from "@/services/api-errors";
import type {
  ContentFormat,
  DocumentDetailDto,
  DocumentStatus,
  PlaceholderDataType,
  PlaceholderValueInputDto,
} from "@/types/api";

export type EditorPlaceholder = Readonly<{
  id: string | null;
  key: string;
  label: string;
  dataType: PlaceholderDataType;
  isRequired: boolean;
  defaultValue?: string | null;
}>;

export type EditorTemplate = Readonly<{
  id: string;
  name: string;
  versionNumber: number;
  templateVersionId: string;
  content: string;
  contentFormat: ContentFormat;
  placeholders: readonly EditorPlaceholder[];
}>;

type EditorFormValues = {
  title: string;
  values: Record<string, string>;
};

type WorkspaceMode = "edit" | "preview";
type MobilePane = "fields" | "document" | "preview";

function createEditorSchema(placeholders: readonly EditorPlaceholder[]) {
  return z.object({
    title: z.string().trim().min(1, "Document title is required."),
    values: z.record(z.string(), z.string()),
  }).superRefine(({ values }, context) => {
    placeholders.forEach((placeholder) => {
      const value = values[placeholder.key]?.trim() ?? "";
      if (!value) return;
      if (placeholder.dataType === "Number" && !Number.isFinite(Number(value))) {
        context.addIssue({ code: "custom", message: "Enter a valid number.", path: ["values", placeholder.key] });
      }
      if (placeholder.dataType === "Email" && !z.email().safeParse(value).success) {
        context.addIssue({ code: "custom", message: "Enter a valid email address.", path: ["values", placeholder.key] });
      }
      if (placeholder.dataType === "Date" && !/^\d{4}-\d{2}-\d{2}$/.test(value)) {
        context.addIssue({ code: "custom", message: "Enter a date in YYYY-MM-DD format.", path: ["values", placeholder.key] });
      }
    });
  });
}

function fillTemplate(content: string, values: Record<string, string>) {
  return Object.entries(values).reduce(
    (rendered, [key, value]) => rendered.replaceAll(`{{${key}}}`, value || `{{${key}}}`),
    content,
  );
}

function isLongTextField(placeholder: EditorPlaceholder) {
  return placeholder.dataType === "Text" && ["description", "summary", "experience", "outcomes", "risks", "priorities", "items", "decisions", "skills", "education"].some((term) => placeholder.key.includes(term));
}

function PlaceholderField({ placeholder, register, error }: {
  placeholder: EditorPlaceholder;
  register: ReturnType<typeof useForm<EditorFormValues>>["register"];
  error?: string;
}) {
  const fieldId = `field-${placeholder.key}`;
  const helpId = `${fieldId}-help`;
  const errorId = `${fieldId}-error`;
  const fieldType = placeholder.dataType === "Date" ? "date" : placeholder.dataType === "Email" ? "email" : placeholder.dataType === "Number" ? "number" : "text";
  const describedBy = error ? `${helpId} ${errorId}` : helpId;
  const fieldProps = {
    id: fieldId,
    "aria-invalid": Boolean(error),
    "aria-describedby": describedBy,
    placeholder: placeholder.defaultValue ?? undefined,
    ...register(`values.${placeholder.key}`),
  };

  return (
    <div className="space-y-2">
      <Label htmlFor={fieldId} className="text-sm font-medium">
        {placeholder.label}
        {placeholder.isRequired ? <span aria-hidden="true" className="ml-1 text-red-600">*</span> : <span className="ml-1 font-normal text-muted-foreground">(optional)</span>}
      </Label>
      {isLongTextField(placeholder) ? <Textarea {...fieldProps} className="min-h-24 resize-y" /> : <Input {...fieldProps} type={fieldType} className="h-10" />}
      <p id={helpId} className="text-xs text-muted-foreground">{placeholder.dataType} value</p>
      {error ? <p id={errorId} className="text-xs font-medium text-red-700">{error}</p> : null}
    </div>
  );
}

function DocumentPage({ content }: { content: string }) {
  return (
    <iframe
      title="Document preview"
      sandbox=""
      srcDoc={content}
      className="mx-auto min-h-[720px] w-full max-w-[800px] bg-white ring-1 ring-slate-200"
    />
  );
}

function toPlaceholderInputs(
  placeholders: readonly EditorPlaceholder[],
  values: Record<string, string>,
): PlaceholderValueInputDto[] {
  return placeholders.flatMap((placeholder) => placeholder.id
    ? [{ placeholderId: placeholder.id, value: values[placeholder.key] ?? "" }]
    : []);
}

export function DocumentEditor({ template, document }: {
  template: EditorTemplate;
  document?: DocumentDetailDto;
}) {
  const router = useRouter();
  const documentId = document?.id ?? "";
  const schema = useMemo(() => createEditorSchema(template.placeholders), [template.placeholders]);
  const persistedValues = useMemo(
    () => Object.fromEntries(document?.placeholderValues.map((value) => [value.placeholderKeySnapshot, value.value]) ?? []),
    [document],
  );
  const defaultValues = useMemo<EditorFormValues>(() => ({
    title: document?.title ?? `Untitled ${template.name}`,
    values: Object.fromEntries(template.placeholders.map((placeholder) => [placeholder.key, persistedValues[placeholder.key] ?? placeholder.defaultValue ?? ""])),
  }), [document, persistedValues, template]);
  const { register, handleSubmit, control, formState: { errors } } = useForm<EditorFormValues>({ resolver: zodResolver(schema), defaultValues });
  const createDraftMutation = useCreateDraftDocument();
  const updateDraftMutation = useUpdateDraftDocument(documentId);
  const previewMutation = usePreviewDocument(documentId);
  const finalizeMutation = useFinalizeDocument(documentId);
  const downloadMutation = useDownloadDocument(documentId, document?.title ?? defaultValues.title);
  const [content, setContent] = useState(document?.content ?? template.content);
  const [status, setStatus] = useState<DocumentStatus>(document?.status ?? "Draft");
  const [workspaceMode, setWorkspaceMode] = useState<WorkspaceMode>(status === "Finalized" ? "preview" : "edit");
  const [mobilePane, setMobilePane] = useState<MobilePane>(status === "Finalized" ? "preview" : "fields");
  const [finalizeOpen, setFinalizeOpen] = useState(false);
  const [notice, setNotice] = useState(status === "Finalized" ? "Finalized documents are read-only." : "Changes are stored only after you save the Draft.");
  const values = useWatch({ control, name: "values" }) ?? {};
  const title = useWatch({ control, name: "title" });
  const readOnly = status === "Finalized";
  const renderedContent = readOnly
    ? fillTemplate(content, values)
    : previewMutation.data?.renderedContent ?? fillTemplate(content, values);
  const activeMutation = createDraftMutation.isPending || updateDraftMutation.isPending || previewMutation.isPending || finalizeMutation.isPending || downloadMutation.isPending;
  const mutationError = createDraftMutation.error ?? updateDraftMutation.error ?? previewMutation.error ?? finalizeMutation.error ?? downloadMutation.error;

  const saveDraft = handleSubmit((formValues) => {
    const placeholderValues = toPlaceholderInputs(template.placeholders, formValues.values);
    if (!document) {
      createDraftMutation.mutate(
        {
          create: { templateVersionId: template.templateVersionId, title: formValues.title },
          update: { title: formValues.title, content, placeholderValues },
        },
        {
          onSuccess: (createdDocument) => {
            setNotice("Draft created and saved.");
            router.replace(`/documents/${createdDocument.id}`);
          },
        },
      );
      return;
    }

    updateDraftMutation.mutate(
      { title: formValues.title, content, placeholderValues },
      { onSuccess: () => setNotice("Draft saved just now.") },
    );
  });

  const preview = handleSubmit((formValues) => {
    if (!document) return;
    previewMutation.mutate(
      { content, placeholderValues: toPlaceholderInputs(template.placeholders, formValues.values) },
      {
        onSuccess: () => {
          setWorkspaceMode("preview");
          setMobilePane("preview");
          setNotice("Preview generated from your current unsaved values.");
        },
      },
    );
  });

  const requestFinalize = handleSubmit(() => setFinalizeOpen(true));
  const finalize = handleSubmit((formValues) => {
    if (!document) return;
    finalizeMutation.mutate(
      {
        title: formValues.title,
        content,
        placeholderValues: toPlaceholderInputs(template.placeholders, formValues.values),
      },
      {
        onSuccess: (finalizedDocument) => {
          setStatus(finalizedDocument.status);
          setWorkspaceMode("preview");
          setMobilePane("preview");
          setFinalizeOpen(false);
          setNotice("Document finalized. It is now read-only.");
        },
      },
    );
  });

  const download = () => {
    if (!document) return;
    downloadMutation.mutate(undefined, {
      onSuccess: ({ blob, fileName }) => {
        const url = URL.createObjectURL(blob);
        const anchor = window.document.createElement("a");
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        URL.revokeObjectURL(url);
        setNotice(`${fileName} downloaded.`);
      },
    });
  };

  return (
    <div className="space-y-5">
      <Link href="/documents" className="inline-flex min-h-9 items-center gap-2 rounded-md text-sm font-medium text-muted-foreground outline-none hover:text-foreground focus-visible:ring-3 focus-visible:ring-ring/40"><ArrowLeft aria-hidden="true" className="size-4" />Back to my documents</Link>

      <div className="flex flex-col gap-4 border-b pb-5 xl:flex-row xl:items-start xl:justify-between">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2"><h1 className="truncate text-2xl font-semibold tracking-[-0.02em]">{title || "Untitled document"}</h1><StatusBadge status={status} /></div>
          <p className="mt-1.5 text-sm text-muted-foreground">Based on {template.name} · Version {template.versionNumber}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          {workspaceMode === "preview" && !readOnly ? (
            <Button type="button" variant="outline" size="lg" onClick={() => { setWorkspaceMode("edit"); setMobilePane("document"); }} disabled={activeMutation}><FilePenLine aria-hidden="true" />Edit</Button>
          ) : (
            <Button type="button" variant="outline" size="lg" onClick={preview} disabled={readOnly || !document || activeMutation} title={!document ? "Save the Draft before previewing" : undefined}><Eye aria-hidden="true" />{previewMutation.isPending ? "Generating…" : "Preview"}</Button>
          )}
          <Button type="button" variant="outline" size="lg" onClick={saveDraft} disabled={readOnly || activeMutation}><Save aria-hidden="true" />{createDraftMutation.isPending ? "Creating…" : updateDraftMutation.isPending ? "Saving…" : "Save Draft"}</Button>
          <Button type="button" size="lg" onClick={requestFinalize} disabled={readOnly || !document || activeMutation} title={!document ? "Save the Draft before finalizing" : undefined}><Check aria-hidden="true" />Finalize</Button>
          <Button type="button" variant="outline" size="lg" disabled={!document || activeMutation} onClick={download} title={!document ? "Save the Draft before downloading" : undefined}><Download aria-hidden="true" />{downloadMutation.isPending ? "Downloading…" : "Download"}</Button>
        </div>
      </div>

      {!document ? <p className="text-xs text-muted-foreground">Save the Draft once to enable server Preview, Finalize, and HTML Download.</p> : null}
      {readOnly ? (
        <Alert className="border-emerald-200 bg-emerald-50 text-emerald-950"><LockKeyhole aria-hidden="true" /><AlertTitle>Finalized document</AlertTitle><AlertDescription className="text-emerald-800">This document is read-only. Its content and saved placeholder values remain available for historical reference.</AlertDescription></Alert>
      ) : null}
      {mutationError ? (
        <Alert variant="destructive" role="alert"><AlertTitle>The document action could not be completed</AlertTitle><AlertDescription>{getApiErrorMessage(mutationError, "Check the entered values and your connection, then try again.")}</AlertDescription></Alert>
      ) : null}
      <p aria-live="polite" className="text-xs text-muted-foreground">{notice}</p>

      <div className="grid grid-cols-3 rounded-lg bg-muted p-1 lg:hidden" aria-label="Editor panes">
        {(["fields", "document", "preview"] as const).map((pane) => (
          <button key={pane} type="button" onClick={() => { setMobilePane(pane); if (pane === "document") setWorkspaceMode("edit"); }} aria-pressed={mobilePane === pane} disabled={pane === "preview" && !document} className={cn("min-h-10 rounded-md px-2 text-sm font-medium capitalize outline-none focus-visible:ring-3 focus-visible:ring-ring/40 disabled:cursor-not-allowed disabled:opacity-50", mobilePane === pane ? "bg-background text-foreground shadow-sm" : "text-muted-foreground")}>{pane}</button>
        ))}
      </div>

      <div className="grid gap-5 lg:grid-cols-[340px_minmax(0,1fr)]">
        <aside className={cn("rounded-xl border bg-card", mobilePane !== "fields" && "hidden lg:block")}>
          <div className="border-b px-5 py-4"><h2 className="font-semibold">Document fields</h2><p className="mt-1 text-xs leading-5 text-muted-foreground">Complete the values used in this template.</p></div>
          {readOnly ? (
            <dl className="divide-y px-5">{template.placeholders.map((placeholder) => <div key={placeholder.key} className="py-4"><dt className="text-xs font-medium text-muted-foreground">{placeholder.label}</dt><dd className="mt-1 whitespace-pre-wrap break-words text-sm">{values[placeholder.key] || "Not provided"}</dd></div>)}</dl>
          ) : (
            <form className="space-y-5 p-5" onSubmit={(event) => event.preventDefault()} noValidate>
              <div className="space-y-2"><Label htmlFor="document-title">Document title<span aria-hidden="true" className="ml-1 text-red-600">*</span></Label><Input id="document-title" className="h-10" aria-invalid={Boolean(errors.title)} aria-describedby={errors.title ? "document-title-error" : undefined} {...register("title")} />{errors.title ? <p id="document-title-error" className="text-xs font-medium text-red-700">{errors.title.message}</p> : null}</div>
              {template.placeholders.map((placeholder) => <PlaceholderField key={placeholder.key} placeholder={placeholder} register={register} error={errors.values?.[placeholder.key]?.message} />)}
            </form>
          )}
        </aside>

        <section aria-labelledby="document-workspace-heading" className={cn("min-w-0 rounded-xl border bg-slate-100", mobilePane === "fields" && "hidden lg:block")}>
          <div className="flex items-center justify-between border-b bg-card px-4 py-3"><div><h2 id="document-workspace-heading" className="text-sm font-semibold">{workspaceMode === "preview" || readOnly ? "Document preview" : "Document workspace"}</h2><p className="mt-0.5 text-xs text-muted-foreground">{workspaceMode === "preview" || readOnly ? "Preview reflects the current field values." : "Edit the independent Draft content below."}</p></div><span className="hidden text-xs text-muted-foreground sm:inline">HTML document</span></div>
          <div className="p-3 sm:p-5">
            {workspaceMode === "preview" || readOnly ? <DocumentPage content={renderedContent} /> : <div className="mx-auto max-w-[800px]"><Label htmlFor="document-content" className="sr-only">Document content</Label><Textarea id="document-content" value={content} onChange={(event) => setContent(event.target.value)} className="min-h-[720px] resize-y rounded-none border-0 bg-white px-6 py-10 font-serif text-[14px] leading-7 shadow-none ring-1 ring-slate-200 focus-visible:ring-2 sm:px-12 sm:py-14" /></div>}
          </div>
        </section>
      </div>

      <Dialog open={finalizeOpen} onOpenChange={setFinalizeOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>Finalize this document?</DialogTitle><DialogDescription>Finalizing saves the current content and field values, then makes the document permanently read-only.</DialogDescription></DialogHeader>
          <DialogFooter><Button type="button" variant="outline" onClick={() => setFinalizeOpen(false)} disabled={finalizeMutation.isPending}>Keep editing</Button><Button type="button" onClick={finalize} disabled={finalizeMutation.isPending}>{finalizeMutation.isPending ? "Finalizing…" : "Finalize document"}</Button></DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
