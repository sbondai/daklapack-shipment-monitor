/**
 * Lifecycle state of a shipment.
 *
 * A string union mirroring the server enum, which serialises as names rather than numbers. Numbers
 * would be unreadable in the UI and would silently change meaning if a member were ever inserted
 * server-side.
 */
export type ShipmentStatus =
  | 'Created'
  | 'InTransit'
  | 'OutForDelivery'
  | 'Delivered'
  | 'Delayed'
  | 'Cancelled';

/** Every status, in lifecycle order. Used to build the filter without repeating the union. */
export const SHIPMENT_STATUSES: readonly ShipmentStatus[] = [
  'Created',
  'InTransit',
  'OutForDelivery',
  'Delivered',
  'Delayed',
  'Cancelled',
] as const;

/** Where a shipment is going. Nested, because the three parts are only meaningful together. */
export interface Destination {
  readonly city: string;
  readonly countryCode: string;
  readonly postalCode: string;
}

/** A shipment as the API returns it. */
export interface Shipment {
  readonly id: string;
  readonly trackingId: string;
  readonly status: ShipmentStatus;
  readonly weightKg: number;
  readonly destination: Destination;
  readonly carrier: string;
  /**
   * An instant, ISO 8601 with an offset, e.g. `2026-08-20T09:15:00+02:00`.
   *
   * Kept as a string deliberately. It is displayed in the viewer's local zone, but it is never
   * parsed into a `Date` and re-serialised, which is how offsets get quietly lost.
   */
  readonly dispatchedAt: string;
  /**
   * A calendar date, ISO 8601, e.g. `2026-08-25` — no time, no zone.
   *
   * Must **not** be converted through a time zone. It is a day a human agreed to, and shifting it
   * is how a Wednesday delivery becomes Tuesday for anyone west of the warehouse.
   */
  readonly estimatedDeliveryOn: string;
  /** Past its delivery date and not finished. Derived server-side from the current date. */
  readonly isOverdue: boolean;
}

/** One page of results. */
export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly page: number;
  /** The size actually applied by the server, which may be smaller than the one requested. */
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
}

/** Fields the API allows sorting by. Matches the server enum exactly. */
export type ShipmentSortField =
  | 'DispatchedAt'
  | 'EstimatedDeliveryOn'
  | 'Status'
  | 'Carrier'
  | 'TrackingId';

export type SortDirection = 'Asc' | 'Desc';

/**
 * Maps API sort fields to the table column ids that carry them.
 *
 * Two vocabularies meet here. The API names its enum members in PascalCase; Angular Material
 * identifies a sortable header by its `matColumnDef`, which is camelCase. They are not
 * interchangeable: passing the enum name to `matSortActive` matches no column, so the header
 * silently renders as unsorted while the server is in fact sorting. Declaring the mapping once,
 * both ways, is what stops that drifting.
 */
export const SORT_FIELD_COLUMNS: Readonly<Record<ShipmentSortField, string>> = {
  DispatchedAt: 'dispatchedAt',
  EstimatedDeliveryOn: 'estimatedDeliveryOn',
  Status: 'status',
  Carrier: 'carrier',
  TrackingId: 'trackingId',
} as const;

const COLUMN_SORT_FIELDS: Readonly<Record<string, ShipmentSortField>> = Object.fromEntries(
  Object.entries(SORT_FIELD_COLUMNS).map(([field, column]) => [column, field as ShipmentSortField]),
);

/** The API sort field a table column represents, or null if the column is not sortable. */
export function sortFieldForColumn(column: string): ShipmentSortField | null {
  return COLUMN_SORT_FIELDS[column] ?? null;
}

/** The API sort direction as Material spells it. */
export function toMaterialDirection(order: SortDirection): 'asc' | 'desc' {
  return order === 'Asc' ? 'asc' : 'desc';
}

/**
 * Material's sort direction as the API spells it.
 *
 * Material uses an empty string for the cleared, unsorted state. `matSortDisableClear` on the table
 * means it should not arrive, but falling back to the default rather than trusting that keeps the
 * ordering total: an empty direction reaching the server would leave paging non-deterministic.
 */
export function fromMaterialDirection(direction: string): SortDirection {
  return direction === 'asc' ? 'Asc' : 'Desc';
}

/** The query the UI sends. `page` is 1-based, matching the API and *not* MatPaginator. */
export interface ShipmentQuery {
  readonly status: ShipmentStatus | null;
  readonly sortBy: ShipmentSortField;
  readonly sortOrder: SortDirection;
  readonly page: number;
  readonly pageSize: number;
}

/** Newest work first, which is what an operations view wants to open on. */
export const defaultShipmentQuery: ShipmentQuery = {
  status: null,
  sortBy: 'DispatchedAt',
  sortOrder: 'Desc',
  page: 1,
  pageSize: 25,
};
