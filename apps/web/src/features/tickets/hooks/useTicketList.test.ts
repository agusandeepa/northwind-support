import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ticketsApi } from '../../../api/tickets';
import { page, ticketListItem } from '../../../test/fixtures';
import { useTicketList } from './useTicketList';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: {
    getTickets: vi.fn(),
  },
}));

const getTickets = vi.mocked(ticketsApi.getTickets);

describe('useTicketList', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    getTickets.mockResolvedValue(page([ticketListItem()]));
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.clearAllMocks();
  });

  it('searches with the text the user typed, once the typing settles', async () => {
    const { result } = renderHook(() => useTicketList());

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    expect(getTickets).toHaveBeenCalledTimes(1);

    act(() => {
      result.current.updateFilters({ search: 'invoice' });
    });

    // Still inside the debounce window: no second request yet.
    expect(getTickets).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    expect(getTickets).toHaveBeenCalledTimes(2);
    expect(getTickets).toHaveBeenLastCalledWith(
      expect.objectContaining({ search: 'invoice', page: 1 }),
    );

    expect(result.current.isLoading).toBe(false);
    expect(result.current.data?.items).toHaveLength(1);
  });

  describe('DEFECT-117: every filter change reaches the API', () => {
    const settle = async () => {
      await act(async () => {
        await vi.advanceTimersByTimeAsync(300);
      });
    };

    it('requests immediately when the status changes, without waiting for a search', async () => {
      const { result } = renderHook(() => useTicketList());
      await settle();
      getTickets.mockClear();

      act(() => {
        result.current.updateFilters({ status: 'InProgress' });
      });
      await act(async () => {
        await vi.advanceTimersByTimeAsync(0);
      });

      expect(getTickets).toHaveBeenCalledTimes(1);
      expect(getTickets).toHaveBeenLastCalledWith(
        expect.objectContaining({ status: 'InProgress', search: '', page: 1 }),
      );
    });

    it.each([
      ['priority', { priority: 'High' }],
      ['categoryId', { categoryId: 3 }],
      ['customerId', { customerId: 2 }],
      ['assignedAgentId', { assignedAgentId: 4 }],
      ['unassignedOnly', { unassignedOnly: true }],
      ['sortBy', { sortBy: 'dueAtUtc', sortDirection: 'asc' }],
    ] as const)('requests immediately when %s changes', async (_name, patch) => {
      const { result } = renderHook(() => useTicketList());
      await settle();
      getTickets.mockClear();

      act(() => {
        result.current.updateFilters(patch);
      });
      await act(async () => {
        await vi.advanceTimersByTimeAsync(0);
      });

      expect(getTickets).toHaveBeenCalledTimes(1);
      expect(getTickets).toHaveBeenLastCalledWith(expect.objectContaining(patch));
    });

    it('keeps the chosen status when the search is typed and when it is cleared', async () => {
      const { result } = renderHook(() => useTicketList());
      await settle();

      act(() => {
        result.current.updateFilters({ status: 'InProgress' });
      });
      await settle();

      act(() => {
        result.current.updateFilters({ search: 'invoice' });
      });
      await settle();
      expect(getTickets).toHaveBeenLastCalledWith(
        expect.objectContaining({ status: 'InProgress', search: 'invoice' }),
      );

      act(() => {
        result.current.updateFilters({ search: '' });
      });
      await settle();
      expect(getTickets).toHaveBeenLastCalledWith(
        expect.objectContaining({ status: 'InProgress', search: '' }),
      );
    });

    it('sends one request for a burst of keystrokes, not one per keystroke', async () => {
      const { result } = renderHook(() => useTicketList());
      await settle();
      getTickets.mockClear();

      for (const text of ['i', 'in', 'inv', 'invo']) {
        act(() => {
          result.current.updateFilters({ search: text });
        });
        await act(async () => {
          await vi.advanceTimersByTimeAsync(100);
        });
      }
      expect(getTickets).not.toHaveBeenCalled();

      await settle();

      expect(getTickets).toHaveBeenCalledTimes(1);
      expect(getTickets).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'invo' }));
    });

    it('does not send an extra request for the page reset caused by typing in search', async () => {
      const { result } = renderHook(() => useTicketList());
      await settle();

      act(() => {
        result.current.updateFilters({ page: 3 });
      });
      await settle();
      getTickets.mockClear();

      act(() => {
        result.current.updateFilters({ search: 'x' });
      });
      await act(async () => {
        await vi.advanceTimersByTimeAsync(0);
      });
      expect(getTickets).not.toHaveBeenCalled();

      await settle();
      expect(getTickets).toHaveBeenCalledTimes(1);
      expect(getTickets).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'x', page: 1 }));
    });

    it('goes back to the first page and clears everything on reset', async () => {
      const { result } = renderHook(() => useTicketList());
      await settle();

      act(() => {
        result.current.updateFilters({ status: 'Open', priority: 'High' });
      });
      await settle();
      getTickets.mockClear();

      act(() => {
        result.current.resetFilters();
      });
      await act(async () => {
        await vi.advanceTimersByTimeAsync(0);
      });

      expect(getTickets).toHaveBeenCalledTimes(1);
      expect(getTickets).toHaveBeenLastCalledWith(
        expect.objectContaining({ status: undefined, priority: undefined, search: '', page: 1 }),
      );
    });
  });
});
