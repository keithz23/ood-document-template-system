"use client";

import Link from "next/link";
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
import { cn } from "@/lib/utils";
import type { DocumentRecord, DocumentStatus, PlaceholderDefinition, TemplateDetail } from "@/types/mock-data";

type EditorFormValues = {
  title: string;
  values: Record<string, string>;
};

type WorkspaceMode = "edit" | "preview";
type MobilePane = "fields" | "document" | "preview";

function createEditorSchema(placeholders: readonly PlaceholderDefinition[]) {
  return z.object({
    title: z.string().trim().min(1, "Document title is required."),
    values: z.record(z.string(), z.string()),
  }).superRefine(({ values }, context) => {
    placeholders.forEach((placeholder) => {
      const value = values[placeholder.key]?.trim() ?? "";
      if (placeholder.isRequired && value.length === 0) {
        context.addIssue({ code: "custom", message: `${placeholder.label} is required.`, path: ["values", placeholder.key] });
        return;
      }
      if (!value) return;
      if (placeholder.dataType === "Number" && !Number.isFinite(Number(value))) {
        context.addIssue({ code: "custom", message: "Enter a valid number.", path: ["values", placeholder.key] });
      }
      if (placeholder.dataType === "Email" && !z.email().safeParse(value).success) {
        context.addIssue({ code: "custom", message: "Enter a valid email address.", path: ["values", placeholder.key] });
      }
      if (placeholder.dataType === "Date" && Number.isNaN(Date.parse(value))) {
        context.addIssue({ code: "custom", message: "Enter a valid date.", path: ["values", placeholder.key] });
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

function isLongTextField(placeholder: PlaceholderDefinition) {
  return placeholder.dataType === "Text" && ["description", "summary", "experience", "outcomes", "risks", "priorities", "items", "decisions", "skills", "education"].some((term) => placeholder.key.includes(term));
}

function PlaceholderField({ placeholder, register, error }: {
  placeholder: PlaceholderDefinition;
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
    placeholder: placeholder.example ?? placeholder.defaultValue ?? undefined,
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
    <div className="mx-auto min-h-[720px] w-full max-w-[800px] bg-white px-6 py-10 text-slate-900 ring-1 ring-slate-200 sm:px-12 sm:py-14">
      <div className="whitespace-pre-wrap font-serif text-[14px] leading-7">{content}</div>
    </div>
  );
}

export function DocumentEditor({ template, document }: { template: TemplateDetail; document?: DocumentRecord }) {
  const schema = useMemo(() => createEditorSchema(template.placeholders), [template.placeholders]);
  const defaultValues = useMemo<EditorFormValues>(() => ({
    title: document?.title ?? `Untitled ${template.name}`,
    values: Object.fromEntries(template.placeholders.map((placeholder) => [placeholder.key, document?.placeholderValues[placeholder.key] ?? placeholder.defaultValue ?? ""])),
  }), [document, template]);
  const { register, handleSubmit, control, formState: { errors } } = useForm<EditorFormValues>({ resolver: zodResolver(schema), defaultValues });
  const [content, setContent] = useState(document?.content ?? template.content);
  const [status, setStatus] = useState<DocumentStatus>(document?.status ?? "Draft");
  const [workspaceMode, setWorkspaceMode] = useState<WorkspaceMode>(status === "Finalized" ? "preview" : "edit");
  const [mobilePane, setMobilePane] = useState<MobilePane>(status === "Finalized" ? "preview" : "fields");
  const [finalizeOpen, setFinalizeOpen] = useState(false);
  const [notice, setNotice] = useState(status === "Draft" ? "Draft changes are stored in this mock session only." : "Finalized documents are read-only.");
  const values = useWatch({ control, name: "values" });
  const title = useWatch({ control, name: "title" });
  const renderedContent = fillTemplate(content, values ?? {});
  const readOnly = status === "Finalized";

  const saveDraft = handleSubmit(() => setNotice("Draft saved just now. Mock data resets when the page reloads."));
  const requestFinalize = handleSubmit(() => setFinalizeOpen(true));
  const finalize = () => {
    setStatus("Finalized");
    setWorkspaceMode("preview");
    setMobilePane("preview");
    setFinalizeOpen(false);
    setNotice("Document finalized. It is now read-only in this mock session.");
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
          <Button type="button" variant="outline" size="lg" onClick={() => { setWorkspaceMode((current) => current === "edit" ? "preview" : "edit"); setMobilePane((current) => current === "preview" ? "document" : "preview"); }} disabled={readOnly}>
            {workspaceMode === "preview" ? <FilePenLine aria-hidden="true" /> : <Eye aria-hidden="true" />}{workspaceMode === "preview" ? "Edit" : "Preview"}
          </Button>
          <Button type="button" variant="outline" size="lg" onClick={saveDraft} disabled={readOnly}><Save aria-hidden="true" />Save draft</Button>
          <Button type="button" size="lg" onClick={requestFinalize} disabled={readOnly}><Check aria-hidden="true" />Finalize</Button>
          <Button type="button" variant="outline" size="lg" disabled={!readOnly} aria-describedby={!readOnly ? "download-availability" : undefined} title={!readOnly ? "Download is available after finalization" : undefined} onClick={() => setNotice("Download is a mock action; no export format has been selected.")}><Download aria-hidden="true" />Download</Button>
        </div>
      </div>

      {!readOnly ? <span id="download-availability" className="sr-only">Download is available after the document is finalized.</span> : null}

      {readOnly ? (
        <Alert className="border-emerald-200 bg-emerald-50 text-emerald-950"><LockKeyhole aria-hidden="true" /><AlertTitle>Finalized document</AlertTitle><AlertDescription className="text-emerald-800">This document is read-only. Its content and saved placeholder values remain available for historical reference.</AlertDescription></Alert>
      ) : null}
      <p aria-live="polite" className="text-xs text-muted-foreground">{notice}</p>

      <div className="grid grid-cols-3 rounded-lg bg-muted p-1 lg:hidden" aria-label="Editor panes">
        {(["fields", "document", "preview"] as const).map((pane) => (
          <button key={pane} type="button" onClick={() => { setMobilePane(pane); if (pane !== "fields") setWorkspaceMode(pane === "preview" ? "preview" : "edit"); }} aria-pressed={mobilePane === pane} className={cn("min-h-10 rounded-md px-2 text-sm font-medium capitalize outline-none focus-visible:ring-3 focus-visible:ring-ring/40", mobilePane === pane ? "bg-background text-foreground shadow-sm" : "text-muted-foreground")}>{pane}</button>
        ))}
      </div>

      <div className="grid gap-5 lg:grid-cols-[340px_minmax(0,1fr)]">
        <aside className={cn("rounded-xl border bg-card", mobilePane !== "fields" && "hidden lg:block")}> 
          <div className="border-b px-5 py-4"><h2 className="font-semibold">Document fields</h2><p className="mt-1 text-xs leading-5 text-muted-foreground">Complete the values used in this template.</p></div>
          {readOnly ? (
            <dl className="divide-y px-5">{template.placeholders.map((placeholder) => <div key={placeholder.key} className="py-4"><dt className="text-xs font-medium text-muted-foreground">{placeholder.label}</dt><dd className="mt-1 whitespace-pre-wrap text-sm">{values?.[placeholder.key] || "Not provided"}</dd></div>)}</dl>
          ) : (
            <form className="space-y-5 p-5" onSubmit={(event) => event.preventDefault()} noValidate>
              <div className="space-y-2"><Label htmlFor="document-title">Document title<span aria-hidden="true" className="ml-1 text-red-600">*</span></Label><Input id="document-title" className="h-10" aria-invalid={Boolean(errors.title)} aria-describedby={errors.title ? "document-title-error" : undefined} {...register("title")} />{errors.title ? <p id="document-title-error" className="text-xs font-medium text-red-700">{errors.title.message}</p> : null}</div>
              {template.placeholders.map((placeholder) => <PlaceholderField key={placeholder.key} placeholder={placeholder} register={register} error={errors.values?.[placeholder.key]?.message} />)}
            </form>
          )}
        </aside>

        <section aria-labelledby="document-workspace-heading" className={cn("min-w-0 rounded-xl border bg-slate-100", mobilePane === "fields" && "hidden lg:block")}>
          <div className="flex items-center justify-between border-b bg-card px-4 py-3"><div><h2 id="document-workspace-heading" className="text-sm font-semibold">{workspaceMode === "preview" || readOnly ? "Document preview" : "Document workspace"}</h2><p className="mt-0.5 text-xs text-muted-foreground">{workspaceMode === "preview" || readOnly ? "Preview reflects the current field values." : "Edit the independent draft content below."}</p></div><span className="hidden text-xs text-muted-foreground sm:inline">HTML-based content · mock editor</span></div>
          <div className="p-3 sm:p-5">
            {workspaceMode === "preview" || readOnly ? <DocumentPage content={renderedContent} /> : <div className="mx-auto max-w-[800px]"><Label htmlFor="document-content" className="sr-only">Document content</Label><Textarea id="document-content" value={content} onChange={(event) => setContent(event.target.value)} className="min-h-[720px] resize-y rounded-none border-0 bg-white px-6 py-10 font-serif text-[14px] leading-7 shadow-none ring-1 ring-slate-200 focus-visible:ring-2 sm:px-12 sm:py-14" /></div>}
          </div>
        </section>
      </div>

      <Dialog open={finalizeOpen} onOpenChange={setFinalizeOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>Finalize this document?</DialogTitle><DialogDescription>Finalizing changes the document from Draft to Finalized. Finalized documents are read-only.</DialogDescription></DialogHeader>
          <DialogFooter><Button type="button" variant="outline" onClick={() => setFinalizeOpen(false)}>Keep editing</Button><Button type="button" onClick={finalize}>Finalize document</Button></DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
