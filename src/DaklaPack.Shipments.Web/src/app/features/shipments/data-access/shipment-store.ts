import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, of, timer } from 'rxjs';
import { catchError, filter, map, switchMap } from 'rxjs/operators';

import { ApiError, toApiError } from '../../../core/api-error';
import { APP_CONFIG } from '../../../core/app-config';
import { assertNever } from '../../../core/assert-never';
import { RequestState } from '../models/request-state.model';
import {
  PagedResult,
  Shipment,
  ShipmentQuery,
  ShipmentSortField,
  ShipmentStatus,
  SortDirection,
  defaultShipmentQuery,
} from '../models/shipment.model';
import { ShipmentApiService } from './shipment-api.service';

/** What the template renders, resolved from {@link RequestState} in TypeScript (see `toView`). */
export type ShipmentView =
  | { readonly kind: 'loading' }
  | { readonly kind: 'no-results' }
  | { readonly kind: 'page-past-end'; readonly page: PagedResult<Shipment> }
  | { readonly kind: 'error'; readonly error: ApiError }
  | { readonly kind: 'ready'; readonly page: PagedResult<Shipment> };

type LoadMode = 'replace' | 'peek';

type Outcome =
  | { readonly mode: LoadMode; readonly page: PagedResult<Shipment> }
  | { readonly mode: LoadMode; readonly error: ApiError };

/**
 * Built from the fields the table shows, not just ids. Ids alone catch a row entering or leaving
 * but miss what this view exists to surface - a status change, a moved delivery date, a shipment
 * tipping overdue - because those keep the same id in the same position.
 */
function shipmentFingerprint(shipment: Shipment): string {
  return [
    shipment.id,
    shipment.trackingId,
    shipment.status,
    shipment.weightKg,
    shipment.destination.city,
    shipment.destination.countryCode,
    shipment.destination.postalCode,
    shipment.carrier,
    shipment.dispatchedAt,
    shipment.estimatedDeliveryOn,
    shipment.isOverdue,
  ].join('|');
}

function fingerprint(page: PagedResult<Shipment>): string {
  return `${page.totalCount}:${page.items.map(shipmentFingerprint).join(',')}`;
}

/**
 * Owns everything about fetching and displaying shipments: the current query, the request state,
 * retries, and the polling policy.
 *
 * This is the piece the brief means by "avoid placing all logic directly in the component". The
 * page component reads signals and passes them down; every decision happens here.
 */
@Injectable()
export class ShipmentStore {
  private readonly api = inject(ShipmentApiService);
  private readonly config = inject(APP_CONFIG);

  private readonly load$ = new Subject<{ query: ShipmentQuery; mode: LoadMode }>();

  private readonly _query = signal<ShipmentQuery>(defaultShipmentQuery);
  private readonly _state = signal<RequestState<PagedResult<Shipment>>>({ status: 'idle' });
  private readonly _resultsChanged = signal(false);
  private readonly _lastUpdatedAt = signal<Date | null>(null);

  readonly query = this._query.asReadonly();

  /**
   * A flag, not a count: a `totalCount` delta is a *net* change, so one arrival plus one departure
   * nets to zero. Reporting "3 new" from that arithmetic would be a number the server never gave.
   */
  readonly resultsChanged = this._resultsChanged.asReadonly();

  readonly lastUpdatedAt = this._lastUpdatedAt.asReadonly();

  readonly view = computed<ShipmentView>(() => toView(this._state()));

  readonly busy = computed(() => this._state().status === 'loading');

  // A template @switch does not narrow a union, so these expose each branch already narrowed - the
  // alternative is a cast in the template, which discards the typing the union provides.
  readonly error = computed<ApiError | null>(() => {
    const view = this.view();
    return view.kind === 'error' ? view.error : null;
  });

  readonly loadedPage = computed<PagedResult<Shipment> | null>(() => {
    const view = this.view();
    return view.kind === 'ready' ? view.page : null;
  });

  readonly pagePastEnd = computed<PagedResult<Shipment> | null>(() => {
    const view = this.view();
    return view.kind === 'page-past-end' ? view.page : null;
  });

