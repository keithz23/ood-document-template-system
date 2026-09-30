import type { LoginResponseDto } from "@/types/api";

const storageKey = "document-template-system.auth";
export const unauthorizedEventName = "document-template-system.unauthorized";

export type AuthSession = LoginResponseDto;

export function getStoredSession(): AuthSession | null {
  if (typeof window === "undefined") return null;

  try {
    const value = window.localStorage.getItem(storageKey);
    if (!value) return null;

    const session = JSON.parse(value) as AuthSession;
    if (
      !session.accessToken ||
      !session.expiresAt ||
      new Date(session.expiresAt).getTime() <= Date.now()
    ) {
      clearStoredSession();
      return null;
    }

    return session;
  } catch {
    clearStoredSession();
    return null;
  }
}

export function storeSession(session: AuthSession) {
  window.localStorage.setItem(storageKey, JSON.stringify(session));
}

export function clearStoredSession() {
  if (typeof window !== "undefined") {
    window.localStorage.removeItem(storageKey);
  }
}

export function notifyUnauthorized(returnTo: string) {
  window.dispatchEvent(
    new CustomEvent(unauthorizedEventName, { detail: { returnTo } }),
  );
}
