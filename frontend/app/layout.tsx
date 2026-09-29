import type { Metadata } from "next";
import { AppProviders } from "@/components/shared/app-providers";
import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "Document Workspace",
    template: "%s · Document Workspace",
  },
  description: "Create documents from reusable, versioned templates.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className="h-full antialiased" data-scroll-behavior="smooth">
      <body className="flex min-h-full flex-col">
        <AppProviders>{children}</AppProviders>
      </body>
    </html>
  );
}
