import { useCallback, useEffect, useMemo, useState } from 'react';
import { authApi } from '../api/auth';
import type { AuthenticatedAgent } from '../types/api';
import { AuthContext } from './authContext';
import { clearSession, getSession, setSession, setUnauthorizedHandler } from './session';

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [agent, setAgent] = useState<AuthenticatedAgent | undefined>(() => getSession()?.agent);

  const logout = useCallback(() => {
    clearSession();
    setAgent(undefined);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const result = await authApi.login({ email, password });

    // The API names the signed-in agent "user"; inside the app it is the session's agent.
    setSession({ token: result.token, expiresAtUtc: result.expiresAtUtc, agent: result.user });
    setAgent(result.user);
  }, []);

  // AU-13: any 401 on a signed-in session ends it; the protected routes then send the user to login.
  useEffect(() => {
    setUnauthorizedHandler(logout);

    return () => setUnauthorizedHandler(undefined);
  }, [logout]);

  const value = useMemo(() => ({ agent, login, logout }), [agent, login, logout]);

  return <AuthContext value={value}>{children}</AuthContext>;
}
