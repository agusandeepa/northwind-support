import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';
import { App } from './App';
import { AuthProvider } from './auth/AuthProvider';
import { RequireAuth } from './auth/RequireAuth';
import { clearSession, getSession, setSession } from './auth/session';
import { makeSession } from './test/fixtures';

describe('App', () => {
  afterEach(() => {
    clearSession();
  });

  it('shows who is signed in, and signing out clears the session', async () => {
    setSession(makeSession());

    render(
      <AuthProvider>
        <MemoryRouter initialEntries={['/tickets']}>
          <Routes>
            <Route path="/login" element={<p>Login screen</p>} />
            <Route element={<RequireAuth />}>
              <Route element={<App />}>
                <Route path="/tickets" element={<p>Ticket list page</p>} />
              </Route>
            </Route>
          </Routes>
        </MemoryRouter>
      </AuthProvider>,
    );

    expect(screen.getByText('Alex Turner')).toBeInTheDocument();

    await userEvent.setup().click(screen.getByRole('button', { name: 'Sign out' }));

    expect(await screen.findByText('Login screen')).toBeInTheDocument();
    expect(getSession()).toBeUndefined();
  });
});
