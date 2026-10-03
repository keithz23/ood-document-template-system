"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  FileClock,
  Files,
  FolderCog,
  LogOut,
  Menu,
  PanelLeft,
  ScrollText,
  Tags,
  UserRound,
  UsersRound,
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
import type { Permission } from "@/types/api";

const navigation = [
  { href: "/templates", label: "Templates", icon: Files, permission: "Templates.View" },
  { href: "/documents", label: "My documents", icon: FileClock, permission: "Documents.ViewOwn" },
] as const;

const accountNavigation = [
  { href: "/profile", label: "Profile", icon: UserRound },
] as const;

const adminNavigation = [
  { href: "/admin/categories", label: "Categories", icon: Tags, permission: "Templates.Manage" },
  { href: "/admin/templates", label: "Manage templates", icon: FolderCog, permission: "Templates.Manage" },
  { href: "/admin/users", label: "Users", icon: UsersRound, permission: "Users.View" },
  { href: "/admin/audit-logs", label: "Audit logs", icon: ScrollText, permission: "AuditLogs.View" },
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
  hasPermission,
  onNavigate,
}: {
  pathname: string;
  hasPermission: (permission: Permission) => boolean;
  onNavigate?: () => void;
}) {
  const renderLink = (
    item: (typeof navigation)[number] | (typeof adminNavigation)[number] | (typeof accountNavigation)[number],
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
      <div className="space-y-1">
        {navigation.filter((item) => hasPermission(item.permission)).map(renderLink)}
        {accountNavigation.map(renderLink)}
      </div>
      {adminNavigation.some((item) => hasPermission(item.permission)) ? (
        <div className="border-t pt-4">
          <p className="mb-2 px-3 text-xs font-semibold uppercase tracking-[0.08em] text-muted-foreground">
            Administration
          </p>
          <div className="space-y-1">
            {adminNavigation.filter((item) => hasPermission(item.permission)).map(renderLink)}
          </div>
        </div>
      ) : null}
    </nav>
  );
}

function UserIdentity() {
  const { session, logout } = useAuth();
  const user = session?.user;

  return (
    <div className="flex items-center gap-3 border-t px-3 pt-4">
      <span className="flex size-8 items-center justify-center rounded-full bg-muted text-muted-foreground">
        <UserRound aria-hidden="true" className="size-4" />
      </span>
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium">
          {user?.fullName ?? "Signed-in user"}
        </p>
        <p className="text-xs text-muted-foreground">
          {user?.role === "Admin" ? "Admin workspace" : "Author workspace"}
        </p>
      </div>
      <Button
        type="button"
        size="icon-sm"
        variant="ghost"
        aria-label="Sign out"
        title="Sign out"
        onClick={() => logout()}
      >
        <LogOut aria-hidden="true" />
      </Button>
    </div>
  );
}

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const { hasPermission } = useAuth();
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
            hasPermission={hasPermission}
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
              hasPermission={hasPermission}
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
