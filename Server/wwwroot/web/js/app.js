/* ICQ Messenger — Web PWA client */
const state = {
  user: null,
  chats: [],
  contacts: [],
  messages: {},
  activeChatId: null,
  tab: "chats",
  hub: null,
  typing: {},
  searchQ: "",
  searchResults: []
};

const $ = (sel, el = document) => el.querySelector(sel);
const $$ = (sel, el = document) => [...el.querySelectorAll(sel)];

function toast(msg) {
  const t = $("#toast");
  t.textContent = msg;
  t.classList.add("show");
  setTimeout(() => t.classList.remove("show"), 2800);
}

function statusClass(s) {
  if (s === 1 || s === "Online") return "online";
  if (s === 2 || s === "Away") return "away";
  if (s === 3 || s === "Busy") return "busy";
  return "";
}

function chatTitle(chat) {
  if (chat.title) return chat.title;
  const other = (chat.participants || []).find((p) => p.id !== state.user?.id);
  return other?.nickname || "Chat";
}

function initial(name) {
  return (name || "?").charAt(0).toUpperCase();
}

// ── Render ──────────────────────────────────────────────────────

function render() {
  const app = $("#app");
  if (!state.user) {
    app.innerHTML = renderLogin();
    bindLogin();
    return;
  }
  app.innerHTML = renderMain();
  bindMain();
  if (state.activeChatId) scrollMessages();
}

function renderLogin() {
  return `
  <div class="login-screen">
    <div class="login-card">
      <div class="login-header">
        <div class="logo">💬</div>
        <h1>ICQ</h1>
        <p>Messenger · Online again</p>
      </div>
      <div class="login-body">
        <label>Nickname or Email</label>
        <input id="login-nick" autocomplete="username" placeholder="your_nick" />
        <label>Password</label>
        <input id="login-pass" type="password" autocomplete="current-password" placeholder="••••••••" />
        <div id="reg-extra" style="display:none;flex-direction:column;gap:12px">
          <label>Email (optional)</label>
          <input id="login-email" type="email" placeholder="you@mail.com" />
        </div>
        <div class="error-msg" id="login-err"></div>
        <button class="btn-primary" id="login-btn">Login 🙂</button>
        <button class="link-btn" id="toggle-reg">New here? Create account</button>
      </div>
    </div>
  </div>`;
}

function bindLogin() {
  let isReg = false;
  $("#toggle-reg").onclick = () => {
    isReg = !isReg;
    $("#reg-extra").style.display = isReg ? "flex" : "none";
    $("#login-btn").textContent = isReg ? "Register 🎉" : "Login 🙂";
    $("#toggle-reg").textContent = isReg ? "Have an account? Login" : "New here? Create account";
    $("#login-err").textContent = "";
  };
  $("#login-btn").onclick = async () => {
    const nick = $("#login-nick").value.trim();
    const pass = $("#login-pass").value;
    const email = $("#login-email")?.value.trim() || null;
    if (!nick || pass.length < 6) {
      $("#login-err").textContent = "Nickname and password (min 6) required";
      return;
    }
    $("#login-btn").disabled = true;
    try {
      const res = isReg
        ? await API.register(email,pass, nick)
        : await API.login(nick, pass);
      API.setTokens(res);
      state.user = res.user;
      await bootSession();
      render();
      toast(`Welcome, ${res.user.nickname}! UIN ${res.user.uin} 🎉`);
    } catch (e) {
      $("#login-err").textContent = e.message;
    } finally {
      $("#login-btn").disabled = false;
    }
  };
  $("#login-pass").onkeydown = (e) => { if (e.key === "Enter") $("#login-btn").click(); };
}

