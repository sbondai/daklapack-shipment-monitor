import {
  SHIPMENT_STATUSES,
  SORT_FIELD_COLUMNS,
  ShipmentSortField,
  SortDirection,
  fromMaterialDirection,
  sortFieldForColumn,
  toMaterialDirection,
} from './shipment.model';

/**
 * Two vocabularies meet here: the API names sort fields in PascalCase, Angular Material identifies
 * a sortable header by its camelCase `matColumnDef`.
 *
 * This mapping has already been wrong once. `matSortActive` was given the API enum name, matched no
 * column, and the header rendered as unsorted while the server was in fact sorting — and the reverse
 * cast only appeared to work because ASP.NET binds enums case-insensitively, so the server quietly
 * accepted a column id. Neither compiler catches that; these tests do.
 */
describe('sort field translation', () => {
  const fields = Object.keys(SORT_FIELD_COLUMNS) as ShipmentSortField[];

  it('maps every API sort field to a column id', () => {
    expect(fields.length).toBeGreaterThan(0);
    for (const field of fields) {
      expect(SORT_FIELD_COLUMNS[field]).toBeTruthy();
    }
  });

  it('round-trips every field back to itself', () => {
    for (const field of fields) {
      expect(sortFieldForColumn(SORT_FIELD_COLUMNS[field])).toBe(field);
    }
  });

  it('gives each field a distinct column, so no two sorts collide', () => {
    const columns = fields.map((field) => SORT_FIELD_COLUMNS[field]);
    expect(new Set(columns).size).toBe(columns.length);
  });

  it('produces camelCase column ids, not the PascalCase enum names', () => {
    // The specific defect: passing the enum name straight to matSortActive matches no column.
    for (const field of fields) {
      const column = SORT_FIELD_COLUMNS[field];
      expect(column).not.toBe(field);
      expect(column[0]).toBe(field[0].toLowerCase());
    }
  });

  it('rejects a column that is not sortable rather than casting it through', () => {
    // destination and weightKg are rendered but not sortable; a click on them must not send the
    // server a field it does not accept.
    expect(sortFieldForColumn('destination')).toBeNull();
    expect(sortFieldForColumn('weightKg')).toBeNull();
    expect(sortFieldForColumn('')).toBeNull();
    expect(sortFieldForColumn('nonsense')).toBeNull();
  });

  it('rejects the PascalCase name, which is what the broken version passed', () => {
    expect(sortFieldForColumn('DispatchedAt')).toBeNull();
    expect(sortFieldForColumn('TrackingId')).toBeNull();
  });

  describe('direction translation', () => {
    it('maps the API direction to Material in both cases', () => {
      expect(toMaterialDirection('Asc')).toBe('asc');
      expect(toMaterialDirection('Desc')).toBe('desc');
    });

    it('maps Material back to the API in both cases', () => {
      expect(fromMaterialDirection('asc')).toBe('Asc');
      expect(fromMaterialDirection('desc')).toBe('Desc');
    });

    it('round-trips both directions', () => {
      const directions: SortDirection[] = ['Asc', 'Desc'];
      for (const direction of directions) {
        expect(fromMaterialDirection(toMaterialDirection(direction))).toBe(direction);
      }
    });

    it('falls back to the default when Material reports no direction', () => {
      // Material uses '' for the cleared state. matSortDisableClear means it should not arrive,
      // but sending an empty direction to the server would leave paging non-deterministic, so the
      // default is used rather than the empty value being trusted through.
      expect(fromMaterialDirection('')).toBe('Desc');
    });

    it('never produces a value the API would reject', () => {
      for (const input of ['asc', 'desc', '', 'ASC', 'nonsense']) {
        expect(['Asc', 'Desc']).toContain(fromMaterialDirection(input));
      }
    });
  });

  it('covers the statuses the filter offers', () => {
    // Not sort translation, but the same class of contract: the filter is built from this list, so
    // a status the API can return but the list omits becomes unfilterable.
    expect(SHIPMENT_STATUSES.length).toBe(6);
    expect(new Set(SHIPMENT_STATUSES).size).toBe(SHIPMENT_STATUSES.length);
  });
});
