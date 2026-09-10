/* API client for ICQ Server */
const API = {
  base: '',
  token: localStorage.getItem('icq_token') || null,
  refresh: localStorage.getItem('icq_refresh') || null,

  setTokens(res) {
    let accessToken = res.accessToken || res.AccessToken;
    let refreshToken = res.refreshToken || res.RefreshToken;
    this.token = accessToken;
    this.refresh = refreshToken;
    if (accessToken) localStorage.setItem('icq_token', accessToken);
    else localStorage.removeItem('icq_token');
    if (refreshToken) localStorage.setItem('icq_refresh', refreshToken);
    else localStorage.removeItem('icq_refresh');
  },

  async request(method, path, body, isForm = false) {
    const headers = {};
    if (this.token) headers['Authorization'] = `Bearer ${this.token}`;
    if (body && !isForm) headers['Content-Type'] = 'application/json';

    let res = await fetch(this.base + path, {
      method,
      headers,
      body: body ? (isForm ? body : JSON.stringify(body)) : undefined
    });

    if (res.status === 401 && this.refresh) {
      const ok = await this.tryRefresh();
      if (ok) {
        headers['Authorization'] = `Bearer ${this.token}`;
        res = await fetch(this.base + path, {
          method,
          headers,
          body: body ? (isForm ? body : JSON.stringify(body)) : undefined
        });
      }
    }

    if (!res.ok) {
      const err = await res.json().catch(() => ({ error: res.statusText }));
      throw new Error(err.error || err.message || res.statusText);
    }
    if (res.status === 204) return null;
    return res.json();
  },

  async tryRefresh() {
    try {
      const data = await fetch(this.base + '/api/auth/refresh', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: this.refresh })
      }).then(r => r.json());
      if (data.accessToken) {
        this.setTokens(data);
        return true;
      }
    } catch {}
    this.setTokens(null, null);
    return false;
  },

  register(email, password, nickname) {
    return this.request('POST', '/api/auth/register', { email, password, nickname });
  },
  login(emailOrUin, password) {
    return this.request('POST', '/api/auth/login', { "NicknameOrEmail": emailOrUin, password });
  },
  me() { return this.request('GET', '/api/auth/me'); },
  searchUsers(q) {
    const url = '/api/users/search?q=' + encodeURIComponent(q) + '&limit=20';
    console.log('[API] searchUsers:', url);
    return this.request('GET', url);
  },
  getContacts() { return this.request('GET', '/api/users/contacts'); },
  addContact(data) { return this.request('POST', '/api/users/contacts', data); },
  getChats() { return this.request('GET', '/api/chats'); },
  createChat(otherUserId) { return this.request('POST', '/api/chats/private', { TargetUserId: otherUserId }); },
  getMessages(chatId, beforeId) {
    let url = `/api/chats/${chatId}/messages`;
    if (beforeId) url += `?beforeId=${beforeId}`;
    return this.request('GET', url);
  },
  sendMessage(body) {
    return this.request('POST', '/api/chats/messages', body);
  },
  uploadFile(file) {
    const fd = new FormData();
    fd.append('file', file);
    return this.request('POST', '/api/files/upload', fd, true);
  },
  getKeyBundle(userId) { return this.request('GET', `/api/keys/bundle/${userId}`); },
  uploadKeyBundle(bundle) { return this.request('PUT', '/api/keys/bundle', bundle); },
  vapidPublicKey() { return this.request('GET', '/api/push/vapid-public-key'); },
  webPushSubscribe(sub) { return this.request('POST', '/api/push/web/subscribe', sub); },
  webPushUnsubscribe() { return this.request('POST', '/api/push/web/unsubscribe'); },
  getSmileys() { return this.request('GET', '/api/smileys'); },

  clearAuth() {
    this.token = null;
    this.refresh = null;
    localStorage.removeItem('icq_token');
    localStorage.removeItem('icq_refresh');
  }
};
