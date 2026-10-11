import { useState } from 'react';
import { toErrorMessage } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { Field } from '../../../components/Field';
import {
  REASON_MAX_LENGTH,
  REASON_MIN_LENGTH,
  escalationBlockedReason,
  validateReason,
} from '../../../lib/escalation';
import type { TicketDetail } from '../../../types/api';

type EscalateResult = Awaited<ReturnType<typeof ticketsApi.escalateTicket>>;

interface EscalationPanelProps {
  ticket: TicketDetail;
  /** Called with the server's answer once an escalation has been recorded. */
  onEscalated: (result: EscalateResult) => void;
}

/** Raises a ticket one priority level, with a recorded reason. The server records who asked. */
export function EscalationPanel({ ticket, onEscalated }: EscalationPanelProps) {
  const [reason, setReason] = useState('');
  const [reasonError, setReasonError] = useState<string>();
  const [isSaving, setIsSaving] = useState(false);
  const [serverError, setServerError] = useState<string>();

  const blockedReason = escalationBlockedReason(ticket);

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationError = validateReason(reason);
    setReasonError(validationError);
    setServerError(undefined);

    // Invalid input never reaches the API.
    if (validationError) {
      return;
    }

    setIsSaving(true);

    try {
      const result = await ticketsApi.escalateTicket(ticket.id, { reason: reason.trim() });

      setReason('');
      onEscalated(result);
    } catch (caught) {
      setServerError(toErrorMessage(caught, 'Could not escalate the ticket.'));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <form className="card form" onSubmit={submit} noValidate aria-labelledby="escalation-heading">
      <h2 id="escalation-heading">Escalate</h2>

      {blockedReason && (
        <p className="field__hint" role="status">
          {blockedReason}
        </p>
      )}

      <Field
        id="escalation-reason"
        label="Reason"
        error={reasonError}
        hint={`${REASON_MIN_LENGTH}-${REASON_MAX_LENGTH} characters. Recorded in the ticket's history.`}
      >
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            rows={3}
            value={reason}
            maxLength={REASON_MAX_LENGTH}
            disabled={blockedReason !== undefined || isSaving}
            onChange={(event) => setReason(event.target.value)}
          />
        )}
      </Field>

      {serverError && (
        <p className="field__error" role="alert">
          {serverError}
        </p>
      )}

      <div className="button-row">
        <button
          type="submit"
          className="button button--primary"
          disabled={blockedReason !== undefined || isSaving}
        >
          {isSaving ? 'Escalating...' : 'Escalate one level'}
        </button>
      </div>
    </form>
  );
}
