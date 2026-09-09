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
  }
};
