const CACHE = 'icq-v1';
const ASSETS = [
  '/web/',
  '/web/index.html',
  '/web/css/app.css',
  '/web/js/app.js',
  '/web/js/api.js',
  '/web/js/signalr.js',
  '/web/js/e2e.js',
  '/web/js/webrtc.js',
  '/web/js/smileys.js',
  '/web/manifest.json',
  '/web/icons/icon-192.png',
  '/web/icons/icon-512.png'
];

self.addEventListener('install', (e) => {
  e.waitUntil(caches.open(CACHE).then(c => c.addAll(ASSETS)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', (e) => {
  e.waitUntil(
    caches.keys().then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k))))
      .then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (e) => {
  if (e.request.method !== 'GET') return;
  e.respondWith(
    caches.match(e.request).then(cached => cached || fetch(e.request).then(res => {
      const clone = res.clone();
      if (res.ok && e.request.url.startsWith(self.location.origin)) {
        caches.open(CACHE).then(c => c.put(e.request, clone));
      }
      return res;
    }).catch(() => cached))
  );
});

self.addEventListener('push', (e) => {
  let data = { title: 'ICQ', body: 'New message' };
  try { if (e.data) data = e.data.json(); } catch {}
  e.waitUntil(
    self.registration.showNotification(data.title || 'ICQ', {
      body: data.body || '',
      icon: '/web/icons/icon-192.png',
      badge: '/web/icons/icon-192.png',
      data: data
    })
  );
});

self.addEventListener('notificationclick', (e) => {
  e.notification.close();
  e.waitUntil(clients.matchAll({ type: 'window' }).then(list => {
    for (const c of list) { if (c.url.includes('/web') && 'focus' in c) return c.focus(); }
    if (clients.openWindow) return clients.openWindow('/web/');
  }));
});
