# E2E Encryption & WebRTC

## E2E (web client)

- ECDH P-256 + AES-256-GCM
- Private keys only in browser localStorage
- API: PUT/GET /api/keys/bundle

## WebRTC (web client)

SignalR signaling: CallOffer, CallAnswer, IceCandidate, CallHangup, CallReject.
Media via getUserMedia + RTCPeerConnection (STUN).
