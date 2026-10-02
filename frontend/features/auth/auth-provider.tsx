"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  clearStoredSession,
  getStoredSession,
  storeSession,
  type AuthSession,
  unauthorizedEventName,
} from "@/features/auth/auth-storage";
import { getCurrentUser } from "@/services/auth-api";
import type { Permission } from "@/types/api";

type AuthContextValue = Readonly<{
  session: AuthSession | null;
  isReady: boolean;
  establishSession: (session: AuthSession) => void;
  clearSession: () => void;
  logout: () => void;
  hasPermission: (permission: Permission) => boolean;
}>;

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [session, setSession] = useState<AuthSession | null>(null);
  const [isReady, setIsReady] = useState(false);

  const currentUserQuery = useQuery({
    queryKey: ["auth", "me"],
    queryFn: getCurrentUser,
    enabled: isReady && Boolean(session),
    staleTime: 60_000,
  });

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setSession(getStoredSession());
      setIsReady(true);
    }, 0);

    return () => window.clearTimeout(timeout);
  }, []);

  const establishSession = useCallback((nextSession: AuthSession) => {
    storeSession(nextSession);
    setSession(nextSession);
  }, []);

  const clearSession = useCallback(() => {
    clearStoredSession();
    setSession(null);
    queryClient.clear();
  }, [queryClient]);

  const logout = useCallback(() => {
    clearStoredSession();
    queryClient.clear();
    window.location.replace("/login");
  }, [queryClient]);

  useEffect(() => {
    const handleUnauthorized = (event: Event) => {
      const { returnTo } = (event as CustomEvent<{ returnTo: string }>).detail;
      clearSession();
      router.replace(`/login?reason=session-expired&returnTo=${encodeURIComponent(returnTo)}`);
    };

    window.addEventListener(unauthorizedEventName, handleUnauthorized);
    return () => window.removeEventListener(unauthorizedEventName, handleUnauthorized);
  }, [clearSession, router]);

  const effectiveSession = useMemo(
    () => session && currentUserQuery.data
      ? { ...session, user: currentUserQuery.data }
      : session,
    [currentUserQuery.data, session],
  );
  const hasPermission = useCallback(
    (permission: Permission) =>
      Boolean(effectiveSession?.user.permissions?.includes(permission)),
    [effectiveSession],
  );
  const value = useMemo(
    () => ({
      session: effectiveSession,
      isReady,
      establishSession,
      clearSession,
      logout,
      hasPermission,
    }),
    [clearSession, effectiveSession, establishSession, hasPermission, isReady, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider.");
  }

  return context;
}
