# VoiceAgent Frontend Integration Prompt (Voice Mode First)

Use this document as an implementation prompt for frontend and product teams.

---

## Primary Goal (Voice Mode)

Build a voice-first demo flow where:
1. User lands on the page.
2. Frontend loads available demo campaigns.
3. User selects campaign and clicks **Start Voice Demo**.
4. Frontend creates a voice session via HTTP.
5. Frontend opens **one WebSocket** connection for real-time bidirectional audio.
6. Frontend streams mic audio chunks to backend.
7. Frontend receives synthesized bot audio chunks from backend and plays them.
8. User clicks **Stop** to end call cleanly.
9. Frontend calls end-session HTTP endpoint and closes socket.

---

## Backend Endpoints to Use

### Voice lifecycle (HTTP)
- `POST /api/voice/session/start`
  - Input: `tenantId`, `clientId`, `campaignId`, `channel`
  - Output: `callSessionId`, `correlationId`
- `POST /api/voice/session/end`
  - Input: `callSessionId`
  - Output: confirmation

### Voice stream (WebSocket)
- `WS /api/voice/web-stream` for web demo channel
- `WS /api/voice/phone-stream` for phone bridge channel

### Demo campaign discovery (for dropdown)
- `GET /api/demo/campaigns`

---

## Important Design Decision: One WebSocket vs Two

Use **one WebSocket per active call session**.

Reason:
- WebSocket is full-duplex, so same connection can:
  - send client audio upstream (binary)
  - receive bot audio downstream (binary)
- Current backend orchestrator handles this model directly.

Do **not** open separate send/receive sockets unless future scaling constraints force it.

---

## Frontend Flow (Voice Mode) — Step-by-step

### 1) Initial load
- Call `GET /api/demo/campaigns`
- Fill campaign dropdown
- Disable Start button until campaign is selected

### 2) Start voice session
- On `Start Voice Demo` click:
  - POST `/api/voice/session/start`
  - include selected `tenantId/clientId/campaignId`
  - set `channel` to `WebText` or preferred web voice marker used by backend conventions
- Save returned `callSessionId`

### 3) Open WebSocket stream
- Connect to `wss://<host>/api/voice/web-stream`
- On open, send a JSON text frame to attach session:

```json
{
  "type": "session",
  "callSessionId": "<GUID_FROM_START_ENDPOINT>"
}
```

### 4) Stream microphone audio
- Capture microphone PCM/Opus chunks (implementation-specific)
- Send each chunk as binary WS frame
- Maintain a small jitter buffer client-side

### 5) Receive and play bot audio
- Listen for binary frames from WS
- Enqueue and play audio chunks in order
- Optional UX: show speaking indicator while audio is playing

### 6) Stop call
- On Stop button click:
  1. POST `/api/voice/session/end` with `callSessionId`
  2. Close websocket (`code=1000`, reason=`"user ended"`)
  3. Stop mic capture and release media tracks
  4. Move UI to `ended`

### 7) Unexpected disconnect handling
- If WS disconnects unexpectedly:
  - Show reconnect option (if session still active)
  - or auto-end: call `/api/voice/session/end` once
- Avoid duplicate end calls; guard with `hasEnded` flag

---

## Suggested Frontend State Machine (Voice)

- `idle`
- `loading_campaigns`
- `ready`
- `starting_session`
- `connecting_ws`
- `live`
- `ending`
- `ended`
- `error`

Rules:
- Only send audio in `live`
- Disable Start while `starting_session` or `connecting_ws`
- Disable Stop except in `live` or `connecting_ws`

---

## UX Notes for Voice Mode

- Show explicit permission step for microphone access.
- Show call timer after WS attach succeeds.
- Show subtle network quality indicator.
- Add fallback message if audio playback fails.
- Ensure hard stop always terminates local media tracks.

---

## API/Socket Contract Checklist

- [ ] Frontend stores `callSessionId` per active voice tab/session.
- [ ] First WS text frame is always `type=session` envelope.
- [ ] Audio frames are sent only after WS open and session attach.
- [ ] WS close and HTTP end are both executed on Stop.
- [ ] Duplicate Stop actions are idempotent on frontend.

---

## Secondary (Lower Priority): Text Demo Mode

Text mode can continue with standard HTTP request/response:
- `GET /api/demo/campaigns`
- `POST /api/demo/start`
- `POST /api/demo/message` (loop)
- `POST /api/demo/end`

This mode is lower priority and not required for current voice-first milestone.

---

## Copy-paste Prompt You Can Send to Frontend Team

> Implement a voice-first demo page for VoiceAgent.
> 
> Required flow:
> 1) Load campaigns from GET `/api/demo/campaigns`.
> 2) On campaign select + Start, call POST `/api/voice/session/start` and store `callSessionId`.
> 3) Open one WebSocket to `/api/voice/web-stream`.
> 4) Immediately send `{ "type": "session", "callSessionId": "..." }` as text frame.
> 5) Stream mic audio chunks as binary WS messages.
> 6) Receive bot audio chunks as binary WS messages and play them in order.
> 7) On Stop: call POST `/api/voice/session/end`, close WS with normal closure, release mic.
> 8) Add robust handling for reconnect/end idempotency and unexpected socket closure.
> 
> Notes:
> - Use one websocket per active call; do not split into send/receive sockets.
> - Build UI state machine: idle → ready → starting_session → connecting_ws → live → ending → ended.
> - Keep text demo mode support as low-priority fallback only.