function renderMain() {
  const u = state.user;
  return `
  <div class="install-banner" id="install-banner">
    📲 <b>iPhone:</b> Share → <b>Add to Home Screen</b> to get notifications like a real app
  </div>
  <div class="main-layout ${state.activeChatId ? "chat-open" : ""}" id="layout">
    <aside class="sidebar">
      <div class="sidebar-header">
        <div class="avatar">${initial(u.nickname)}</div>
        <div class="user-meta">
          <div class="nick">${esc(u.nickname)}</div>
          <div class="uin">UIN ${u.uin}</div>
        </div>
        <span class="status-dot online" title="Online"></span>
        <button title="Enable notifications" id="btn-notify" style="color:#fff;font-size:18px;padding:4px">🔔</button>
        <button title="Logout" id="btn-logout" style="color:#fff;font-size:18px;padding:4px">🚪</button>
      </div>
      <div class="tabs">
        <button data-tab="chats" class="${state.tab === "chats" ? "active" : ""}">Chats 💬</button>
        <button data-tab="contacts" class="${state.tab === "contacts" ? "active" : ""}">Buddies 👥</button>
      </div>
      <div class="search-bar">
        <input id="search" placeholder="${state.tab === "chats" ? "Search chats…" : "Find by nick or UIN…"}" value="${esc(state.searchQ)}" />
      </div>
      <div class="list-scroll" id="list">${state.tab === "chats" ? renderChatList() : renderContactList()}</div>
    </aside>
    <section class="chat-panel">
      ${state.activeChatId ? renderChat() : renderEmpty()}
    </section>
  </div>
  <div class="toast" id="toast"></div>
  <div class="call-overlay hidden" id="call-overlay">
    <div class="call-type-badge" id="call-type">Voice call</div>
    <div class="call-avatar-big" id="call-avatar">?</div>
    <div class="call-peer-name" id="call-peer-name">Buddy</div>
    <div class="call-status" id="call-status">Calling…</div>
    <div class="call-videos" id="call-videos">
      <video id="remoteVideo" autoplay playsinline></video>
      <video id="localVideo" autoplay playsinline muted></video>
    </div>
    <div class="call-controls" id="call-controls"></div>
  </div>`;
}

function renderChatList() {
  let chats = state.chats;
  if (state.searchQ) {
    const q = state.searchQ.toLowerCase();
    chats = chats.filter((c) => chatTitle(c).toLowerCase().includes(q));
  }
  if (!chats.length) {
    return `<div class="empty-chat" style="padding:40px 16px"><div class="big">💭</div><p>No chats yet. Open Buddies and say hi!</p></div>`;
  }
  return chats.map((c) => {
    const title = chatTitle(c);
    const last = c.lastMessage;
    const preview = last ? expandSmileys(last.text || "") : "No messages yet";
    const active = c.id === state.activeChatId ? "active" : "";
    return `
    <div class="list-item ${active}" data-chat="${c.id}">
      <div class="item-avatar">${initial(title)}
        <span class="sdot ${statusClass(1)}"></span>
      </div>
      <div class="item-body">
        <div class="item-title">${esc(title)}</div>
        <div class="item-sub">${esc(preview)}</div>
      </div>
      ${c.unreadCount ? `<span class="badge">${c.unreadCount}</span>` : ""}
    </div>`;
  }).join("");
}

function renderContactList() {
  if (state.searchQ && state.searchResults.length) {
    return state.searchResults.map((u) => `
      <div class="list-item" data-user="${u.id}">
        <div class="item-avatar">${initial(u.nickname)}
          <span class="sdot ${statusClass(u.status)}"></span>
        </div>
        <div class="item-body">
          <div class="item-title">${esc(u.nickname)}</div>
          <div class="item-sub">UIN ${u.uin} · tap to chat</div>
        </div>
      </div>`).join("");
  }
  if (!state.contacts.length) {
    return `<div class="empty-chat" style="padding:40px 16px">
      <div class="big">👋</div>
      <p>Search by nickname or UIN above,<br>or add a buddy.</p>
      <button class="btn-primary" id="btn-add-uin" style="margin-top:12px">Add by UIN</button>
    </div>`;
  }
  return state.contacts.map((c) => {
    const name = c.nicknameOverride || c.user.nickname;
    return `
    <div class="list-item" data-user="${c.user.id}">
      <div class="item-avatar">${initial(name)}
        <span class="sdot ${statusClass(c.user.status)}"></span>
      </div>
      <div class="item-body">
        <div class="item-title">${esc(name)}</div>
        <div class="item-sub">UIN ${c.user.uin}${c.user.statusMessage ? " · " + esc(c.user.statusMessage) : ""}</div>
      </div>
    </div>`;
  }).join("");
}

function renderEmpty() {
  return `
  <div class="empty-chat">
    <div class="big">🐧</div>
    <h2>ICQ Messenger</h2>
    <p>Pick a chat or find a buddy.<br>Try the legendary <b>*BANG*</b> smiley 🤕🧱</p>
  </div>`;
}

