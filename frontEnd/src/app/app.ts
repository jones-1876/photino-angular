import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { PhotinoService } from './photino.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('Gimx');
  protected readonly photino = inject(PhotinoService);

  private pings = 0;

  protected ping(): void {
    this.photino.send(`ping #${++this.pings}`);
  }
}
