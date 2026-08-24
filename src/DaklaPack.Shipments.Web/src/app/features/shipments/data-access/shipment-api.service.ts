import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { APP_CONFIG } from '../../../core/app-config';
import { PagedResult, Shipment, ShipmentQuery } from '../models/shipment.model';

/**
 * Talks to the shipments API.
 *
 * HTTP only. It holds no state, makes no decisions and knows nothing about loading spinners or
 * error banners — that belongs to the store. Keeping this boundary means the service is trivially
 * testable with `HttpTestingController` and can be reused by anything else that needs the data.
 */
@Injectable({ providedIn: 'root' })
export class ShipmentApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  /** Fetches one page of shipments. */
  getShipments(query: ShipmentQuery): Observable<PagedResult<Shipment>> {
    let params = new HttpParams()
      .set('sortBy', query.sortBy)
      .set('sortOrder', query.sortOrder)
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    // Omitted rather than sent empty: the API treats an absent status as "all", and sending
    // `status=` would be a malformed enum value.
    if (query.status !== null) {
      params = params.set('status', query.status);
    }

    return this.http.get<PagedResult<Shipment>>(`${this.config.apiBaseUrl}/shipments`, { params });
  }
}
