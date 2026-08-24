import { Pipe, PipeTransform } from '@angular/core';

const MONTHS = [
  'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
] as const;

/**
 * Formats an ISO calendar date (`2026-09-03`) without ever building a `Date`.
 *
 * `DatePipe` cannot do this. It parses the string into an instant — UTC midnight — and shifts it
 * into some zone, but a delivery date is a day a human agreed to and no offset is correct to apply
 * to it. Passing `: 'UTC'` looks like it pins the value down; it does not, and rendered every date
 * one day early in a UTC+2 browser.
 *
 * Instants are the opposite case and still use `DatePipe`.
 */
@Pipe({ name: 'calendarDate', standalone: true })
export class CalendarDatePipe implements PipeTransform {
  transform(isoDate: string | null | undefined): string {
    if (!isoDate) {
      return '';
    }

    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoDate);
    // Better to show the raw value than invent a date from something unexpected.
    if (!match) {
      return isoDate;
    }

    const [, year, month, day] = match;
    const monthName = MONTHS[Number(month) - 1];

    return monthName ? `${Number(day)} ${monthName} ${year}` : isoDate;
  }
}
