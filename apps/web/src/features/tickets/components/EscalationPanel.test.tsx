import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { escalation, ticketDetail } from '../../../test/fixtures';
import { EscalationPanel } from './EscalationPanel';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: { escalateTicket: vi.fn() },
}));

const escalateTicket = vi.mocked(ticketsApi.escalateTicket);

async function fillIn(reason: string) {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText('Reason'), reason);

  return user;
}

describe('EscalationPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('blocks a short reason and does not call the API', async () => {
    const onEscalated = vi.fn();
    render(<EscalationPanel ticket={ticketDetail()} onEscalated={onEscalated} />);

    const user = await fillIn('abc');
    await user.click(screen.getByRole('button', { name: 'Escalate one level' }));

    expect(screen.getByText(/reason of at least 5 characters/i)).toBeInTheDocument();
    expect(escalateTicket).not.toHaveBeenCalled();
    expect(onEscalated).not.toHaveBeenCalled();
  });

  it('sends only the trimmed reason (who escalates comes from the token) and hands the result up', async () => {
    const result = { ticket: ticketDetail({ priority: 'Critical' }), escalation: escalation() };
    escalateTicket.mockResolvedValue(result);
    const onEscalated = vi.fn();
    render(<EscalationPanel ticket={ticketDetail()} onEscalated={onEscalated} />);

    const user = await fillIn('  Customer is blocked  ');
    await user.click(screen.getByRole('button', { name: 'Escalate one level' }));

    expect(escalateTicket).toHaveBeenCalledWith(12, { reason: 'Customer is blocked' });
    expect(onEscalated).toHaveBeenCalledWith(result);
    expect(screen.getByLabelText('Reason')).toHaveValue('');
  });

  it('does not offer a field for who is escalating', () => {
    render(<EscalationPanel ticket={ticketDetail()} onEscalated={vi.fn()} />);

    expect(screen.queryByLabelText('Your name')).not.toBeInTheDocument();
  });

  it('shows a pending state while the request is in flight', async () => {
    let finish: (value: Awaited<ReturnType<typeof ticketsApi.escalateTicket>>) => void = () => {};
    escalateTicket.mockReturnValue(new Promise((resolve) => (finish = resolve)));
    render(<EscalationPanel ticket={ticketDetail()} onEscalated={vi.fn()} />);

    const user = await fillIn('A good reason');
    await user.click(screen.getByRole('button', { name: 'Escalate one level' }));

    expect(await screen.findByRole('button', { name: 'Escalating...' })).toBeDisabled();

    finish({ ticket: ticketDetail(), escalation: escalation() });
  });

  it('shows the message of a 409 and leaves the ticket alone', async () => {
    escalateTicket.mockRejectedValue(
      new ApiError(409, 'Ticket TCK-0012 is already Critical and cannot be escalated.'),
    );
    const onEscalated = vi.fn();
    render(<EscalationPanel ticket={ticketDetail()} onEscalated={onEscalated} />);

    const user = await fillIn('A good reason');
    await user.click(screen.getByRole('button', { name: 'Escalate one level' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('already Critical');
    expect(onEscalated).not.toHaveBeenCalled();
  });

  it('shows a failure that is not an API error', async () => {
    escalateTicket.mockRejectedValue(new TypeError('Failed to fetch'));
    render(<EscalationPanel ticket={ticketDetail()} onEscalated={vi.fn()} />);

    const user = await fillIn('A good reason');
    await user.click(screen.getByRole('button', { name: 'Escalate one level' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not escalate the ticket.');
  });

  it('disables the form for a Critical ticket and says why', () => {
    render(<EscalationPanel ticket={ticketDetail({ priority: 'Critical' })} onEscalated={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'Escalate one level' })).toBeDisabled();
    expect(screen.getByText(/already Critical/i)).toBeInTheDocument();
  });

  it.each(['Resolved', 'Closed'] as const)('disables the form for a %s ticket', (status) => {
    render(<EscalationPanel ticket={ticketDetail({ status })} onEscalated={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'Escalate one level' })).toBeDisabled();
    expect(screen.getByText(new RegExp(status, 'i'))).toBeInTheDocument();
  });
});
