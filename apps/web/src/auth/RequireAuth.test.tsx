import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';
import { makeSession } from '../test/fixtures';
import { AuthProvider } from './AuthProvider';
import { RequireAuth } from './RequireAuth';
import { clearSession, setSession } from './session';

function renderAt(path: string) {
  return render(
    <AuthProvider>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/login" element={<p>Login screen</p>} />
          <Route element={<RequireAuth />}>
            <Route path="/tickets" element={<p>Ticket list page</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  );
}

describe('RequireAuth', () => {
  afterEach(() => {
    clearSession();
  });

  it('sends a visitor who is not signed in to the login page', () => {
    clearSession();

    renderAt('/tickets');

    expect(screen.getByText('Login screen')).toBeInTheDocument();
    expect(screen.queryByText('Ticket list page')).not.toBeInTheDocument();
  });

  it('shows the page to a signed-in agent', () => {
    setSession(makeSession());

    renderAt('/tickets');

    expect(screen.getByText('Ticket list page')).toBeInTheDocument();
  });
});
