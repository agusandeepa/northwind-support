import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { escalation } from '../../../test/fixtures';
import { EscalationHistory } from './EscalationHistory';

describe('EscalationHistory', () => {
  it('shows the reason, who and the priority change of each escalation', () => {
    render(
      <EscalationHistory
        escalations={[escalation({ id: 2, reason: 'Second reason' }), escalation({ id: 1, reason: 'First reason' })]}
        isLoading={false}
        error={undefined}
        onRetry={vi.fn()}
      />,
    );

    const reasons = screen.getAllByText(/reason$/).map((element) => element.textContent);
    expect(reasons).toEqual(['Second reason', 'First reason']);
    expect(screen.getAllByText(/By Alex Turner/)).toHaveLength(2);
    expect(screen.getAllByText('Medium').length).toBeGreaterThan(0);
    expect(screen.getAllByText('High').length).toBeGreaterThan(0);
  });

  it('says when the agent changed and when they did not', () => {
    render(
      <EscalationHistory
        escalations={[
          escalation({ id: 2, fromAgent: { id: 4, fullName: 'Sara Lindqvist' }, toAgent: { id: 5, fullName: 'Ben Osei' } }),
          escalation({ id: 1 }),
        ]}
        isLoading={false}
        error={undefined}
        onRetry={vi.fn()}
      />,
    );

    expect(screen.getByText('Sara Lindqvist to Ben Osei')).toBeInTheDocument();
    expect(screen.getByText('Sara Lindqvist (unchanged)')).toBeInTheDocument();
  });

  it('shows Unassigned for a ticket that had no owner', () => {
    render(
      <EscalationHistory
        escalations={[escalation({ fromAgent: null, toAgent: { id: 5, fullName: 'Ben Osei' } })]}
        isLoading={false}
        error={undefined}
        onRetry={vi.fn()}
      />,
    );

    expect(screen.getByText('Unassigned to Ben Osei')).toBeInTheDocument();
  });

  it('has an empty state', () => {
    render(<EscalationHistory escalations={[]} isLoading={false} error={undefined} onRetry={vi.fn()} />);

    expect(screen.getByText('Not escalated yet')).toBeInTheDocument();
  });

  it('shows a loading state before the first load', () => {
    render(<EscalationHistory escalations={undefined} isLoading error={undefined} onRetry={vi.fn()} />);

    expect(screen.getByRole('status')).toHaveTextContent('Loading history');
  });

  it('shows the error and lets the user retry', async () => {
    const onRetry = vi.fn();
    render(<EscalationHistory escalations={undefined} isLoading={false} error="Unable to load." onRetry={onRetry} />);

    await userEvent.click(screen.getByRole('button', { name: 'Try again' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Unable to load.');
    expect(onRetry).toHaveBeenCalledOnce();
  });
});
