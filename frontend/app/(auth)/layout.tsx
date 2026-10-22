import { CheckCircle2, FileText } from "lucide-react";
import type { ReactNode } from "react";

const highlights = [
  "Start from governed, versioned templates",
  "Fill typed fields with instant validation",
  "Preview, finalize and download in one flow",
];

export default function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="grid min-h-screen lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)]">
      <aside className="relative hidden overflow-hidden bg-brand-panel p-12 text-brand-panel-foreground lg:flex lg:flex-col lg:justify-between">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -right-32 -top-32 size-[28rem] rounded-full bg-brand-panel-accent/25 blur-3xl"
        />
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -bottom-40 -left-24 size-[26rem] rounded-full bg-primary/30 blur-3xl"
        />

        <div className="relative flex items-center gap-3">
          <span className="flex size-10 items-center justify-center rounded-lg bg-gradient-to-br from-brand-panel-accent to-primary text-primary-foreground shadow-lg shadow-primary/30">
            <FileText aria-hidden="true" className="size-5" />
          </span>
          <span className="font-heading text-lg font-bold">Document Workspace</span>
        </div>

        <div aria-hidden="true" className="relative mx-auto w-full max-w-sm">
          <div className="absolute inset-x-6 -top-4 h-full rotate-[-4deg] rounded-lg bg-white/10 ring-1 ring-white/10" />
          <div className="relative rounded-lg bg-white p-6 shadow-2xl shadow-black/30">
            <div className="h-3 w-2/5 rounded-full bg-primary/80" />
            <div className="mt-5 space-y-2">
              <div className="h-2 w-full rounded-full bg-slate-200" />
              <div className="h-2 w-11/12 rounded-full bg-slate-200" />
              <div className="h-2 w-4/5 rounded-full bg-slate-200" />
            </div>
            <div className="mt-5 flex flex-wrap gap-2">
              <span className="rounded bg-primary/10 px-2 py-1 font-mono text-[11px] text-primary">{"{{company_name}}"}</span>
              <span className="rounded bg-emerald-500/10 px-2 py-1 font-mono text-[11px] text-emerald-700">{"{{start_date}}"}</span>
            </div>
            <div className="mt-5 space-y-2">
              <div className="h-2 w-full rounded-full bg-slate-200" />
              <div className="h-2 w-3/5 rounded-full bg-slate-200" />
            </div>
          </div>
        </div>

        <div className="relative">
          <h2 className="max-w-md text-3xl font-bold leading-tight tracking-[-0.02em]">
            Professional documents, built from templates you trust.
          </h2>
          <ul className="mt-6 space-y-3 text-sm text-brand-panel-foreground/75">
            {highlights.map((item) => (
              <li key={item} className="flex items-center gap-2.5">
                <CheckCircle2 aria-hidden="true" className="size-4 text-brand-panel-accent" />
                {item}
              </li>
            ))}
          </ul>
        </div>
      </aside>
      <div className="bg-background">{children}</div>
    </div>
  );
}
