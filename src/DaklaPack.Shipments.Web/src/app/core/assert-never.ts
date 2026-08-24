/**
 * Fails to compile if `value` is not `never`.
 *
 * An Angular template `@switch` does no exhaustiveness checking, so a missing arm there compiles
 * and silently renders nothing. Resolving state in TypeScript through a switch that ends here means
 * adding a union member breaks the build instead.
 */
export function assertNever(value: never, context: string): never {
  throw new Error(`Unhandled ${context}: ${JSON.stringify(value)}`);
}
