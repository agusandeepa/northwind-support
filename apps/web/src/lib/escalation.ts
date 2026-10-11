import type { TicketDetail } from '../types/api';

/** Mirrors the server's limits (TicketEscalation.ReasonMinLength and ReasonMaxLength). */
export const REASON_MIN_LENGTH = 5;
export const REASON_MAX_LENGTH = 500;

/** Same rule as the API, so an invalid reason never leaves the browser. */
export function validateReason(reason: string): string | undefined {
  const length = reason.trim().length;

  if (length < REASON_MIN_LENGTH) {
    return `Give a reason of at least ${REASON_MIN_LENGTH} characters.`;
  }

  if (length > REASON_MAX_LENGTH) {
    return `Keep the reason to ${REASON_MAX_LENGTH} characters or fewer.`;
  }

  return undefined;
}

/**
 * Why a ticket cannot be escalated, or undefined when it can. The server stays the authority
 * (it answers 409); this only lets the screen say so before the user tries.
 */
export function escalationBlockedReason(
  ticket: Pick<TicketDetail, 'priority' | 'status'>,
): string | undefined {
  if (ticket.status === 'Resolved' || ticket.status === 'Closed') {
    return `This ticket is ${ticket.status.toLowerCase()} and cannot be escalated.`;
  }

  if (ticket.priority === 'Critical') {
    return 'This ticket is already Critical and cannot be escalated further.';
  }

  return undefined;
}
