import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { clearSession, setSession, setUnauthorizedHandler } from '../auth/session';
import { makeSession } from '../test/fixtures';
import { ApiError, request } from './client';

describe('request', () => {
  const unauthorized = vi.fn();

  beforeEach(() => {
    clearSession();
    setUnauthorizedHandler(unauthorized);
  });

  afterEach(() => {
    setUnauthorizedHandler(undefined);
    vi.unstubAllGlobals();
    vi.clearAllMocks();
  });

  it('attaches the signed-in agent\'s token to the call', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({})));
    vi.stubGlobal('fetch', fetchMock);
    setSession(makeSession({ token: 'abc123' }));

    await request('/tickets');

    const init = fetchMock.mock.calls[0][1] as RequestInit;
    expect(init.headers).toMatchObject({ Authorization: 'Bearer abc123' });
  });

  it('sends no Authorization header when nobody is signed in', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({})));
    vi.stubGlobal('fetch', fetchMock);

    await request('/auth/login');

    const init = fetchMock.mock.calls[0][1] as RequestInit;
    expect(init.headers).not.toHaveProperty('Authorization');
  });

  it('ends the session when the server answers 401 to a signed-in call', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(JSON.stringify({ title: 'Unauthorized' }), { status: 401 })),
    );
    setSession(makeSession());

    await expect(request('/tickets')).rejects.toBeInstanceOf(ApiError);

    expect(unauthorized).toHaveBeenCalledTimes(1);
  });

  it('does not treat a failed sign-in as an expired session', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(JSON.stringify({ title: 'Unauthorized' }), { status: 401 })),
    );

    await expect(request('/auth/login')).rejects.toBeInstanceOf(ApiError);

    expect(unauthorized).not.toHaveBeenCalled();
  });
});