function renderChat() {
  const chat = state.chats.find((c) => c.id === state.activeChatId);
  const title = chat ? chatTitle(chat) : "Chat";
  const msgs = state.messages[state.activeChatId] || [];
  const typing = state.typing[state.activeChatId];

  const smileyRows = SMILEY_PICKER.map((row) =>
    row.map((code) => {
      const emoji = ICQ_SMILEYS[code] || code;
      const bang = code.includes("BANG") || code.includes("HEADBANG") || code.includes("WALL");
      return `<button type="button" data-code="${esc(code)}" title="${esc(code)}" class="${bang ? "bang" : ""}">${emoji}</button>`;
    }).join("")
  ).join("");

  return `
  <div class="chat-top">
    <button class="back-btn" id="btn-back">←</button>
    <div class="title">${esc(title)}</div>
    <div class="actions">
      <button title="Voice call" id="btn-call">📞</button>
      <button title="Video call" id="btn-video">📹</button>
    </div>
  </div>
  <div class="messages" id="messages">
    ${msgs.map((m) => renderMsg(m)).join("")}
  </div>
  <div class="typing-bar">${typing ? "typing…" : ""}</div>
  <div class="composer">
    <div class="smiley-bar" id="smiley-bar">${smileyRows}</div>
    <div class="composer-row">
      <button class="icon-btn" id="btn-smile" title="Smileys">😊</button>
      <button class="icon-btn" id="btn-file" title="Attach">📎</button>
      <textarea id="msg-input" rows="1" placeholder="Message…  try *BANG*"></textarea>
      <button class="send-btn" id="btn-send" title="Send">➤</button>
    </div>
  </div>`;
}

