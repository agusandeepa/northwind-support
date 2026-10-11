import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { emptyFilters } from '../hooks/useTicketList';
import { TicketFilters } from './TicketFilters';

function renderFilters(onChange = vi.fn()) {
  render(
    <TicketFilters
      filters={emptyFilters}
      agents={[]}
      categories={[]}
      customers={[]}
      onChange={onChange}
      onReset={vi.fn()}
    />,
  );

  return onChange;
}

describe('TicketFilters', () => {
  it('lists every SLA state, in words', () => {
    renderFilters();

    const options = screen.getAllByRole('option').map((option) => option.textContent);

    expect(options).toEqual(expect.arrayContaining(['Within SLA', 'At risk', 'Breached', 'Met', 'No SLA']));
  });

  it('reports the chosen SLA status', async () => {
    const onChange = renderFilters();

    await userEvent.selectOptions(screen.getByLabelText('SLA'), 'At risk');

    expect(onChange).toHaveBeenCalledWith({ slaStatus: 'AtRisk' });
  });

  it('reports no SLA status when "Any SLA state" is chosen again', async () => {
    const onChange = renderFilters();

    await userEvent.selectOptions(screen.getByLabelText('SLA'), 'Breached');
    await userEvent.selectOptions(screen.getByLabelText('SLA'), 'Any SLA state');

    expect(onChange).toHaveBeenLastCalledWith({ slaStatus: undefined });
  });
});
