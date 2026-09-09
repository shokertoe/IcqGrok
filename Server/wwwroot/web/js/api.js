/* API client for ICQ Server */
const API = {
  base: '',
  token: localStorage.getItem('icq_token') || null,
  refresh: localStorage.getItem('icq_refresh') || null,

  setTokens(access, refresh) {
    this.token = access;
    this.refresh = refresh;
    if (access) localStorage.setItem('icq_token', access);
    else localStorage.removeItem('icq_token');
    if (refresh) localStorage.setItem('icq_refresh', refresh);
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
        this.setTokens(data.accessToken, data.refreshToken || this.refresh);
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
    return this.request('POST', '/api/auth/login', { emailOrUin, password });
  },
  me() { return this.request('GET', '/api/users/me'); },
  searchUsers(q) { return this.request('GET', '/api/users/search?q=' + encodeURIComponent(q)); },
  getContacts() { return this.request('GET', '/api/users/contacts'); },
  addContact(data) { return this.request('POST', '/api/users/contacts', data); },
  getChats() { return this.request('GET', '/api/chats'); },
  createChat(otherUserId) { return this.request('POST', '/api/chats', { otherUserId }); },
  getMessages(chatId, beforeId) {
    let url = `/api/chats/${chatId}/messages`;
    if (beforeId) url += `?beforeId=${beforeId}`;
    return this.request('GET', url);
  },
  sendMessage(chatId, content, isEncrypted = false, encryptedPayload = null) {
    return this.request('POST', '/api/chats/messages', { chatId, content, isEncrypted, encryptedPayload });
  },
  uploadFile(file) {
    const fd = new FormData();
    fd.append('file', file);
    return this.request('POST', '/api/files/upload', fd, true);
  },
  getKeyBundle(userId) { return this.request('GET', `/api/keys/${userId}`); },
  uploadKeyBundle(bundle) { return this.request('POST', '/api/keys', bundle); },
  getVapidKey() { return this.request('GET', '/api/push/vapid-public-key'); },
  subscribePush(sub) { return this.request('POST', '/api/push/subscribe', sub); },
  getSmileys() { return this.request('GET', '/api/smileys'); }
};
