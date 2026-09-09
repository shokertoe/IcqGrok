# ICQGrok — Full ICQ Messenger Analog

Modern recreation of classic ICQ with:

- **Server**: ASP.NET Core 8 + SignalR + EF Core (SQLite / PostgreSQL)
- **Web PWA**: voice/video calls (WebRTC), E2E encryption (ECDH P-256 + AES-GCM), push (VAPID), smileys (*BANG* 🤦‍♂️)
- **SwiftUI** client (macOS / iOS)
- **Android** Jetpack Compose skeleton

## Quick start

```bash
cd docker
docker compose up -d --build
# Server: http://localhost:5000
# Web client: http://localhost:5000/web/
```

Or run server directly:

```bash
cd Server
dotnet run
```

## Features

- UIN + email registration, JWT + refresh tokens
- Buddy list / contacts, real-time chats, typing indicators, statuses
- File upload
- WebRTC targeted signaling (CallOffer / Answer / ICE / Hangup / Reject)
- End-to-end encryption for web (Web Crypto)
- Classic ICQ smileys + *BANG*
- PWA installable + Web Push

## Structure

```
Server/           # ASP.NET Core backend + wwwroot/web PWA
Client/ICQApp/    # SwiftUI multiplatform
Android/          # Kotlin + Compose
docker/           # Dockerfile + docker-compose (Postgres)
docs/             # E2E & WebRTC notes
```

## License

MIT — build something fun.
