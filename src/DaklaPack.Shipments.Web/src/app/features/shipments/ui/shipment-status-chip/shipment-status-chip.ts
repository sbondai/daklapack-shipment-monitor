import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { ShipmentStatus } from '../../models/shipment.model';

type Tone = 'neutral' | 'progress' | 'active' | 'success' | 'warning' | 'danger';

/**
 * A shipment's status, as a tonal chip.
 *
 * The label is always rendered and never replaced by colour alone (WCAG 1.4.1): colour is an
 * additional cue, not the only one. Roughly one in twelve men cannot reliably separate the amber
 * and green used here, and a monitoring view they cannot read is not a monitoring view.
 *
 * Colours come from the status custom properties declared in styles.scss rather than from hex
 * literals here, so the whole set changes in one place and follows the light/dark scheme.
 */
@Component({
  selector: 'app-shipment-status-chip',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="chip" [attr.data-tone]="tone()">{{ label() }}</span>`,
  styles: `
    .chip {
      display: inline-flex;
      align-items: center;
      padding: 0.25rem 0.7rem;
      border-radius: var(--mat-sys-corner-small, 8px);
      font: var(--mat-sys-label-large);
      letter-spacing: 0.01em;
      white-space: nowrap;
    }

    .chip[data-tone='neutral']  { background: var(--status-neutral-bg);  color: var(--status-neutral-fg); }
    .chip[data-tone='progress'] { background: var(--status-progress-bg); color: var(--status-progress-fg); }
    .chip[data-tone='active']   { background: var(--status-active-bg);   color: var(--status-active-fg); }
    .chip[data-tone='success']  { background: var(--status-success-bg);  color: var(--status-success-fg); }
    .chip[data-tone='warning']  { background: var(--status-warning-bg);  color: var(--status-warning-fg); }
    .chip[data-tone='danger']   { background: var(--status-danger-bg);   color: var(--status-danger-fg); }
  `,
})
export class ShipmentStatusChip {
  readonly status = input.required<ShipmentStatus>();

  /** Human wording. The API sends PascalCase names; people read words. */
  readonly label = computed<string>(() => {
    switch (this.status()) {
      case 'Created': return 'Created';
      case 'InTransit': return 'In transit';
      case 'OutForDelivery': return 'Out for delivery';
      case 'Delivered': return 'Delivered';
      case 'Delayed': return 'Delayed';
      case 'Cancelled': return 'Cancelled';
    }
  });

  readonly tone = computed<Tone>(() => {
    switch (this.status()) {
      case 'Created': return 'neutral';
      case 'InTransit': return 'progress';
      case 'OutForDelivery': return 'active';
      case 'Delivered': return 'success';
      case 'Delayed': return 'warning';
      case 'Cancelled': return 'danger';
    }
  });
}
