import type { AuthenticatedAgent } from '../types/api';

const STORAGE_KEY = 'northwind.session';

export interface Session {
  token: string;
  expiresAtUtc: string;
  agent: AuthenticatedAgent;
}

// The session lives in memory, and is mirrored to localStorage so a reload or a second tab
// stays signed in. If storage is blocked (private window), the session simply lasts until reload.
let current: Session | undefined;
let isLoaded = false;
let unauthorizedHandler: (() => void) | undefined;

function isSession(value: unknown): value is Session {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const candidate = value as Partial<Record<keyof Session, unknown>>;

  return (
    typeof candidate.token === 'string' &&
    typeof candidate.expiresAtUtc === 'string' &&
    typeof candidate.agent === 'object' &&
    candidate.agent !== null
  );
}

function readStored(): Session | undefined {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);

    if (raw === null) {
      return undefined;
    }

    const parsed: unknown = JSON.parse(raw);

    if (isSession(parsed) && Date.parse(parsed.expiresAtUtc) > Date.now()) {
      return parsed;
    }

    window.localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Storage unavailable or the stored value is corrupt: start signed out.
  }

  return undefined;
}

export function getSession(): Session | undefined {
  if (!isLoaded) {
    current = readStored();
    isLoaded = true;
  }

  return current;
}

export function setSession(session: Session): void {
  current = session;
  isLoaded = true;

  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  } catch {
    // Kept in memory only.
  }
}

export function clearSession(): void {
  current = undefined;
  isLoaded = true;

  try {
    window.localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Nothing stored that we could remove.
  }
}

/** The signed-in provider registers what should happen when the server rejects the token. */
export function setUnauthorizedHandler(handler: (() => void) | undefined): void {
  unauthorizedHandler = handler;
}

export function notifyUnauthorized(): void {
  unauthorizedHandler?.();
}
