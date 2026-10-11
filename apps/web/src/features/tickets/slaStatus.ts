import type { BadgeTone } from '../../components/Badge';
import type { SlaStatus } from '../../types/api';

export interface SlaDisplay {
  label: string;
  tone: BadgeTone;
  /** Shown beside the label so the state never depends on colour alone. */
  symbol: string;
}

/** How each SLA state is shown, in the order the filter lists them. */
export const slaDisplay: Record<SlaStatus, SlaDisplay> = {
  WithinSla: { label: 'Within SLA', tone: 'info', symbol: '\u25CF' },
  AtRisk: { label: 'At risk', tone: 'warning', symbol: '\u25B2' },
  Breached: { label: 'Breached', tone: 'danger', symbol: '\u2715' },
  Met: { label: 'Met', tone: 'success', symbol: '\u2713' },
  NotApplicable: { label: 'No SLA', tone: 'neutral', symbol: '\u2013' },
};

export const slaStatuses = Object.keys(slaDisplay) as SlaStatus[];
