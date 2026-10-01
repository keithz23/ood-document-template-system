"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  FileClock,
  Files,
  FolderCog,
  Menu,
  PanelLeft,
  Tags,
  UserRound,
} from "lucide-react";
import { useState, type ReactNode } from "react";
import { Button } from "@/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { cn } from "@/lib/utils";
import { useAuth } from "@/features/auth/auth-provider";

const navigation = [
  { href: "/templates", label: "Templates", icon: Files },
  { href: "/documents", label: "My documents", icon: FileClock },
] as const;

const adminNavigation = [
  { href: "/admin/categories", label: "Categories", icon: Tags },
  { href: "/admin/templates", label: "Manage templates", icon: FolderCog },
] as const;

function ProductMark() {
  return (
    <Link
      href="/templates"
      className="flex items-center gap-2.5 rounded-md focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/40"
    >
      <span className="flex size-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
        <PanelLeft aria-hidden="true" className="size-4" />
      </span>
      <span className="text-sm font-semibold tracking-[-0.01em]">
        Document Workspace
      </span>
    </Link>
  );
}

function Navigation({
  pathname,
  isAdmin,
  onNavigate,
}: {
  pathname: string;
  isAdmin: boolean;
  onNavigate?: () => void;
}) {
  const renderLink = (
    item: (typeof navigation)[number] | (typeof adminNavigation)[number],
  ) => {
    const isActive =
      pathname === item.href || pathname.startsWith(`${item.href}/`);
    const Icon = item.icon;

    return (
      <Link
        key={item.href}
        href={item.href}
        onClick={onNavigate}
        aria-current={isActive ? "page" : undefined}
        className={cn(
          "flex min-h-10 items-center gap-3 rounded-lg px-3 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/40",
          isActive
            ? "bg-sidebar-accent text-sidebar-accent-foreground"
            : "text-muted-foreground hover:bg-sidebar-accent/70 hover:text-foreground",
        )}
      >
        <Icon aria-hidden="true" className="size-4" />
        {item.label}
      </Link>
    );
  };

  return (
    <nav aria-label="Primary navigation" className="space-y-5">
      <div className="space-y-1">{navigation.map(renderLink)}</div>
      {isAdmin ? (
        <div className="border-t pt-4">
          <p className="mb-2 px-3 text-xs font-semibold uppercase tracking-[0.08em] text-muted-foreground">
            Administration
          </p>
          <div className="space-y-1">{adminNavigation.map(renderLink)}</div>
        </div>
      ) : null}
    </nav>
  );
}

function UserIdentity() {
  const { session } = useAuth();
  const user = session?.user;

  return (
    <div className="flex items-center gap-3 border-t px-3 pt-4">
      <span className="flex size-8 items-center justify-center rounded-full bg-muted text-muted-foreground">
        <UserRound aria-hidden="true" className="size-4" />
      </span>
      <div className="min-w-0">
        <p className="truncate text-sm font-medium">
          {user?.fullName ?? "Signed-in user"}
        </p>
        <p className="text-xs text-muted-foreground">
          {user?.role === "Admin" ? "Admin workspace" : "Author workspace"}
        </p>
      </div>
    </div>
  );
}

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const { session } = useAuth();
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <div className="min-h-screen bg-background">
      <a
        href="#main-content"
        className="fixed left-3 top-3 z-[70] -translate-y-20 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground focus:translate-y-0"
      >
        Skip to content
      </a>

      <aside className="fixed inset-y-0 left-0 z-30 hidden w-60 flex-col border-r bg-sidebar md:flex">
        <div className="flex h-16 items-center border-b px-5">
          <ProductMark />
        </div>
        <div className="flex flex-1 flex-col justify-between gap-8 px-3 py-4">
          <Navigation
            pathname={pathname}
            isAdmin={session?.user.role === "Admin"}
          />
          <UserIdentity />
        </div>
      </aside>

      <header className="sticky top-0 z-20 flex h-14 items-center justify-between border-b bg-background/95 px-4 supports-[backdrop-filter]:backdrop-blur-sm md:hidden">
        <ProductMark />
        <Button
          type="button"
          size="icon-lg"
          variant="ghost"
          aria-label="Open navigation"
          onClick={() => setMobileOpen(true)}
        >
          <Menu aria-hidden="true" />
        </Button>
      </header>

      <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
        <SheetContent side="left" className="w-[min(84vw,20rem)] bg-sidebar">
          <SheetHeader className="border-b py-5">
            <SheetTitle>
              <ProductMark />
            </SheetTitle>
            <SheetDescription className="sr-only">
              Primary navigation
            </SheetDescription>
          </SheetHeader>
          <div className="flex flex-1 flex-col justify-between gap-8 px-3 pb-4">
            <Navigation
              pathname={pathname}
              isAdmin={session?.user.role === "Admin"}
              onNavigate={() => setMobileOpen(false)}
            />
            <UserIdentity />
          </div>
        </SheetContent>
      </Sheet>

      <main id="main-content" className="md:pl-60">
        <div className="mx-auto w-full max-w-[1440px] px-4 py-6 sm:px-6 sm:py-8 lg:px-8">
          {children}
        </div>
      </main>
    </div>
  );
}
