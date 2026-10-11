import { createContext } from 'react';
import type { AuthenticatedAgent } from '../types/api';

export interface AuthContextValue {
  /** The signed-in agent, or undefined when nobody is signed in. */
  agent: AuthenticatedAgent | undefined;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);
