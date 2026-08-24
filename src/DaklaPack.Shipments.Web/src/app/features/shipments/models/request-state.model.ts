import { ApiError } from '../../../core/api-error';

/**
 * The state of a request, as a discriminated union — so `loading && hasError` is unrepresentable
 * and the data only exists in the state where it has actually loaded.
 *
 * There is deliberately no `empty` member. Emptiness is a property of the data, not an outcome of
 * the request, and treating it as an outcome discarded the page envelope and stranded operators on
 * a page that had emptied out. `toView` derives "no results" and "page past the end" instead.
 */
export type RequestState<T> =
  | { readonly status: 'idle' }
  | { readonly status: 'loading' }
  | { readonly status: 'loaded'; readonly data: T }
  | { readonly status: 'error'; readonly error: ApiError };
