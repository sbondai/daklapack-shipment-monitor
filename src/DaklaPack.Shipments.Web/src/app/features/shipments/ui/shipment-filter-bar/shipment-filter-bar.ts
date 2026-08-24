import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';

import { SHIPMENT_STATUSES, ShipmentStatus } from '../../models/shipment.model';

/** Status filter and manual refresh. Presentational: it reports intent and holds no state. */
@Component({
  selector: 'app-shipment-filter-bar',
  standalone: true,
  imports: [MatButtonModule, MatFormFieldModule, MatIconModule, MatSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bar">
      <mat-form-field appearance="outline" subscriptSizing="dynamic" class="bar__filter">
        <mat-label>Filter by status</mat-label>
        <mat-icon matPrefix>filter_list</mat-icon>
        <mat-select
          [value]="status()"
          [disabled]="busy()"
          (valueChange)="statusChange.emit($event)"
        >
          <mat-option [value]="null">All statuses</mat-option>
          @for (option of statuses; track option) {
            <mat-option [value]="option">{{ option }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      <span class="bar__spacer"></span>

      <button matButton="outlined" type="button" [disabled]="busy()" (click)="refresh.emit()">
        <mat-icon>refresh</mat-icon>
        Refresh
      </button>
    </div>
  `,
  styles: `
    .bar {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
      width: 100%;
    }
    .bar__filter { min-width: 16rem; }
    .bar__filter mat-icon[matPrefix] {
      margin-inline: 0.25rem 0.5rem;
      color: var(--mat-sys-on-surface-variant);
    }
    .bar__spacer { flex: 1 1 auto; }
  `,
})
export class ShipmentFilterBar {
  protected readonly statuses = SHIPMENT_STATUSES;

  readonly status = input<ShipmentStatus | null>(null);
  readonly busy = input(false);

  readonly statusChange = output<ShipmentStatus | null>();
  readonly refresh = output<void>();
}
