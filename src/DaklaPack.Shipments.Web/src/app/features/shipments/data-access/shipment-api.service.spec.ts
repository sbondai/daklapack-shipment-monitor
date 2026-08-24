import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';

import { APP_CONFIG, defaultAppConfig } from '../../../core/app-config';
import { PagedResult, Shipment, defaultShipmentQuery } from '../models/shipment.model';
import { ShipmentApiService } from './shipment-api.service';

describe('ShipmentApiService', () => {
  let service: ShipmentApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: APP_CONFIG, useValue: defaultAppConfig },
        ShipmentApiService,
      ],
    });

    service = TestBed.inject(ShipmentApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('calls the shipments endpoint on the configured base path', () => {
    service.getShipments(defaultShipmentQuery).subscribe();

    const request = http.expectOne((r) => r.url === '/api/v1/shipments');
    expect(request.request.method).toBe('GET');
    request.flush(emptyPage());
  });

  it('sends every query parameter the API expects', () => {
    service
      .getShipments({ ...defaultShipmentQuery, sortBy: 'Carrier', sortOrder: 'Asc', page: 3, pageSize: 50 })
      .subscribe();

    const request = http.expectOne((r) => r.url === '/api/v1/shipments');
    expect(request.request.params.get('sortBy')).toBe('Carrier');
    expect(request.request.params.get('sortOrder')).toBe('Asc');
    expect(request.request.params.get('page')).toBe('3');
    expect(request.request.params.get('pageSize')).toBe('50');
    request.flush(emptyPage());
  });

  it('omits status entirely when no filter is applied', () => {
    // Sending status= would be a malformed enum value, not "no filter". The API treats an absent
    // parameter as "all", so the parameter has to be left off rather than sent empty.
    service.getShipments({ ...defaultShipmentQuery, status: null }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/v1/shipments');
    expect(request.request.params.has('status')).toBe(false);
    request.flush(emptyPage());
  });

  it('sends status when a filter is applied', () => {
    service.getShipments({ ...defaultShipmentQuery, status: 'Delayed' }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/v1/shipments');
    expect(request.request.params.get('status')).toBe('Delayed');
    request.flush(emptyPage());
  });

  it('returns the page the server sent, untouched', () => {
    const page = emptyPage();
    let received: PagedResult<Shipment> | undefined;

    service.getShipments(defaultShipmentQuery).subscribe((r) => (received = r));
    http.expectOne((r) => r.url === '/api/v1/shipments').flush(page);

    expect(received).toEqual(page);
  });
});

function emptyPage(): PagedResult<Shipment> {
  return { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 };
}
