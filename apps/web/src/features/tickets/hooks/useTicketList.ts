import { useCallback, useEffect, useState } from 'react';
import { ticketsApi } from '../../../api/tickets';
import { useAsyncData } from '../../../hooks/useAsyncData';
import type { TicketFilters } from '../../../types/api';

const DEBOUNCE_MS = 300;

const LOAD_ERROR = 'Unable to load tickets.';

export const emptyFilters: TicketFilters = {
  search: '',
  status: undefined,
  priority: undefined,
  categoryId: undefined,
  customerId: undefined,
  assignedAgentId: undefined,
  unassignedOnly: false,
  page: 1,
  pageSize: 20,
  sortBy: 'createdAtUtc',
  sortDirection: 'desc',
};

/**
 * Owns the ticket list's filters and the data they produce.
 *
 * `filters` is the committed set: it is exactly what the last request was made with, and any
 * change to it loads the list again. Typing is the one exception, since a request per keystroke
 * would be wasteful: the text lives in `searchText` and is only committed once typing settles.
 * Because every other filter goes straight into `filters`, adding one (e.g. the SLA status)
 * needs no extra wiring here.
 */
export function useTicketList() {
  const [filters, setFilters] = useState<TicketFilters>(emptyFilters);
  const [searchText, setSearchText] = useState(emptyFilters.search);

  // A new function (so a new request) exactly when the committed filters change.
  const loadTickets = useCallback(() => ticketsApi.getTickets(filters), [filters]);
  const { data, error, isLoading, reload } = useAsyncData(loadTickets, LOAD_ERROR);

  // Commits the typed search once the user stops typing. A new search starts again at page 1.
  useEffect(() => {
    const handle = window.setTimeout(() => {
      setFilters((current) =>
        current.search === searchText ? current : { ...current, search: searchText, page: 1 },
      );
    }, DEBOUNCE_MS);

    return () => window.clearTimeout(handle);
  }, [searchText]);

  const updateFilters = (patch: Partial<TicketFilters>) => {
    const { search, ...immediate } = patch;

    if (search !== undefined) {
      setSearchText(search);
    }

    if (Object.keys(immediate).length > 0) {
      setFilters((current) => ({ ...current, ...immediate, page: immediate.page ?? 1 }));
    }
  };

  const resetFilters = () => {
    setSearchText(emptyFilters.search);
    setFilters(emptyFilters);
  };

  // The search box shows what was typed, even while the request for it is still pending.
  return {
    filters: { ...filters, search: searchText },
    updateFilters,
    resetFilters,
    reload,
    isLoading,
    data,
    error,
  };
}
