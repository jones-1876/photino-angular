import { Injectable, signal } from '@angular/core';

/**
 * Photino injects `window.external` into the WebView with two members.
 * TypeScript's DOM lib already declares `window.external` with a different
 * (deprecated) shape, so this is cast at the access site rather than declared
 * in a `declare global` block, which would conflict.
 */
interface PhotinoExternal {
  sendMessage(message: string): void;
  receiveMessage(callback: (message: string) => void): void;
}

/** Bridge between the Angular UI and the Photino host (backend/Program.cs). */
@Injectable({ providedIn: 'root' })
export class PhotinoService {
  private readonly bridge = PhotinoService.resolveBridge();

  /** True when running inside the Photino window, false under a plain `ng serve` browser tab. */
  readonly available = this.bridge !== null;

  /** Every message the host has sent back, most recent last. */
  readonly messages = signal<readonly string[]>([]);

  constructor() {
    this.bridge?.receiveMessage((message) => {
      this.messages.update((all) => [...all, message]);
    });
  }

  /** Sends a message to the .NET host. No-ops outside the Photino window. */
  send(message: string): void {
    if (!this.bridge) {
      console.warn('[PhotinoService] no host bridge available; message dropped:', message);
      return;
    }
    this.bridge.sendMessage(message);
  }

  private static resolveBridge(): PhotinoExternal | null {
    const external = (globalThis as { external?: Partial<PhotinoExternal> }).external;
    return typeof external?.sendMessage === 'function' &&
      typeof external?.receiveMessage === 'function'
      ? (external as PhotinoExternal)
      : null;
  }
}
