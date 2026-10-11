import { Badge } from '../../../components/Badge';
import { formatDateTime } from '../../../lib/format';
import type { SlaStatus } from '../../../types/api';
import { slaDisplay } from '../slaStatus';

interface SlaIndicatorProps {
  dueAtUtc: string | null;
  slaStatus: SlaStatus;
}

/**
 * The one place an SLA state is drawn, for the ticket list and the detail page alike. The
 * state is told in words and with a symbol as well as a colour.
 */
export function SlaIndicator({ dueAtUtc, slaStatus }: SlaIndicatorProps) {
  const { label, tone, symbol } = slaDisplay[slaStatus];

  return (
    <div className="sla-cell">
      <Badge tone={tone}>
        <span aria-hidden="true">{symbol} </span>
        {label}
      </Badge>
      <span className="sla-cell__due">{formatDateTime(dueAtUtc)}</span>
    </div>
  );

}
