import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

/**
 * The non-data states: loading, empty and error.
 *
 * One presentational component rather than three near-identical ones, because they differ only in
 * icon, wording and whether a retry is offered. `aria-live` is set so a screen reader announces the
 * transition; a spinner nobody is told about is not an accessible loading state.
 */
@Component({
  selector: 'app-state-panel',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="panel" role="status" aria-live="polite">
      @if (loading()) {
        <mat-spinner diameter="40" />
        <p class="panel__message">Loading shipments…</p>
      } @else {
        <mat-icon class="panel__icon" [class.panel__icon--error]="tone() === 'error'">
          {{ icon() }}
        </mat-icon>
        <p class="panel__message">{{ message() }}</p>
        @if (detail()) {
          <p class="panel__detail">{{ detail() }}</p>
        }
        @if (retryable()) {
          <button matButton="outlined" type="button" (click)="retry.emit()">Try again</button>
        }
      }
    </div>
  `,
  styles: `
    .panel {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.75rem;
      padding: 4rem 1.5rem;
      text-align: center;
    }
    .panel__icon {
      font-size: 3rem;
      width: 3rem;
      height: 3rem;
      color: var(--mat-sys-on-surface-variant);
    }
    .panel__icon--error {
      color: var(--mat-sys-error);
    }
    .panel__message {
      margin: 0;
      font: var(--mat-sys-title-medium);
      color: var(--mat-sys-on-surface);
    }
    .panel__detail {
      margin: 0;
      max-width: 44ch;
      font: var(--mat-sys-body-medium);
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class StatePanel {
  readonly loading = input(false);
  readonly tone = input<'neutral' | 'error'>('neutral');
  readonly icon = input('inbox');
  readonly message = input('');
  readonly detail = input<string | null>(null);
  readonly retryable = input(false);

  readonly retry = output<void>();
}
