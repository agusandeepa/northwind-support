import { useState } from 'react';
import { toErrorMessage } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { Field } from '../../../components/Field';
import type { EscalationResult, TicketDetail } from '../../../types/api';
import { MAX_ACTOR_LENGTH, MAX_REASON_LENGTH, MIN_REASON_LENGTH, validateEscalation, whyNotEscalatable } from '../escalationRules';
import type { EscalationErrors } from '../escalationRules';

interface EscalationPanelProps {
  ticket: TicketDetail;
  onEscalated: (result: EscalationResult) => void;
}

/** Raises a ticket one priority level. Checks the same rules as the API before calling it. */
export function EscalationPanel({ ticket, onEscalated }: EscalationPanelProps) {
  const [reason, setReason] = useState('');
  const [escalatedBy, setEscalatedBy] = useState('');
  const [errors, setErrors] = useState<EscalationErrors>({});
  const [isSaving, setIsSaving] = useState(false);
  const [serverError, setServerError] = useState<string>();

  const blockedReason = whyNotEscalatable(ticket);

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setServerError(undefined);

    const validationErrors = validateEscalation(reason, escalatedBy);
    setErrors(validationErrors);

    // Invalid input never reaches the API.
    if (Object.keys(validationErrors).length > 0) {
      return;
    }

    setIsSaving(true);

    try {
      const result = await ticketsApi.escalateTicket(ticket.id, {
        reason: reason.trim(),
        escalatedBy: escalatedBy.trim(),
      });

      setReason('');
      onEscalated(result);
    } catch (caught) {
      // Also covers the API refusing a ticket the screen thought it could escalate (409).
      setServerError(toErrorMessage(caught, 'Could not escalate the ticket.'));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <form className="card form" onSubmit={submit} noValidate aria-label="Escalate this ticket">
      <h2>Escalate</h2>

      {blockedReason && <p className="field__hint">{blockedReason}</p>}

      <fieldset className="form__fieldset" disabled={blockedReason !== undefined || isSaving}>
        <Field
          id="escalation-reason"
          label="Reason"
          error={errors.reason}
          hint={`${MIN_REASON_LENGTH} to ${MAX_REASON_LENGTH} characters. It is kept in the ticket's history.`}
        >
          {(fieldProps) => (
            <textarea
              {...fieldProps}
              rows={3}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
            />
          )}
        </Field>

        <Field id="escalation-by" label="Your name" error={errors.escalatedBy}>
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="text"
              value={escalatedBy}
              maxLength={MAX_ACTOR_LENGTH}
              onChange={(event) => setEscalatedBy(event.target.value)}
            />
          )}
        </Field>

        <div className="button-row">
          <button type="submit" className="button button--primary">
            {isSaving ? 'Escalating...' : 'Escalate one level'}
          </button>
        </div>
      </fieldset>

      {serverError && (
        <p className="field__error" role="alert">
          {serverError}
        </p>
      )}
    </form>
  );
}