  /** Surfaced so the footer can state whether the view is refreshing itself. */
  readonly isLive = computed(
    () => this._query().page === 1 && this._state().status !== 'error',
  );

  constructor() {
    this.load$
      .pipe(
        // switchMap, not mergeMap: a newer request supersedes an older one, so rapid filter
        // changes cannot deliver out of order.
        switchMap(({ query, mode }) => {
          if (mode === 'replace') {
            this._state.set({ status: 'loading' });
          }

          return this.api.getShipments(query).pipe(
            map((page): Outcome => ({ mode, page })),
            catchError((response: HttpErrorResponse) =>
              of<Outcome>({ mode, error: toApiError(response) }),
            ),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((outcome) => this.apply(outcome));

    timer(this.config.pollIntervalMs, this.config.pollIntervalMs)
      .pipe(
        // First page only: on a later page the operator is reading a stable set, and refreshing
        // underneath them would shift rows mid-scan.
        filter(() => this._query().page === 1 && this._state().status === 'loaded'),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.load$.next({ query: this._query(), mode: 'peek' }));
  }

  initialise(): void {
    this.load$.next({ query: this._query(), mode: 'replace' });
  }

  refresh(): void {
    this._resultsChanged.set(false);
    this.load$.next({ query: this._query(), mode: 'replace' });
  }

  /** Returns to the first page: the result set changed. */
  filterByStatus(status: ShipmentStatus | null): void {
    this.applyQuery({ ...this._query(), status, page: 1 });
  }

  sortBy(sortBy: ShipmentSortField, sortOrder: SortDirection): void {
    this.applyQuery({ ...this._query(), sortBy, sortOrder, page: 1 });
  }

  /**
   * Moves to a page.
   *
   * `pageIndex` is Angular Material zero-based index; the API is one-based. **This is the only
   * place that conversion happens** — doing it in more than one place is how the classic
   * off-by-one page bug gets in.
   */
  goToPage(pageIndex: number, pageSize: number): void {
    this.applyQuery({ ...this._query(), page: pageIndex + 1, pageSize });
  }

  /**
   * Jumps to the last page that still holds rows.
   *
   * Offered when the current page has emptied out beneath the operator — results shrank while they
   * were on page four and there are now only two. Without a route back they would be looking at an
   * empty table with no paginator and no way to reach the data that is still there.
   */
  goToLastPage(): void {
    const page = this.pagePastEnd();
    if (page === null) {
      return;
    }

    this.applyQuery({ ...this._query(), page: Math.max(page.totalPages, 1) });
  }

  private applyQuery(query: ShipmentQuery): void {
    this._query.set(query);
    this._resultsChanged.set(false);
    this.load$.next({ query, mode: 'replace' });
  }

  private apply(outcome: Outcome): void {
    if ('error' in outcome) {
      // A failed background poll must not destroy a good view.
      if (outcome.mode === 'replace') {
        this._state.set({ status: 'error', error: outcome.error });
      }
      return;
    }

    if (outcome.mode === 'peek') {
      const current = this._state();
      if (current.status === 'loaded') {
        this._resultsChanged.set(fingerprint(outcome.page) !== fingerprint(current.data));
        // A poll finding nothing different still confirms the view is current; a stale timestamp
        // would make "nothing changed" look like "we stopped checking".
        this._lastUpdatedAt.set(new Date());
      }
      return;
    }

    this._lastUpdatedAt.set(new Date());
    this._state.set({ status: 'loaded', data: outcome.page });
  }
}

function toView(state: RequestState<PagedResult<Shipment>>): ShipmentView {
  switch (state.status) {
    case 'idle':
    case 'loading':
      return { kind: 'loading' };
    case 'error':
      return { kind: 'error', error: state.error };
    case 'loaded':
      if (state.data.items.length > 0) {
        return { kind: 'ready', page: state.data };
      }

      // Two different situations arrive as an empty array. "Nothing matches" is an answer;
      // "you are on page nine of two" is a navigation problem, and conflating them hides the way
      // back.
      return state.data.totalCount === 0
        ? { kind: 'no-results' }
        : { kind: 'page-past-end', page: state.data };
    default:
      return assertNever(state, 'request state');
  }
}
