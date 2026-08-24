import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [MatToolbarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-toolbar class="app-bar">
      <span class="app-bar__title">Daklapack <span>Shipment Monitor</span></span>
    </mat-toolbar>
  `,
  styles: `
    .app-bar {
      background: var(--dak-black);
      color: var(--dak-text-on-dark);
      border-bottom: 3px solid var(--dak-red);
    }

    .app-bar__title {
      font-family: Poppins, sans-serif;
      font-weight: 600;
      font-size: 1.0625rem;
    }

    .app-bar__title span {
      font-weight: 300;
      opacity: 0.75;
    }
  `,
})
export class App {}
