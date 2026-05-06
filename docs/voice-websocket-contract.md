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

## Close behavior

- Server closes with normal close code `1000` (`NormalClosure`) when done.
- Client should gracefully close its socket when call/session flow ends.
- Frontend should also call `POST /api/voice/session/end` to finalize and persist end-of-session state.
