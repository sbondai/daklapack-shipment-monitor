import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { APP_CONFIG, AppConfig } from '../../../core/app-config';
import { PagedResult, Shipment } from '../models/shipment.model';
import { ShipmentApiService } from './shipment-api.service';
import { ShipmentStore } from './shipment-store';

const POLL_MS = 15_000;

const config: AppConfig = { apiBaseUrl: '/api/v1', pollIntervalMs: POLL_MS };

function shipment(id: string, trackingId = `DP-2026-${id.padStart(6, '0')}`): Shipment {
  return {
    id,
    trackingId,
    status: 'InTransit',
    weightKg: 10,
    destination: { city: 'Amsterdam', countryCode: 'NL', postalCode: '1011AB' },
    carrier: 'PostNL',
    dispatchedAt: '2026-08-20T09:15:00+02:00',
    estimatedDeliveryOn: '2026-08-25',
    isOverdue: false,
  };
}

function page(
  items: Shipment[],
  overrides: Partial<PagedResult<Shipment>> = {},
): PagedResult<Shipment> {
  return {
    items,
    page: 1,
    pageSize: 25,
    totalCount: items.length,
    totalPages: items.length === 0 ? 0 : 1,
    ...overrides,
  };
}

describe('ShipmentStore', () => {
  let store: ShipmentStore;
  let http: HttpTestingController;

  beforeEach(() => {
    // Fake timers so the polling policy is asserted deterministically. A test that waits 15 real
    // seconds is a test people delete.
    vi.useFakeTimers();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: APP_CONFIG, useValue: config },
        ShipmentApiService,
        ShipmentStore,
      ],
    });

    store = TestBed.inject(ShipmentStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  /** The single outstanding shipments request. */
  function pending(): TestRequest {
    return http.expectOne((r) => r.url === '/api/v1/shipments');
  }

  function loadWith(result: PagedResult<Shipment>): void {
    store.initialise();
    pending().flush(result);
  }

  describe('initial load', () => {
    it('starts in a loading view before anything returns', () => {
      store.initialise();

      expect(store.view().kind).toBe('loading');
      expect(store.busy()).toBe(true);
      pending().flush(page([]));
    });

    it('becomes ready when rows come back', () => {
      loadWith(page([shipment('1'), shipment('2')]));

      expect(store.view().kind).toBe('ready');
      expect(store.loadedPage()?.items.length).toBe(2);
      expect(store.busy()).toBe(false);
    });

    it('records when the data was fetched', () => {
      expect(store.lastUpdatedAt()).toBeNull();

      loadWith(page([shipment('1')]));

      expect(store.lastUpdatedAt()).toBeInstanceOf(Date);
    });
  });

  describe('empty results versus a page past the end', () => {
    it('reports no results when nothing matches at all', () => {
      loadWith(page([], { totalCount: 0, totalPages: 0 }));

      expect(store.view().kind).toBe('no-results');
      expect(store.pagePastEnd()).toBeNull();
    });

    it('reports a page past the end when matches exist on earlier pages', () => {
      // The trap this guards: mapping every empty array to "no results" discards the envelope, so
      // the paginator vanishes and an operator whose page shrank away has no route back.
      loadWith(page([], { page: 9, totalCount: 40, totalPages: 2 }));

      expect(store.view().kind).toBe('page-past-end');
      expect(store.pagePastEnd()?.totalCount).toBe(40);
    });

    it('can navigate back to the last page that still has rows', () => {
      loadWith(page([], { page: 9, totalCount: 40, totalPages: 2 }));

      store.goToLastPage();

      expect(pending().request.params.get('page')).toBe('2');
    });

    it('goToLastPage does nothing when the view is not past the end', () => {
      loadWith(page([shipment('1')]));

      store.goToLastPage();

      http.expectNone((r) => r.url === '/api/v1/shipments');
    });
  });

  describe('errors', () => {
    it('surfaces an error view when a foreground load fails', () => {
      store.initialise();
      pending().flush({ title: 'Nope' }, { status: 500, statusText: 'Server Error' });

      expect(store.view().kind).toBe('error');
      expect(store.error()).not.toBeNull();
    });

    it('distinguishes an unreachable service from a server failure', () => {
      store.initialise();
      pending().error(new ProgressEvent('network error'), { status: 0, statusText: '' });

      expect(store.error()?.status).toBeNull();
      expect(store.error()?.message).toContain('Could not reach');
    });

    it('recovers on retry', () => {
      store.initialise();
      pending().flush({}, { status: 500, statusText: 'Server Error' });
      expect(store.view().kind).toBe('error');

      store.refresh();
      pending().flush(page([shipment('1')]));

      expect(store.view().kind).toBe('ready');
    });
  });

  describe('paging', () => {
    it('converts the zero-based material index to the one-based api page', () => {
      loadWith(page([shipment('1')]));

      store.goToPage(0, 25);
      expect(pending().request.params.get('page')).toBe('1');

      store.goToPage(4, 25);
      expect(pending().request.params.get('page')).toBe('5');
    });

    it('carries the page size through', () => {
      loadWith(page([shipment('1')]));

      store.goToPage(1, 50);

      expect(pending().request.params.get('pageSize')).toBe('50');
    });
  });

  describe('filter and sort', () => {
    it('returns to the first page when the filter changes', () => {
      loadWith(page([shipment('1')]));
      store.goToPage(3, 25);
      pending().flush(page([shipment('2')], { page: 4 }));

      store.filterByStatus('Delayed');

      const request = pending();
      expect(request.request.params.get('status')).toBe('Delayed');
      expect(request.request.params.get('page')).toBe('1');
    });

    it('returns to the first page when the sort changes', () => {
      loadWith(page([shipment('1')]));
      store.goToPage(3, 25);
      pending().flush(page([shipment('2')], { page: 4 }));

      store.sortBy('Carrier', 'Asc');

      const request = pending();
      expect(request.request.params.get('sortBy')).toBe('Carrier');
      expect(request.request.params.get('page')).toBe('1');
    });
  });

  describe('obsolete requests', () => {
    it('ignores a superseded response', () => {
      // Rapid filter clicks. Without switchMap the first response can land last and paint rows
      // the operator has already filtered away.
      loadWith(page([shipment('1')]));

      store.filterByStatus('Delayed');
      const first = pending();

      store.filterByStatus('Delivered');
      const second = http.expectOne((r) => r.url === '/api/v1/shipments');

      // The superseded request was unsubscribed, so answering it must change nothing.
      expect(first.cancelled).toBe(true);
      second.flush(page([shipment('99')]));

      expect(store.loadedPage()?.items[0].id).toBe('99');
    });
  });

  describe('polling', () => {
    it('refreshes on the first page', () => {
      loadWith(page([shipment('1')]));

      vi.advanceTimersByTime(POLL_MS);

      pending().flush(page([shipment('1')]));
      expect(store.resultsChanged()).toBe(false);
    });

    it('does not refresh once the operator has paged past the first page', () => {
      // On a later page the operator is reading a stable set; refreshing under them would shift
      // rows mid-scan.
      loadWith(page([shipment('1')]));
      store.goToPage(1, 25);
      pending().flush(page([shipment('2')], { page: 2 }));

      vi.advanceTimersByTime(POLL_MS * 3);

      http.expectNone((r) => r.url === '/api/v1/shipments');
    });

    it('flags that results changed without claiming how many arrived', () => {
      loadWith(page([shipment('1')], { totalCount: 40 }));

      vi.advanceTimersByTime(POLL_MS);
      pending().flush(page([shipment('2')], { totalCount: 41 }));

      expect(store.resultsChanged()).toBe(true);
      // Deliberately no count: a net total delta is not proof of arrivals.
      expect(store.loadedPage()?.items[0].id).toBe('1');
    });

    it('flags a change when the result set shrinks', () => {
      loadWith(page([shipment('1')], { totalCount: 40 }));

      vi.advanceTimersByTime(POLL_MS);
      pending().flush(page([shipment('1')], { totalCount: 39 }));

      expect(store.resultsChanged()).toBe(true);
    });

    it('flags a status change on the same shipment', () => {
      // The change this view exists to show. Same id, same position, same total - an id-only
      // comparison would call this "nothing changed" while the operator reads a stale status.
      loadWith(page([shipment('1')], { totalCount: 40 }));

      vi.advanceTimersByTime(POLL_MS);
      pending().flush(page([{ ...shipment('1'), status: 'Delayed' }], { totalCount: 40 }));

      expect(store.resultsChanged()).toBe(true);
    });

    it('flags a delivery date change on the same shipment', () => {
      loadWith(page([shipment('1')], { totalCount: 40 }));

      vi.advanceTimersByTime(POLL_MS);
      pending().flush(
        page([{ ...shipment('1'), estimatedDeliveryOn: '2026-09-02' }], { totalCount: 40 }),
      );

      expect(store.resultsChanged()).toBe(true);
    });

    it('flags a shipment tipping overdue', () => {
      // Happens with no server-side edit at all: the business date rolls over and the API derives
      // isOverdue differently on the next read.
      loadWith(page([shipment('1')], { totalCount: 40 }));

      vi.advanceTimersByTime(POLL_MS);
      pending().flush(page([{ ...shipment('1'), isOverdue: true }], { totalCount: 40 }));

      expect(store.resultsChanged()).toBe(true);
    });

    it('flags a carrier correction on the same shipment', () => {
      loadWith(page([shipment('1')], { totalCount: 40 }));

      vi.advanceTimersByTime(POLL_MS);
      pending().flush(page([{ ...shipment('1'), carrier: 'DHL' }], { totalCount: 40 }));

      expect(store.resultsChanged()).toBe(true);
    });

    it('stays quiet when an identical page comes back', () => {
      loadWith(page([shipment('1'), shipment('2')], { totalCount: 40 }));

      vi.advanceTimersByTime(POLL_MS);
      pending().flush(page([shipment('1'), shipment('2')], { totalCount: 40 }));

      expect(store.resultsChanged()).toBe(false);
    });

    it('flags a change when membership moves but the total does not', () => {
      // One arrives, one leaves. A total-count comparison alone would call this "no change", which
      // is why the page identity is compared and not just the number.
      loadWith(page([shipment('1')], { totalCount: 40 }));

      vi.advanceTimersByTime(POLL_MS);
      pending().flush(page([shipment('2')], { totalCount: 40 }));

      expect(store.resultsChanged()).toBe(true);
    });

    it('does not replace the visible rows behind the operator', () => {
      loadWith(page([shipment('1')], { totalCount: 40 }));

      vi.advanceTimersByTime(POLL_MS);
      pending().flush(page([shipment('2')], { totalCount: 41 }));

      expect(store.loadedPage()?.items[0].id).toBe('1');
    });

    it('applies the change only when the operator asks', () => {
      loadWith(page([shipment('1')], { totalCount: 40 }));
      vi.advanceTimersByTime(POLL_MS);
      pending().flush(page([shipment('2')], { totalCount: 41 }));

      store.refresh();
      pending().flush(page([shipment('2')], { totalCount: 41 }));

      expect(store.resultsChanged()).toBe(false);
      expect(store.loadedPage()?.items[0].id).toBe('2');
    });

    it('a failed background poll leaves the visible page intact', () => {
      loadWith(page([shipment('1')], { totalCount: 40 }));

      vi.advanceTimersByTime(POLL_MS);
      pending().flush({}, { status: 500, statusText: 'Server Error' });

      expect(store.view().kind).toBe('ready');
      expect(store.loadedPage()?.items[0].id).toBe('1');
      expect(store.error()).toBeNull();
    });
  });

  describe('live indicator', () => {
    it('is live on the first page', () => {
      loadWith(page([shipment('1')]));

      expect(store.isLive()).toBe(true);
    });

    it('is paused beyond the first page', () => {
      loadWith(page([shipment('1')]));
      store.goToPage(2, 25);
      pending().flush(page([shipment('2')], { page: 3 }));

      expect(store.isLive()).toBe(false);
    });

    it('is not live while in error', () => {
      store.initialise();
      pending().flush({}, { status: 500, statusText: 'Server Error' });

      expect(store.isLive()).toBe(false);
    });
  });
});
