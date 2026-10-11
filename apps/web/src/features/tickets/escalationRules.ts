import type { TicketDetail } from '../../types/api';

/** Same limits as the API's EscalateTicketRequestValidator. */
export const MIN_REASON_LENGTH = 5;
export const MAX_REASON_LENGTH = 500;
export const MAX_ACTOR_LENGTH = 100;

export interface EscalationErrors {
  reason?: string;
  escalatedBy?: string;
}

/** What is wrong with the input, by field. Empty when it can be sent. Lengths are measured after trimming. */
export function validateEscalation(reason: string, escalatedBy: string): EscalationErrors {
  const errors: EscalationErrors = {};
  const reasonLength = reason.trim().length;

  if (reasonLength < MIN_REASON_LENGTH) {
    errors.reason = `Give a reason of at least ${MIN_REASON_LENGTH} characters.`;
  } else if (reasonLength > MAX_REASON_LENGTH) {
    errors.reason = `The reason can be at most ${MAX_REASON_LENGTH} characters.`;
  }

  if (escalatedBy.trim().length === 0) {
    errors.escalatedBy = 'Enter your name.';
  } else if (escalatedBy.trim().length > MAX_ACTOR_LENGTH) {
    errors.escalatedBy = `The name can be at most ${MAX_ACTOR_LENGTH} characters.`;
  }

  return errors;
}

/** Why the ticket cannot be escalated, or undefined when it can. The API makes the final call. */
export function whyNotEscalatable(ticket: Pick<TicketDetail, 'status' | 'priority'>): string | undefined {
  if (ticket.status === 'Resolved' || ticket.status === 'Closed') {
    return `A ${ticket.status.toLowerCase()} ticket cannot be escalated.`;
  }

  if (ticket.priority === 'Critical') {
    return 'This ticket is already Critical, so it cannot be escalated further.';
  }

  return undefined;
}
