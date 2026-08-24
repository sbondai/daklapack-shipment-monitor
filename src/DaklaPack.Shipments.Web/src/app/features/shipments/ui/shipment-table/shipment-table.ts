import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort, SortDirection } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';

import { CalendarDatePipe } from '../../../../shared/pipes/calendar-date.pipe';
import { ShipmentStatusChip } from '../shipment-status-chip/shipment-status-chip';
import { PagedResult, Shipment } from '../../models/shipment.model';

/**
 * The shipment table, plus a card list below the mobile breakpoint.
 *
 * Purely presentational: inputs in, events out, no injected services, `OnPush`. It does not know
 * that pages are 1-based on the server — it emits Material's own `PageEvent` and the store converts.
 * A table forced onto a phone is a horizontal scrollbar, not a structured layout, so the same data
 * is rendered as cards at narrow widths.
 */
@Component({
  selector: 'app-shipment-table',
  standalone: true,
  imports: [
    CalendarDatePipe,
    DatePipe,
    DecimalPipe,
    MatIconModule,
    MatPaginatorModule,
    MatSortModule,
    MatTableModule,
    MatTooltipModule,
    ShipmentStatusChip,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shipment-table.html',
  styleUrl: './shipment-table.scss',
})
export class ShipmentTable {
  readonly page = input.required<PagedResult<Shipment>>();

  /** The sorted column id, so the visible header matches the ordering the server applied. */
  readonly activeSort = input.required<string>();

  /** The direction the server applied, in Material's own vocabulary. */
  readonly activeDirection = input.required<SortDirection>();

  readonly sortChange = output<Sort>();
  readonly pageChange = output<PageEvent>();

  protected readonly columns = [
    'trackingId',
    'status',
    'destination',
    'weightKg',
    'carrier',
    'dispatchedAt',
    'estimatedDeliveryOn',
  ] as const;

  /** Material's paginator is zero-based; the API is one-based. */
  protected pageIndex(page: PagedResult<Shipment>): number {
    return page.page - 1;
  }
}
