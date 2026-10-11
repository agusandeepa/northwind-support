import { formatDateTime } from '../../../lib/format';
import type { CreatedTicket } from '../../../types/api';
import { PriorityBadge } from './TicketBadges';

interface TriageResultProps {
  ticket: CreatedTicket;
}

/** What triage decided for a new ticket, with its reasons, so nobody has to guess why. */
export function TriageResult({ ticket }: TriageResultProps) {
  const { triage } = ticket;

  return (
    <dl className="summary" aria-label="How the ticket was triaged">
      <div>
        <dt>Priority</dt>
        <dd>
          <PriorityBadge priority={ticket.priority} />
          <span className="summary__note">{triage.priorityReason}</span>
        </dd>
      </div>

      <div>
        <dt>Due</dt>
        <dd>
          {formatDateTime(ticket.dueAtUtc)}
          <span className="summary__note">{triage.slaReason}</span>
        </dd>
      </div>

      <div>
        <dt>{ticket.assignedAgent ? 'Assigned to' : 'Owner'}</dt>
        <dd>
          {ticket.assignedAgent?.fullName ?? 'Unassigned'}
          <span className="summary__note">{triage.assignmentReason}</span>
        </dd>
      </div>
    </dl>
  );
}
