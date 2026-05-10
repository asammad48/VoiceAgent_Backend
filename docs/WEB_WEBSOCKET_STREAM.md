# Web WebSocket Stream — Integration Reference

Full specification for the `/api/voice/web-stream` endpoint: what to send, what you receive, and the complete lifecycle.

---

## Overview

The web voice stream is a **single full-duplex WebSocket connection** that handles real-time audio in both directions:

```
Client  ──── binary audio (mic) ────►  Server
Client  ◄─── binary audio (bot) ────  Server
```

The backend pipeline per audio chunk:

```
mic audio → Deepgram STT → speech-end check → Gemini LLM → ElevenLabs TTS → bot audio
```

Audio is only passed to the LLM when speech-end is detected — not on every chunk.

---

## Step 1 — Start a session (HTTP, before WebSocket)

**`POST /api/voice/session/start`**

### Request body

```json
{
  "tenantId":   "00000000-0000-0000-0000-000000000001",
  "clientId":   "00000000-0000-0000-0000-000000000101",
  "campaignId": "00000000-0000-0000-0000-000000000201",
  "channel":    "WebText"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `tenantId` | GUID | Yes | Tenant identifier |
| `clientId` | GUID | Yes | Client under the tenant |
| `campaignId` | GUID | Yes | Campaign to run (e.g. RestaurantOrder, CabBooking) |
| `channel` | string | Yes | Use `"WebText"` for browser sessions |

### Response

```json
{
  "success": true,
  "data": {
    "callSessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "correlationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "status": "started"
  },
  "message": "Voice session started."
}
```

Save `callSessionId` — it is required to attach the WebSocket.

---

## Step 2 — Connect WebSocket

```
ws://<host>/api/voice/web-stream
wss://<host>/api/voice/web-stream   (TLS)
```

If the request is not a WebSocket upgrade, the server returns **HTTP 400**.

---

## Step 3 — Attach session (first text frame)

Immediately after the WebSocket opens, send this as a **UTF-8 text frame**:

```json
{
  "type": "session",
  "callSessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `type` | string | Yes | Must be exactly `"session"` |
| `callSessionId` | GUID string | Yes | Returned by `POST /api/voice/session/start` |

**Until this frame is received, all binary audio frames sent by the client are silently dropped.**

The server logs a `voice_stream_attached` event internally on success. There is no acknowledgement frame sent back.

---

## Step 4 — Stream microphone audio (client → server)

Send microphone audio as **binary WebSocket frames**.

| Property | Value |
|---|---|
| Frame type | Binary |
| Audio format | WAV (PCM 16-bit, 16 kHz, mono recommended) |
| Max frame size | 32 KB per frame |
| Timing | Continuous chunks as captured from mic |

Each binary frame is individually transcribed by Deepgram (`nova-2` model).

---

## Step 5 — Receive bot audio (server → client)

When speech-end is detected and the bot has a reply, the server sends the synthesised audio as a **single binary WebSocket frame**.

| Property | Value |
|---|---|
| Frame type | Binary |
| Audio format | MP3 (`audio/mpeg`, 44100 Hz, 128 kbps) |
| Voice | Brian — Deep, Resonant and Comforting (ElevenLabs) |
| Timing | One frame per bot turn (not streamed in chunks) |

Play the received bytes directly as an MP3 audio buffer.

---

## Step 6 — End the session

### Option A — Clean stop (user clicks Stop)

1. Call `POST /api/voice/session/end`:

```json
{ "callSessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
```

Response:

```json
{ "success": true, "data": true, "message": "Voice session ended." }
```

2. Close the WebSocket with **code 1000** (`NormalClosure`), reason `"user ended"`.
3. Stop mic capture and release media tracks.

### Option B — Server closes first

The server closes with code `1000` when the conversation reaches a completed state. The client should detect this and call `POST /api/voice/session/end` once if not already called.

---

## Internal event log (per session)

The server writes these events to the database during the session:

| Event type | When |
|---|---|
| `voice_stream_attached` | Session attach text frame received |
| `stt_partial` | Each audio chunk transcribed |
| `tts_audio_sent` | Bot audio frame sent to client |

---

## Complete message flow diagram

```
Client                                    Server
  |                                          |
  |── POST /api/voice/session/start ────────►|
  |◄── { callSessionId, correlationId } ─── |
  |                                          |
  |── WS connect /api/voice/web-stream ─────►|
  |── TEXT { type:"session", callSessionId } ►|  ← attach session
  |                                          |
  |── BINARY <mic audio chunk 1> ───────────►|  → Deepgram STT
  |── BINARY <mic audio chunk 2> ───────────►|  → Deepgram STT
  |── BINARY <mic audio chunk N> ───────────►|  → speech-end detected
  |                                          |     → Gemini LLM reply
  |                                          |     → ElevenLabs TTS
  |◄── BINARY <bot MP3 audio> ──────────────|
  |                                          |
  |── BINARY <mic audio chunk ...> ─────────►|
  |◄── BINARY <bot MP3 audio> ──────────────|
  |                                          |
  |── WS close (1000) ──────────────────────►|
  |── POST /api/voice/session/end ──────────►|
  |◄── { success: true } ───────────────────|
```

---

## Error cases

| Scenario | Behaviour |
|---|---|
| Non-WebSocket HTTP request to `/api/voice/web-stream` | Server returns `400 Bad Request` |
| Binary frame received before session attach | Frame is silently dropped |
| Invalid `callSessionId` in session frame | Session is never attached; all audio dropped |
| Unexpected disconnect | Server loop exits; client should call `session/end` once |
| Bot audio synthesis fails (mock mode) | Empty byte array returned; no binary frame sent |

---

## Quick-start checklist

- [ ] Call `POST /api/voice/session/start` and store `callSessionId`
- [ ] Open WebSocket to `ws://<host>/api/voice/web-stream`
- [ ] Send `{ "type": "session", "callSessionId": "..." }` as first text frame
- [ ] Stream mic audio as binary frames (WAV, 16 kHz, 16-bit PCM)
- [ ] Listen for binary frames and play as MP3
- [ ] On stop: call `POST /api/voice/session/end`, then close WebSocket with code 1000
- [ ] Guard against duplicate stop calls with a `hasEnded` flag
