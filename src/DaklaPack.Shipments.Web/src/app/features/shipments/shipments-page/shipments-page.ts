import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { PageEvent } from '@angular/material/paginator';
import { Sort, SortDirection as MaterialSortDirection } from '@angular/material/sort';
import { MatToolbarModule } from '@angular/material/toolbar';

import { AppFooter } from '../../../shared/ui/app-footer';
import { StatePanel } from '../../../shared/ui/state-panel';
import { APP_CONFIG } from '../../../core/app-config';
import { ShipmentStore } from '../data-access/shipment-store';
import {
  SORT_FIELD_COLUMNS,
  ShipmentStatus,
  fromMaterialDirection,
  sortFieldForColumn,
  toMaterialDirection,
} from '../models/shipment.model';
import { ShipmentFilterBar } from '../ui/shipment-filter-bar/shipment-filter-bar';
import { ShipmentTable } from '../ui/shipment-table/shipment-table';

/**
 * The shipment monitoring view.
 *
 * A smart container and nothing more: it reads signals from the store, hands them to presentational
 * children, and forwards their events back. There is no fetching, no error handling and no state
 * here — that all lives in {@link ShipmentStore}, which is the separation the brief asks for.
 */
@Component({
  selector: 'app-shipments-page',
  standalone: true,
  imports: [
    AppFooter,
    MatButtonModule,
    MatIconModule,
    MatToolbarModule,
    ShipmentFilterBar,
    ShipmentTable,
    StatePanel,
  ],
  providers: [ShipmentStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shipments-page.html',
  styleUrl: './shipments-page.scss',
})
export class ShipmentsPage implements OnInit {
  protected readonly store = inject(ShipmentStore);

  /** Shown in the footer so the refresh cadence is stated rather than guessed at. */
  protected readonly pollSeconds = Math.round(inject(APP_CONFIG).pollIntervalMs / 1000);

  /** The applied sort direction in Material's vocabulary. */
  protected readonly materialDirection = computed<MaterialSortDirection>(() =>
    toMaterialDirection(this.store.query().sortOrder),
  );

  /** The applied sort as a table column id, so the visible header matches the server ordering. */
  protected readonly materialSortColumn = computed(() => SORT_FIELD_COLUMNS[this.store.query().sortBy]);

  ngOnInit(): void {
    this.store.initialise();
  }

  protected onStatusChange(status: ShipmentStatus | null): void {
    this.store.filterByStatus(status);
  }

  protected onSortChange(sort: Sort): void {
    // sort.active is a column id, not an API enum member. Translating rather than casting: the cast
    // only appeared to work because ASP.NET binds enums case-insensitively, which meant a genuine
    // mismatch was being papered over by the server's leniency.
    const sortBy = sortFieldForColumn(sort.active);
    if (sortBy === null) {
      return;
    }

    this.store.sortBy(sortBy, fromMaterialDirection(sort.direction));
  }

  protected onPageChange(event: PageEvent): void {
    this.store.goToPage(event.pageIndex, event.pageSize);
  }
}
