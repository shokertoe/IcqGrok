/* Minimal SignalR client wrapper for ICQ */
class IcqHub {
  constructor(url, token) {
    this.url = url;
    this.token = token;
    this.connection = null;
    this.handlers = {};
  }

  on(event, fn) {
    if (!this.handlers[event]) this.handlers[event] = [];
    this.handlers[event].push(fn);
  }

  async start() {
    // Use @microsoft/signalr if available, else fallback to native WebSocket negotiate
    if (typeof signalR !== 'undefined') {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(this.url, { accessTokenFactory: () => this.token })
        .withAutomaticReconnect()
        .build();

      for (const [ev, fns] of Object.entries(this.handlers)) {
        for (const fn of fns) this.connection.on(ev, fn);
      }

      await this.connection.start();
      return;
    }

    // Fallback: simple WS (limited)
    console.warn('SignalR JS client not loaded, real-time limited');
  }

  async invoke(method, ...args) {
    if (this.connection) return this.connection.invoke(method, ...args);
  }

  async stop() {
    if (this.connection) await this.connection.stop();
  }
}

// Load official client from CDN if needed
(function loadSignalR() {
  if (typeof signalR !== 'undefined') return;
  const s = document.createElement('script');
  s.src = 'https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.0/dist/browser/signalr.min.js';
  s.async = true;
  document.head.appendChild(s);
})();
