import { HttpErrorResponse } from '@angular/common/http';

/** A transport or server failure, reduced to what the UI can actually show a person. */
export interface ApiError {
  /** A message safe and useful to display. */
  readonly message: string;
  /** HTTP status, or `null` when the request never reached the server. */
  readonly status: number | null;
  /** Field-level validation messages, when the server sent any. */
  readonly fieldErrors: readonly string[];
}

/** RFC 7807 problem response, as produced by the API. */
interface ProblemDetails {
  readonly title?: string;
  readonly detail?: string;
  readonly status?: number;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null;
}

/**
 * Translates an {@link HttpErrorResponse} into an {@link ApiError}.
 *
 * The distinction that matters to a person watching a dashboard is whether the request never left
 * the browser (status 0 — they are offline, or the API is down) or the server answered with a
 * failure. Those need different words, so they are separated here rather than collapsed into a
 * single "something went wrong".
 */
export function toApiError(response: HttpErrorResponse): ApiError {
  if (response.status === 0) {
    return {
      message: 'Could not reach the shipments service. Check your connection and try again.',
      status: null,
      fieldErrors: [],
    };
  }

  const problem: ProblemDetails = isProblemDetails(response.error) ? response.error : {};

  const fieldErrors = problem.errors
    ? Object.values(problem.errors).flatMap((messages) => [...messages])
    : [];

  return {
    message: problem.detail ?? problem.title ?? `The shipments service returned ${response.status}.`,
    status: response.status,
    fieldErrors,
  };
}
