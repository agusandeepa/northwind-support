import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { createdTicket } from '../../../test/fixtures';
import { TriageResult } from './TriageResult';

describe('TriageResult', () => {
  it('shows the applied priority, the due date and the owner, each with its reason', () => {
    render(<TriageResult ticket={createdTicket()} />);

    expect(screen.getByText('Critical')).toBeInTheDocument();
    expect(screen.getByText('Outage tickets are always Critical.')).toBeInTheDocument();

    expect(screen.getByText(/4 h base window x 0.5 for a Premium customer = 2 h/)).toBeInTheDocument();

    expect(screen.getByText('Assigned to')).toBeInTheDocument();
    expect(screen.getByText('Sara Lindqvist')).toBeInTheDocument();
    expect(screen.getByText(/fewest open tickets/)).toBeInTheDocument();
  });

  it('says why a ticket was left unassigned', () => {
    render(
      <TriageResult
        ticket={createdTicket({
          assignedAgent: null,
          triage: {
            priorityReason: 'No priority was requested, so it defaults to Medium.',
            slaReason: 'Medium priority: 24 h base window.',
            assignmentReason: 'Left unassigned: every eligible agent is at their open-ticket limit.',
          },
        })}
      />,
    );

    expect(screen.getByText('Unassigned')).toBeInTheDocument();
    expect(screen.getByText(/Left unassigned: every eligible agent is at their open-ticket limit/)).toBeInTheDocument();
    expect(screen.queryByText('Assigned to')).not.toBeInTheDocument();
  });
});
