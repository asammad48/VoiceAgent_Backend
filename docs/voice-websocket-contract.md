# Voice WebSocket Contract

This document defines the backend WebSocket contract for live voice streaming.

## WebSocket routes

- `WS /api/voice/web-stream`
- `WS /api/voice/phone-stream`

Both routes are full-duplex streams on a single WebSocket connection.

## Session attach (first text frame)

The client must send this JSON message as the first text frame:

```json
{
  "type": "session",
  "callSessionId": "GUID"
}
```

- `type` must be `session`.
- `callSessionId` must be a valid GUID returned by `POST /api/voice/session/start`.

## Audio frame direction

After the session is attached:

- Client -> Server: binary microphone audio frames.
- Server -> Client: binary bot audio frames.

Do not open separate send/receive sockets. A single socket handles both directions.

## Control frames (Server -> Client, JSON text)

The server sends text frames alongside audio to signal bot turn lifecycle:

| Frame | When sent |
|---|---|
| `{ "type": "bot_started" }` | Immediately before bot audio is sent |
| `{ "type": "bot_ended" }` | Immediately after bot audio is sent |
| `{ "type": "call_ended", "reason": "..." }` | After the final TTS audio has had time to play |

### `isClosing` flag

When the bot's reply is the **final turn** of the call, `bot_started` and `bot_ended` carry an extra field:

```json
{ "type": "bot_started", "isClosing": true }
{ "type": "bot_ended",   "isClosing": true }
```

**Recommended client behaviour on `isClosing: true`:**
1. Play the audio chunk to completion (do not cut short).
2. After playback finishes, send a WebSocket close frame (`code=1000`).
3. Do **not** wait for the `call_ended` frame — the server will also send it after an estimated playback delay, but a client-initiated close is preferred.

This ensures the closing line is always heard in full before the connection drops.

## Close behavior

- Server sends `call_ended` after a delay calculated from the final audio duration, ensuring the audio has finished playing on the client before the signal arrives.
- Server then waits briefly for the client to send a close frame.
- Server closes with normal close code `1000` (`NormalClosure`) after that.
- Frontend should also call `POST /api/voice/session/end` to finalize and persist end-of-session state.
