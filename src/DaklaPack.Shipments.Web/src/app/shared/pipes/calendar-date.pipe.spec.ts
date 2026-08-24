import { CalendarDatePipe } from './calendar-date.pipe';

describe('CalendarDatePipe', () => {
  const pipe = new CalendarDatePipe();

  it('formats an ISO calendar date', () => {
    expect(pipe.transform('2026-09-03')).toBe('3 Sep 2026');
  });

  it('drops the leading zero from the day', () => {
    expect(pipe.transform('2026-08-05')).toBe('5 Aug 2026');
  });

  it('handles the first and last day of the year', () => {
    expect(pipe.transform('2026-01-01')).toBe('1 Jan 2026');
    expect(pipe.transform('2026-12-31')).toBe('31 Dec 2026');
  });

  it('does not shift the date, whatever the host time zone', () => {
    // The regression this pipe exists for: DatePipe parsed the same string into UTC midnight and
    // then offset it, rendering 2 Sep in a UTC+2 browser. Reading the components cannot do that.
    expect(pipe.transform('2026-09-03')).toBe('3 Sep 2026');
    expect(pipe.transform('2026-01-01')).not.toContain('2025');
    expect(pipe.transform('2026-12-31')).not.toContain('2027');
  });

  it('returns an empty string for no value', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
    expect(pipe.transform('')).toBe('');
  });

  it('passes through anything that is not an ISO date rather than inventing one', () => {
    expect(pipe.transform('not-a-date')).toBe('not-a-date');
    expect(pipe.transform('2026-13-01')).toBe('2026-13-01');
  });
});
