import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { SlaStatus } from '../../../types/api';
import { slaDisplay, slaStatuses } from '../slaStatus';
import { SlaIndicator } from './SlaIndicator';

const wording: Record<SlaStatus, string> = {
  WithinSla: 'Within SLA',
  AtRisk: 'At risk',
  Breached: 'Breached',
  Met: 'Met',
  NotApplicable: 'No SLA',
};

describe('SlaIndicator', () => {
  it.each(slaStatuses)('says %s in words', (status) => {
    render(<SlaIndicator slaStatus={status} dueAtUtc={null} />);

    expect(screen.getByText(wording[status])).toBeInTheDocument();
  });

  it('tells every state apart by its text and by its symbol, not only by colour', () => {
    const texts = slaStatuses.map((status) => {
      const { container, unmount } = render(<SlaIndicator slaStatus={status} dueAtUtc={null} />);
      const text = container.textContent;
      unmount();

      return text;
    });

    expect(new Set(texts).size).toBe(slaStatuses.length);
    expect(new Set(slaStatuses.map((status) => slaDisplay[status].symbol)).size).toBe(slaStatuses.length);
  });

  it('keeps the symbol out of the accessible text, so a screen reader reads the words only', () => {
    render(<SlaIndicator slaStatus="AtRisk" dueAtUtc={null} />);

    expect(screen.getByText(slaDisplay.AtRisk.symbol, { exact: false })).toHaveAttribute('aria-hidden', 'true');
  });

  it('shows the due date, or a dash when there is none', () => {
    const { rerender } = render(<SlaIndicator slaStatus="WithinSla" dueAtUtc="2026-10-12T10:00:00Z" />);
    expect(screen.queryByText('-')).not.toBeInTheDocument();

    rerender(<SlaIndicator slaStatus="NotApplicable" dueAtUtc={null} />);
    expect(screen.getByText('-')).toBeInTheDocument();
  });
});
