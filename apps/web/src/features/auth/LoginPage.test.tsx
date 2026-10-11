import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, type InitialEntry } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { authApi } from '../../api/auth';
import { ApiError } from '../../api/client';
import { AuthProvider } from '../../auth/AuthProvider';
import { clearSession, getSession, type Session } from '../../auth/session';
import { makeSession } from '../../test/fixtures';
import type { LoginResult } from '../../types/api';
import { LoginPage } from './LoginPage';

vi.mock('../../api/auth', () => ({
  authApi: {
    login: vi.fn(),
  },
}));

const login = vi.mocked(authApi.login);

/** The API calls the signed-in agent "user"; the app keeps it as the session's agent. */
function loginResult(session: Session): LoginResult {
  return { token: session.token, expiresAtUtc: session.expiresAtUtc, user: session.agent };
}

function renderLogin(entry: InitialEntry = '/login') {
  return render(
    <AuthProvider>
      <MemoryRouter initialEntries={[entry]}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/tickets" element={<p>Ticket list page</p>} />
          <Route path="/tickets/:id" element={<p>Ticket detail page</p>} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  );
}

async function signIn(email: string, password: string) {
  const user = userEvent.setup();

  await user.type(screen.getByLabelText('Email'), email);
  await user.type(screen.getByLabelText('Password'), password);
  await user.click(screen.getByRole('button', { name: 'Sign in' }));
}

describe('LoginPage', () => {
  afterEach(() => {
    clearSession();
    vi.clearAllMocks();
  });

  it('blocks an invalid email and never calls the API', async () => {
    renderLogin();

    await signIn('not-an-email', 'secret');

    expect(screen.getByText('Enter a valid email address.')).toBeInTheDocument();
    expect(login).not.toHaveBeenCalled();
  });

  it('shows one clear error for rejected credentials and stores no session', async () => {
    login.mockRejectedValue(new ApiError(401, 'Invalid email or password.'));
    renderLogin();

    await signIn('alex.turner@northwind-support.example', 'wrong');

    expect(await screen.findByRole('alert')).toHaveTextContent('Incorrect email or password.');
    expect(getSession()).toBeUndefined();
  });

  it('signs in and lands on the ticket list by default', async () => {
    const session = makeSession({ token: 'jwt-token' });
    login.mockResolvedValue(loginResult(session));
    renderLogin();

    await signIn('alex.turner@northwind-support.example', 'LocalDevOnly!1');

    expect(await screen.findByText('Ticket list page')).toBeInTheDocument();
    expect(getSession()?.token).toBe('jwt-token');
  });

  it('returns the visitor to the page they originally wanted', async () => {
    login.mockResolvedValue(loginResult(makeSession()));
    renderLogin({ pathname: '/login', state: { from: { pathname: '/tickets/12' } } });

    await signIn('alex.turner@northwind-support.example', 'LocalDevOnly!1');

    expect(await screen.findByText('Ticket detail page')).toBeInTheDocument();
  });
});
