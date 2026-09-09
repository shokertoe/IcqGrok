# ICQ Messenger — Full Stack Clone

Собственный мессенджер в стиле классического ICQ.

| Слой | Стек |
|------|------|
| Server | ASP.NET Core 8, SignalR, EF Core, SQLite **или** PostgreSQL |
| iOS / macOS | SwiftUI |
| Android | Kotlin + Jetpack Compose + official SignalR Java client |
| Deploy | Docker Compose (PostgreSQL + server) |

## Быстрый старт

```bash
cd Server && dotnet restore && dotnet run
# → http://localhost:5000/web/  Swagger: /swagger
```

Docker: `cd docker && docker compose up --build` → http://localhost:8080

## Возможности

- Auth JWT + UIN, buddy list, private/group chats, SignalR
- Web PWA (iOS home screen + Web Push)
- WebRTC voice/video calls
- E2E (ECDH P-256 + AES-GCM) for 1:1 text on web
- ICQ smileys including `*BANG*`
- File upload, FCM push, PostgreSQL/Docker
- SwiftUI + Android Compose clients

Подробнее: см. полный README в архиве репозитория и `docs/E2E_AND_WEBRTC.md`.
