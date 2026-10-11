import { afterEach, describe, expect, it, vi } from 'vitest';
import { emptyFilters } from '../features/tickets/hooks/useTicketList';
import { ticketsApi } from './tickets';

function stubFetch() {
  const fetchMock = vi.fn().mockResolvedValue({
    ok: true,
    status: 200,
    json: () => Promise.resolve({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }),
  });
  vi.stubGlobal('fetch', fetchMock);

  return fetchMock;
}

describe('ticketsApi.getTickets', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('sends the SLA status filter', async () => {
    const fetchMock = stubFetch();

    await ticketsApi.getTickets({ ...emptyFilters, slaStatus: 'AtRisk' });

    expect(String(fetchMock.mock.calls[0]?.[0])).toContain('slaStatus=AtRisk');
  });

  it('leaves the SLA status out when none is chosen', async () => {
    const fetchMock = stubFetch();

    await ticketsApi.getTickets(emptyFilters);

    expect(String(fetchMock.mock.calls[0]?.[0])).not.toContain('slaStatus');
  });
});
