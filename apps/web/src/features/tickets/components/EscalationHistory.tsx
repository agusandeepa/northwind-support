import { EmptyState } from '../../../components/EmptyState';
import { ErrorMessage } from '../../../components/ErrorMessage';
import { Spinner } from '../../../components/Spinner';
import { formatDateTime } from '../../../lib/format';
import type { AgentSummary, Escalation } from '../../../types/api';
import { PriorityBadge } from './TicketBadges';

interface EscalationHistoryProps {
  escalations: Escalation[] | undefined;
  isLoading: boolean;
  error: string | undefined;
  onRetry: () => void;
}

/** A ticket's escalations, newest first: what changed, why, who and when. */
export function EscalationHistory({ escalations, isLoading, error, onRetry }: EscalationHistoryProps) {
  return (
    <div className="card">
      <h2>Escalation history</h2>

      {isLoading && !escalations && <Spinner label="Loading history" />}

      {error && <ErrorMessage message={error} onRetry={onRetry} />}

      {escalations && escalations.length === 0 && (
        <EmptyState title="Not escalated yet" description="Escalations of this ticket will be listed here." />
      )}

      {escalations && escalations.length > 0 && (
        <ol className="escalation-list">
          {escalations.map((escalation) => (
            <li key={escalation.id} className="escalation">
              <p className="escalation__reason">{escalation.reason}</p>

              <dl className="escalation__changes">
                <div>
                  <dt>Priority</dt>
                  <dd>
                    <PriorityBadge priority={escalation.fromPriority} /> to{' '}
                    <PriorityBadge priority={escalation.toPriority} />
                  </dd>
                </div>

                <div>
                  <dt>Agent</dt>
                  <dd>{describeAgents(escalation.fromAgent, escalation.toAgent)}</dd>
                </div>

                <div>
                  <dt>Due</dt>
                  <dd>
                    {formatDateTime(escalation.fromDueAtUtc)} to {formatDateTime(escalation.toDueAtUtc)}
                  </dd>
                </div>
              </dl>

              <p className="escalation__meta">
                By {escalation.escalatedBy} on {formatDateTime(escalation.escalatedAtUtc)}
              </p>
            </li>
          ))}
        </ol>
      )}
    </div>
  );
}

function describeAgents(from: AgentSummary | null, to: AgentSummary | null): string {
  const name = (agent: AgentSummary | null) => agent?.fullName ?? 'Unassigned';

  return from?.id === to?.id ? `${name(to)} (unchanged)` : `${name(from)} to ${name(to)}`;
}
