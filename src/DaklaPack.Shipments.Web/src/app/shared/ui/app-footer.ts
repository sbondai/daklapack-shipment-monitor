import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

/**
 * The application footer.
 *
 * Deliberately not a copy of daklapack.com's marketing footer — locations, services and careers
 * links belong on the public site, not on an internal operations console. What an operator
 * actually needs at the bottom of a monitoring view is whether the numbers above are current, so
 * that is what this carries: when the data was last fetched, and whether it is still refreshing.
 *
 * The live indicator matters because polling stops once the operator pages past the first page. A
 * dashboard that quietly changes its own refresh behaviour is worse than one that never refreshed,
 * because the person watching cannot tell which they are looking at. This says so out loud.
 */
@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [DatePipe, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <footer class="footer">
      <div class="footer__status" role="status" aria-live="polite">
        @if (live()) {
          <span class="dot dot--live" aria-hidden="true"></span>
          <span>Live &mdash; refreshing every {{ intervalSeconds() }}s</span>
        } @else {
          <span class="dot dot--paused" aria-hidden="true"></span>
          <span>Paused &mdash; refresh resumes on page 1</span>
        }

        @if (lastUpdatedAt(); as updated) {
          <span class="footer__sep" aria-hidden="true">&middot;</span>
          <span>Updated {{ updated | date: 'HH:mm:ss' }}</span>
        }
      </div>

      <div class="footer__meta">
        <span class="footer__brand">
          <mat-icon aria-hidden="true">local_shipping</mat-icon>
          Daklapack Group
        </span>
        <span class="footer__sep" aria-hidden="true">&middot;</span>
        <span>Operations &mdash; Shipment Monitor</span>
      </div>
    </footer>
  `,
  styles: `
    .footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem 1.5rem;
      flex-wrap: wrap;
      padding: 1rem 1.5rem;
      margin-top: 2rem;
      background: var(--dak-black);
      color: rgb(255 255 252 / 72%);
      border-top: 3px solid var(--dak-red);
      font-size: 0.8125rem;
    }

    .footer__status,
    .footer__meta {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .footer__brand {
      display: inline-flex;
      align-items: center;
      gap: 0.4rem;
      color: var(--dak-text-on-dark);
      font-weight: 500;
    }

    .footer__brand mat-icon {
      font-size: 1.05rem;
      width: 1.05rem;
      height: 1.05rem;
      color: var(--dak-red);
    }

    .footer__sep {
      opacity: 0.4;
    }

    .dot {
      width: 0.5rem;
      height: 0.5rem;
      border-radius: 50%;
      flex: none;
    }

    .dot--live {
      background: #4caf50;
      box-shadow: 0 0 0 3px rgb(76 175 80 / 20%);
    }

    .dot--paused {
      background: #e7af66;
    }

    /* A pulsing dot is a distraction on a screen someone watches all day, and it is exactly the
       kind of motion vestibular disorders react to. Animate only if the viewer allows it. */
    @media (prefers-reduced-motion: no-preference) {
      .dot--live {
        animation: pulse 2.4s ease-in-out infinite;
      }
    }

    @keyframes pulse {
      0%, 100% { box-shadow: 0 0 0 3px rgb(76 175 80 / 20%); }
      50%      { box-shadow: 0 0 0 6px rgb(76 175 80 / 6%); }
    }

    @media (max-width: 600px) {
      .footer {
        padding: 1rem;
        font-size: 0.75rem;
      }
    }
  `,
})
export class AppFooter {
  readonly live = input(false);
  readonly lastUpdatedAt = input<Date | null>(null);
  readonly intervalSeconds = input(15);
}