function renderMsg(m) {
  const mine = m.senderId === state.user.id;
  let text = m.text || "";
  if (m.isEncrypted && !m._decrypted) {
    text = "🔒 …";
  } else {
    text = expandSmileys(text);
  }
  const lock = m.isEncrypted ? "🔒 " : "";
  let extra = "";
  if (m.attachmentUrl) {
    const src = m.attachmentUrl.startsWith("http") ? m.attachmentUrl : location.origin + m.attachmentUrl;
    if (m.type === 1 || /\.(jpg|jpeg|png|gif|webp)$/i.test(m.attachmentUrl)) {
      extra = `<img class="attach" src="${esc(src)}" alt="image" />`;
    } else {
      extra = `<a href="${esc(src)}" target="_blank" rel="noopener">📎 ${esc(m.attachmentName || "File")}</a>`;
    }
  }
  const time = m.sentAt ? new Date(m.sentAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : "";
  return `
  <div class="msg ${mine ? "me" : "them"}">
    ${!mine ? `<div class="sender">${esc(m.senderNickname || "")}</div>` : ""}
    <div class="bubble">${lock}${escHtmlWithEmoji(text)}${extra}</div>
    <div class="time">${time}</div>
  </div>`;
}

async function decryptMessagesForChat(chatId) {
  const chat = state.chats.find((c) => c.id === chatId);
  const peerId = E2E.peerIdFromChat(chat, state.user.id);
  const list = state.messages[chatId];
  if (!list || !peerId) return;
  let changed = false;
  for (const m of list) {
    if (m.isEncrypted && !m._decrypted && m.text) {
      try {
        const keyPeer = m.senderId === state.user.id ? peerId : m.senderId;
        m.text = await E2E.decryptText(m.text, keyPeer, API);
        m._decrypted = true;
        changed = true;
      } catch (e) {
        m.text = "🔒 [cannot decrypt]";
        m._decrypted = true;
        changed = true;
      }
    }
  }
  if (changed) render();
}

function esc(s) {
  return String(s ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function escHtmlWithEmoji(s) {
  return esc(s);
}

function bindMain() {
  const isIos = /iphone|ipad|ipod/i.test(navigator.userAgent);
  const isStandalone = window.matchMedia("(display-mode: standalone)").matches || navigator.standalone;
  if (isIos && !isStandalone) $("#install-banner")?.classList.add("show");

  $("#btn-logout").onclick = () => {
    state.hub?.disconnect();
    API.clearAuth();
    state.user = null;
    render();
  };

  $("#btn-notify").onclick = () => enablePush().then(() => toast("Notifications ready 🔔")).catch((e) => toast(e.message));

  $$(".tabs button").forEach((b) => {
    b.onclick = () => { state.tab = b.dataset.tab; state.searchQ = ""; render(); };
  });

  const search = $("#search");
  let searchTimer;
  search.oninput = () => {
    state.searchQ = search.value;
    clearTimeout(searchTimer);
    if (state.tab === "contacts" && state.searchQ.length >= 2) {
      searchTimer = setTimeout(async () => {
        try {
          const r = await API.searchUsers(state.searchQ);
          state.searchResults = r.users || r || [];
          render();
          const s = $("#search"); if (s) { s.focus(); s.value = state.searchQ; }
        } catch {}
      }, 300);
    } else {
      state.searchResults = [];
      render();
      const s = $("#search"); if (s) { s.focus(); s.value = state.searchQ; }
    }
  };

  $$("[data-chat]").forEach((el) => {
    el.onclick = () => openChat(el.dataset.chat);
  });
  $$("[data-user]").forEach((el) => {
    el.onclick = async () => {
      try {
        const chat = await API.createPrivate(el.dataset.user);
        if (!state.chats.find((c) => c.id === chat.id)) state.chats.unshift(chat);
        await openChat(chat.id);
      } catch (e) { toast(e.message); }
    };
  });

  $("#btn-add-uin")?.addEventListener("click", async () => {
    const uin = prompt("Enter UIN number:");
    if (!uin) return;
    try {
      await API.addContact(Number(uin));
      state.contacts = await API.getContacts();
      toast("Contact request sent 🙂");
      render();
    } catch (e) { toast(e.message); }
  });

  if (state.activeChatId) bindChat();
}

function bindChat() {
  $("#btn-back")?.addEventListener("click", () => {
    state.activeChatId = null;
    render();
  });

  $("#btn-smile").onclick = () => $("#smiley-bar").classList.toggle("open");

  $$("#smiley-bar button").forEach((b) => {
    b.onclick = () => {
      const code = b.dataset.code;
      const ta = $("#msg-input");
      const emoji = ICQ_SMILEYS[code] || code;
      insertAtCursor(ta, emoji + " ");
      ta.focus();
    };
  });

  $("#btn-file").onclick = () => {
    const inp = document.createElement("input");
    inp.type = "file";
    inp.accept = "image/*,.pdf,.zip,.txt";
    inp.onchange = async () => {
      const file = inp.files?.[0];
      if (!file) return;
      try {
        toast("Uploading…");
        const up = await API.upload(file);
        const body = {
          chatId: state.activeChatId,
          text: up.fileName || "File",
          type: up.suggestedType ?? (up.url.match(/\.(jpg|png|gif|webp)/i) ? 1 : 2),
          attachmentUrl: up.url,
          attachmentName: up.fileName,
          attachmentSize: up.size,
          clientMessageId: crypto.randomUUID()
        };
        await sendPayload(body);
      } catch (e) { toast(e.message); }
    };
    inp.click();
  };

  const ta = $("#msg-input");
  ta.onkeydown = (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      $("#btn-send").click();
    }
  };
  let typingTimer;
  ta.oninput = () => {
    state.hub?.invoke("Typing", state.activeChatId, true);
    clearTimeout(typingTimer);
    typingTimer = setTimeout(() => state.hub?.invoke("Typing", state.activeChatId, false), 1500);
  };

  $("#btn-send").onclick = async () => {
    const text = ta.value.trim();
    if (!text) return;
    ta.value = "";
    await sendPayload({
      chatId: state.activeChatId,
      text,
      type: 0,
      clientMessageId: crypto.randomUUID()
    });
  };

  $("#btn-call")?.addEventListener("click", () => showCallMenu());
  $("#btn-video")?.addEventListener("click", () => startOutgoingCall(false));
}

function peerFromActiveChat() {
  const chat = state.chats.find((c) => c.id === state.activeChatId);
  if (!chat) return null;
  const other = (chat.participants || []).find((p) => p.id !== state.user?.id);
  return other ? { id: other.id, name: other.nickname || "Buddy" } : null;
}

function showCallMenu() {
  const peer = peerFromActiveChat();
  if (!peer) { toast("No peer in this chat"); return; }
  startOutgoingCall(true);
}

async function startOutgoingCall(audioOnly) {
  const peer = peerFromActiveChat();
  if (!peer) { toast("No peer in this chat"); return; }
  if (!state.hub?.connected) { toast("Not connected to server"); return; }
  try {
    await ICQCall.startCall(peer.id, peer.name, !!audioOnly);
  } catch (e) {
    toast(e.message || "Call failed");
  }
}

function insertAtCursor(ta, text) {
  const start = ta.selectionStart ?? ta.value.length;
  const end = ta.selectionEnd ?? ta.value.length;
  ta.value = ta.value.slice(0, start) + text + ta.value.slice(end);
  ta.selectionStart = ta.selectionEnd = start + text.length;
}

async function sendPayload(body) {
  try {
    const chat = state.chats.find((c) => c.id === body.chatId);
    const peerId = ICQE2E.peerIdFromChat(chat, state.user.id);
    if (peerId && body.text && body.type === 0) {
      try {
        body.text = await ICQE2E.encryptText(body.text, peerId, API);
        body.isEncrypted = true;
      } catch {
        /* send plaintext if no keys */
      }
    }
    if (state.hub?.connected) {
      await state.hub.invoke("SendMessage", body);
    } else {
      const msg = await API.sendMessage(body);
      pushLocalMessage(msg);
    }
  } catch (e) {
    toast(e.message);
  }
}

function pushLocalMessage(m) {
  if (!state.messages[m.chatId]) state.messages[m.chatId] = [];
  if (state.messages[m.chatId].some((x) => x.id === m.id || (m.clientMessageId && x.clientMessageId === m.clientMessageId))) return;
  state.messages[m.chatId].push(m);
  const chat = state.chats.find((c) => c.id === m.chatId);
  if (chat) chat.lastMessage = m;
  if (state.activeChatId === m.chatId) {
    render();
    scrollMessages();
    decryptMessagesForChat(m.chatId);
  }
}

async function openChat(chatId) {
  state.activeChatId = chatId;
  try {
    if (state.hub?.connected) await state.hub.invoke("JoinChat", chatId);
    const msgs = await API.getMessages(chatId);
    state.messages[chatId] = msgs || [];
  } catch (e) {
    toast(e.message);
  }
  render();
  scrollMessages();
  await decryptMessagesForChat(chatId);
}

function scrollMessages() {
  const el = $("#messages");
  if (el) el.scrollTop = el.scrollHeight;
}

async function bootSession() {
  await ICQE2E.ensureKeys(API);
  state.chats = await API.getChats();
  try { state.contacts = await API.getContacts(); } catch { state.contacts = []; }
  state.hub = createHub({
    onMessage: (m) => {
      pushLocalMessage(m);
      if (state.activeChatId === m.chatId) decryptMessagesForChat(m.chatId);
    },
    onTyping: (chatId, userId, isTyping) => {
      if (userId === state.user?.id) return;
      state.typing[chatId] = isTyping;
      if (state.activeChatId === chatId) render();
    },
    onStatus: () => {},
    onError: (err) => toast(String(err))
  });
  await state.hub.connect();
  if (window.ICQCall) ICQCall.attachHub(state.hub);
}

async function enablePush() {
  if (!("serviceWorker" in navigator) || !("PushManager" in window)) {
    throw new Error("Push not supported in this browser");
  }
  const reg = await navigator.serviceWorker.register("/web/sw.js");
  const perm = await Notification.requestPermission();
  if (perm !== "granted") throw new Error("Notification permission denied");
  const { publicKey } = await API.vapidPublicKey();
  if (!publicKey) throw new Error("Server has no VAPID key");
  const sub = await reg.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: urlBase64ToUint8Array(publicKey)
  });
  const json = sub.toJSON();
  await API.webPushSubscribe({
    endpoint: json.endpoint,
    keys: { p256dh: json.keys.p256dh, auth: json.keys.auth }
  });
}

function urlBase64ToUint8Array(base64String) {
  const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
  const raw = atob(base64);
  const arr = new Uint8Array(raw.length);
  for (let i = 0; i < raw.length; i++) arr[i] = raw.charCodeAt(i);
  return arr;
}

(async function main() {
  if (API.token) {
    try {
      const me = await API.me();
      state.user = { id: me.id, nickname: me.nickname || "User", uin: me.uin || 0 };
      // refresh full user via login payload if possible — me() is minimal
      const stored = localStorage.getItem("icq_user");
      if (stored) {
        try { state.user = { ...state.user, ...JSON.parse(stored) }; } catch {}
      }
      await bootSession();
    } catch {
      API.clearAuth();
      state.user = null;
    }
  }
  render();
  navigator.serviceWorker?.addEventListener("message", (ev) => {
    if (ev.data?.type === "open-chat" && ev.data.chatId) openChat(ev.data.chatId);
  });
})();
