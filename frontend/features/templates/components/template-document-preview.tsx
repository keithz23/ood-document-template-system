export function TemplateDocumentPreview({
  content,
  title,
}: {
  content: string;
  title: string;
}) {
  return (
    <div className="bg-dotted rounded-xl border bg-muted/60 p-3 sm:p-8">
      <iframe
        title={`${title} template preview`}
        sandbox=""
        srcDoc={content}
        className="mx-auto min-h-[580px] w-full max-w-[720px] rounded-sm bg-card shadow-lg shadow-black/5 ring-1 ring-border"
      />
    </div>
  );
}
