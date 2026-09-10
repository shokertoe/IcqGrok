/* Minimal SignalR client wrapper for ICQ */
class IcqHub {
  constructor(url, token) {
    this.url = url;
    this.token = token;
    this.connection = null;
    this.handlers = {};
    this.connected = false;
    this._signalRLoaded = typeof signalR !== 'undefined'
      ? Promise.resolve()
      : new Promise((resolve, reject) => {
          if (typeof signalR !== 'undefined') { resolve(); return; }
          const CDN_URLS = [
            'https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.0/dist/browser/signalr.min.js',
            'https://unpkg.com/@microsoft/signalr@8.0.0/dist/browser/signalr.min.js',
            'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js'
          ];
          let idx = 0;
          function tryLoad() {
            if (idx >= CDN_URLS.length) { reject(new Error('SignalR CDN unavailable')); return; }
            const timeout = setTimeout(() => { idx++; tryLoad(); }, 5000);
            const s = document.createElement('script');
            s.src = CDN_URLS[idx];
            s.onload = () => { clearTimeout(timeout); resolve(); };
            s.onerror = () => { clearTimeout(timeout); idx++; tryLoad(); };
            document.head.appendChild(s);
          }
          tryLoad();
        });
  }

  on(event, fn) {
    if (!this.handlers[event]) this.handlers[event] = [];
    this.handlers[event].push(fn);
  }

  async start() {
    try {
      await this._signalRLoaded;
    } catch (e) {
      console.warn('SignalR client not available:', e.message);
      this.connected = false;
      return;
    }

    if (typeof signalR === 'undefined') {
      console.warn('SignalR JS client not loaded, real-time limited');
      this.connected = false;
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.url, { accessTokenFactory: () => this.token })
      .withAutomaticReconnect()
      .build();

    this.connection.onclose(() => { this.connected = false; });
    this.connection.onreconnecting(() => { this.connected = false; });
    this.connection.onreconnected(() => { this.connected = true; });

    for (const [ev, fns] of Object.entries(this.handlers)) {
      for (const fn of fns) this.connection.on(ev, fn);
    }

    try {
      await this.connection.start();
      this.connected = true;
    } catch (e) {
      console.error('SignalR connection failed:', e);
      this.connected = false;
      throw e;
    }
  }

  async invoke(method, ...args) {
    if (!this.connected || !this.connection) {
      console.warn('Cannot invoke', method, ': not connected');
      return;
    }
    return this.connection.invoke(method, ...args);
  }

  async disconnect() {
    if (this.connection) await this.connection.stop();
    this.connected = false;
  }

  async stop() {
    await this.disconnect();
  }
}
