import type { TemplateDetail } from "@/types/mock-data";

export function TemplateDocumentPreview({
  template,
}: {
  template: TemplateDetail;
}) {
  return (
    <div className="rounded-xl border bg-slate-100 p-3 sm:p-6">
      <div className="mx-auto min-h-[580px] max-w-[720px] bg-white px-6 py-10 ring-1 ring-slate-200 sm:px-12 sm:py-14">
        <div className="whitespace-pre-wrap font-serif text-[13px] leading-7 text-slate-800">
          {template.content}
        </div>
      </div>
    </div>
  );
}
