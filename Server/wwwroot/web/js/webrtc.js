/* WebRTC voice/video calls */
const ICQCall = {
  pc: null,
  localStream: null,
  remoteStream: null,
  targetUserId: null,
  targetConnectionId: null,
  isVideo: false,
  isCaller: false,
  onState: null,

  _showOverlay() {
    const el = document.getElementById('call-overlay');
    if (el) el.classList.remove('hidden');
  },

  _hideOverlay() {
    const el = document.getElementById('call-overlay');
    if (el) el.classList.add('hidden');
  },

  _updateUI(type, status, peerName) {
    const typeEl = document.getElementById('call-type');
    const nameEl = document.getElementById('call-peer-name');
    const statusEl = document.getElementById('call-status');
    const avatarEl = document.getElementById('call-avatar');
    const controlsEl = document.getElementById('call-controls');
    const videosEl = document.getElementById('call-videos');

    if (typeEl) typeEl.textContent = type || 'Voice call';
    if (nameEl) nameEl.textContent = peerName || 'Buddy';
    if (statusEl) statusEl.textContent = status || '';
    if (avatarEl) avatarEl.textContent = (peerName || '?').charAt(0).toUpperCase();

    if (videosEl) {
      const remote = videosEl.querySelector('#remoteVideo');
      const local = videosEl.querySelector('#localVideo');
      videosEl.style.display = this.isVideo ? 'flex' : 'none';
      if (this.remoteStream && remote) remote.srcObject = this.remoteStream;
      if (this.localStream && local) local.srcObject = this.localStream;
    }

    if (controlsEl) {
      controlsEl.innerHTML = `
        <button class="hangup" onclick="ICQCall.hangup()" title="Hang up">📞</button>
      `;
    }
  },

  async startCall(userId, video = false) {
    this.targetUserId = userId;
    this.isVideo = video;
    this.isCaller = true;
    await this._ensurePc();
    const offer = await this.pc.createOffer();
    await this.pc.setLocalDescription(offer);
    if (window.hub) {
      await window.hub.invoke('CallOffer', userId, JSON.stringify(offer), video ? 'video' : 'audio');
    }
    this._emit('calling');
  },

  async handleOffer(fromUserId, sdp, callType, fromConnectionId) {
    this.targetUserId = fromUserId;
    this.targetConnectionId = fromConnectionId;
    this.isVideo = callType === 'video';
    this.isCaller = false;
    this._emit('incoming', { fromUserId, callType });
    // UI will call accept or reject
    this._pendingOffer = { sdp, fromConnectionId };
  },

  async accept() {
    if (!this._pendingOffer) return;
    await this._ensurePc();
    const offer = JSON.parse(this._pendingOffer.sdp);
    await this.pc.setRemoteDescription(offer);
    const answer = await this.pc.createAnswer();
    await this.pc.setLocalDescription(answer);
    if (window.hub) {
      await window.hub.invoke('CallAnswer', this.targetUserId, JSON.stringify(answer), this._pendingOffer.fromConnectionId);
    }
    this._pendingOffer = null;
    this._emit('connected');
  },

  async handleAnswer(fromUserId, sdp, fromConnectionId) {
    this.targetConnectionId = fromConnectionId;
    if (!this.pc) return;
    await this.pc.setRemoteDescription(JSON.parse(sdp));
    this._emit('connected');
  },

  async handleIce(fromUserId, candidate, fromConnectionId) {
    if (!this.pc) return;
    try {
      await this.pc.addIceCandidate(JSON.parse(candidate));
    } catch (e) { console.warn('ICE add failed', e); }
  },

  async reject() {
    if (window.hub && this.targetUserId) {
      await window.hub.invoke('RejectCall', this.targetUserId, this.targetConnectionId);
    }
    this.hangup(false);
  },

  async hangup(notify = true) {
    if (notify && window.hub && this.targetUserId) {
      try { await window.hub.invoke('Hangup', this.targetUserId, this.targetConnectionId); } catch {}
    }
    if (this.localStream) {
      this.localStream.getTracks().forEach(t => t.stop());
      this.localStream = null;
    }
    if (this.pc) {
      this.pc.close();
      this.pc = null;
    }
    this.targetUserId = null;
    this.targetConnectionId = null;
    this._pendingOffer = null;
    this._hideOverlay();
    this._emit('ended');
  },

  async _ensurePc() {
    if (this.pc) return;
    this.pc = new RTCPeerConnection({
      iceServers: [
        { urls: 'stun:stun.l.google.com:19302' },
        { urls: 'stun:stun1.l.google.com:19302' }
      ]
    });

    this.pc.onicecandidate = (e) => {
      if (e.candidate && window.hub && this.targetUserId) {
        window.hub.invoke('IceCandidate', this.targetUserId, JSON.stringify(e.candidate), this.targetConnectionId);
      }
    };

    this.pc.ontrack = (e) => {
      this.remoteStream = e.streams[0];
      this._emit('remoteStream', this.remoteStream);
    };

    this.pc.onconnectionstatechange = () => {
      if (this.pc.connectionState === 'failed' || this.pc.connectionState === 'disconnected') {
        this.hangup(false);
      }
    };

    this.localStream = await navigator.mediaDevices.getUserMedia({
      audio: true,
      video: this.isVideo
    });
    this.localStream.getTracks().forEach(t => this.pc.addTrack(t, this.localStream));
    this._emit('localStream', this.localStream);
  },

  _emit(event, data) {
    if (typeof this.onState === 'function') this.onState(event, data);

    switch (event) {
      case 'calling':
        this._showOverlay();
        this._updateUI('Voice call', 'Calling...', this.targetUserId);
        break;
      case 'incoming':
        this._showOverlay();
        this._updateUI(data?.callType === 'video' ? 'Video call' : 'Voice call', `Incoming from ${data?.fromUserId || '?'}`);
        break;
      case 'connected':
        this._updateUI(this.isVideo ? 'Video call' : 'Voice call', 'Connected');
        break;
      case 'ended':
      case 'remoteStream':
      case 'localStream':
        this._updateUI();
        break;
    }
  },

  attachHub(hub) {
    window.hub = hub;
    hub.on('CallOffer', async (fromUserId, sdp, callType, fromConnectionId) => {
      await this.handleOffer(fromUserId, sdp, callType, fromConnectionId);
    });
    hub.on('CallAnswer', async (fromUserId, sdp, fromConnectionId) => {
      await this.handleAnswer(fromUserId, sdp, fromConnectionId);
    });
    hub.on('IceCandidate', async (fromUserId, candidate, fromConnectionId) => {
      await this.handleIce(fromUserId, candidate, fromConnectionId);
    });
    hub.on('CallHangup', async (fromUserId, fromConnectionId) => {
      this.hangup(false);
    });
    hub.on('CallReject', async (fromUserId, fromConnectionId) => {
      this.hangup(false);
    });
  }
};
