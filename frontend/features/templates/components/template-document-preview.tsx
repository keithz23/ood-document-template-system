export function TemplateDocumentPreview({
  content,
  title,
}: {
  content: string;
  title: string;
}) {
  return (
    <div className="rounded-xl border bg-slate-100 p-3 sm:p-6">
      <iframe
        title={`${title} template preview`}
        sandbox=""
        srcDoc={content}
        className="mx-auto min-h-[580px] w-full max-w-[720px] bg-white ring-1 ring-slate-200"
      />
    </div>
  );
}
