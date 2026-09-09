/* E2E encryption: ECDH P-256 + AES-GCM (Web Crypto) */
const E2E = {
  keyPair: null,
  sharedSecrets: {}, // userId -> CryptoKey

  async init() {
    const stored = localStorage.getItem('icq_e2e_priv');
    if (stored) {
      try {
        const jwk = JSON.parse(stored);
        this.keyPair = {
          privateKey: await crypto.subtle.importKey('jwk', jwk.privateKey, { name: 'ECDH', namedCurve: 'P-256' }, true, ['deriveKey', 'deriveBits']),
          publicKey: await crypto.subtle.importKey('jwk', jwk.publicKey, { name: 'ECDH', namedCurve: 'P-256' }, true, [])
        };
        return;
      } catch {}
    }
    this.keyPair = await crypto.subtle.generateKey({ name: 'ECDH', namedCurve: 'P-256' }, true, ['deriveKey', 'deriveBits']);
    const privJwk = await crypto.subtle.exportKey('jwk', this.keyPair.privateKey);
    const pubJwk = await crypto.subtle.exportKey('jwk', this.keyPair.publicKey);
    localStorage.setItem('icq_e2e_priv', JSON.stringify({ privateKey: privJwk, publicKey: pubJwk }));
  },

  async getPublicKeyJwk() {
    if (!this.keyPair) await this.init();
    return crypto.subtle.exportKey('jwk', this.keyPair.publicKey);
  },

  async getPublicKeyBase64() {
    const jwk = await this.getPublicKeyJwk();
    return btoa(JSON.stringify(jwk));
  },

  async deriveShared(userId, theirPublicJwkOrB64) {
    if (!this.keyPair) await this.init();
    let jwk = theirPublicJwkOrB64;
    if (typeof jwk === 'string') {
      try { jwk = JSON.parse(atob(jwk)); } catch { jwk = JSON.parse(jwk); }
    }
    const theirKey = await crypto.subtle.importKey('jwk', jwk, { name: 'ECDH', namedCurve: 'P-256' }, false, []);
    const shared = await crypto.subtle.deriveKey(
      { name: 'ECDH', public: theirKey },
      this.keyPair.privateKey,
      { name: 'AES-GCM', length: 256 },
      false,
      ['encrypt', 'decrypt']
    );
    this.sharedSecrets[userId] = shared;
    return shared;
  },

  async encryptText(userId, plaintext) {
    let key = this.sharedSecrets[userId];
    if (!key) throw new Error('No shared secret for user ' + userId);
    const iv = crypto.getRandomValues(new Uint8Array(12));
    const enc = new TextEncoder().encode(plaintext);
    const cipher = await crypto.subtle.encrypt({ name: 'AES-GCM', iv }, key, enc);
    const combined = new Uint8Array(iv.length + cipher.byteLength);
    combined.set(iv, 0);
    combined.set(new Uint8Array(cipher), iv.length);
    return btoa(String.fromCharCode(...combined));
  },

  async decryptText(userId, payloadB64) {
    let key = this.sharedSecrets[userId];
    if (!key) throw new Error('No shared secret');
    const raw = Uint8Array.from(atob(payloadB64), c => c.charCodeAt(0));
    const iv = raw.slice(0, 12);
    const data = raw.slice(12);
    const plain = await crypto.subtle.decrypt({ name: 'AES-GCM', iv }, key, data);
    return new TextDecoder().decode(plain);
  },

  async uploadBundle() {
    const pub = await this.getPublicKeyBase64();
    // Simplified: use identity key as signed prekey for demo
    return API.uploadKeyBundle({
      identityKeyPublic: pub,
      signedPreKeyPublic: pub,
      signedPreKeySignature: 'demo',
      signedPreKeyId: 1,
      oneTimePreKeysJson: null
    });
  },

  async ensureSharedWith(userId) {
    if (this.sharedSecrets[userId]) return;
    const bundle = await API.getKeyBundle(userId);
    if (bundle && bundle.identityKeyPublic) {
      await this.deriveShared(userId, bundle.identityKeyPublic);
    }
  }
};
