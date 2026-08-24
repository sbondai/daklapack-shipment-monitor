import { ChangeDetectionStrategy, Component } from '@angular/core';

import { ShipmentsPage } from './features/shipments/shipments-page/shipments-page';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [ShipmentsPage],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<app-shipments-page />',
})
export class App {}
