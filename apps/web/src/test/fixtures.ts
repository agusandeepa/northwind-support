import type { Session } from '../auth/session';
import type { CreatedTicket, Escalation, PagedResult, TicketDetail, TicketListItem } from '../types/api';

/** A list row with sensible defaults, so tests only state what they care about. */
export function ticketListItem(overrides: Partial<TicketListItem> = {}): TicketListItem {
  return {
    id: 12,
    reference: 'TCK-0012',
    title: 'Invoice total does not match the order',
    status: 'InProgress',
    priority: 'High',
    customer: { id: 2, name: 'Fabrikam Inc', tier: 'Standard' },
    category: { id: 2, name: 'Billing' },
    assignedAgent: { id: 4, fullName: 'Sara Lindqvist' },
    createdAtUtc: '2026-09-24T08:12:00Z',
    updatedAtUtc: '2026-09-25T10:03:00Z',
    dueAtUtc: '2026-09-24T16:12:00Z',
    resolvedAtUtc: null,
    slaStatus: 'Breached',
    ...overrides,
  };
}

export function page(items: TicketListItem[]): PagedResult<TicketListItem> {
  return {
    items,
    page: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: 1,
  };
}

export function ticketDetail(overrides: Partial<TicketDetail> = {}): TicketDetail {
  return {
    id: 12,
    reference: 'TCK-0012',
    title: 'Invoice total does not match the order',
    description: 'The invoice shows a different total from the order confirmation.',
    status: 'InProgress',
    priority: 'High',
    customer: { id: 2, name: 'Fabrikam Inc', tier: 'Standard', email: 'billing@fabrikam.example', phone: null },
    category: { id: 2, name: 'Billing', requiresSpecialist: true },
    assignedAgent: { id: 4, fullName: 'Sara Lindqvist' },
    createdAtUtc: '2026-09-24T08:12:00Z',
    updatedAtUtc: '2026-09-25T10:03:00Z',
    dueAtUtc: '2026-09-24T16:12:00Z',
    resolvedAtUtc: null,
    slaStatus: 'WithinSla',
    ...overrides,
  };
}

export function escalation(overrides: Partial<Escalation> = {}): Escalation {
  return {
    id: 1,
    fromPriority: 'Medium',
    toPriority: 'High',
    fromAgent: { id: 4, fullName: 'Sara Lindqvist' },
    toAgent: { id: 4, fullName: 'Sara Lindqvist' },
    fromDueAtUtc: '2026-09-25T08:00:00Z',
    toDueAtUtc: '2026-09-24T16:00:00Z',
    reason: 'The customer cannot pay their invoice',
    escalatedBy: 'Alex Turner',
    escalatedAtUtc: '2026-09-24T08:00:00Z',
    ...overrides,
  };
}

export function createdTicket(overrides: Partial<CreatedTicket> = {}): CreatedTicket {
  return {
    ...ticketDetail({ priority: 'Critical', dueAtUtc: '2026-10-12T12:00:00Z', slaStatus: 'WithinSla' }),
    triage: {
      priorityReason: 'Outage tickets are always Critical.',
      slaReason: 'Critical priority: 4 h base window x 0.5 for a Premium customer = 2 h.',
      assignmentReason: 'Assigned to Sara Lindqvist: fewest open tickets (2) among 3 eligible agent(s).',
    },
    ...overrides,
  };
}

/** A signed-in session that has not expired. */
export function makeSession(overrides: Partial<Session> = {}): Session {
  return {
    token: 'test-token',
    expiresAtUtc: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    agent: { id: 1, fullName: 'Alex Turner', email: 'alex.turner@northwind-support.example' },
    ...overrides,
  };
}
