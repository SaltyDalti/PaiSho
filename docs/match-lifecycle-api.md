# Match lifecycle API (sketch)

Server-authoritative contract for future authenticated Pai Sho matches.  
Not implemented yet — use this before adding Netcode, Lobby, or a custom backend.

## Goals

- Authoritative rules validation on the server (reuse Domain movement/placement/win logic).
- Persistent match state + move history for async and realtime play.
- Clear client roles mapped from today’s local `Host` / `Opponent`.

## Resources

### `POST /v1/matches`

Create a match.

```json
{
  "mode": "async" | "realtime",
  "ruleset": "garden-v1",
  "seatAssignment": "open" | "invite"
}
```

Response:

```json
{
  "matchId": "m_...",
  "status": "waiting",
  "seats": [
    { "seat": "host", "playerId": null },
    { "seat": "opponent", "playerId": null }
  ],
  "createdAt": "..."
}
```

### `POST /v1/matches/{matchId}/join`

Authenticated player claims a seat.

### `GET /v1/matches/{matchId}`

Full snapshot for clients reconnecting.

```json
{
  "matchId": "m_...",
  "status": "waiting" | "spring" | "active" | "finished" | "aborted",
  "phase": "Spring" | "Play" | "End",
  "currentSeat": "host" | "opponent",
  "board": {
    "pieces": [
      { "id": "p1", "type": "Jasmine", "seat": "host", "coord": 189 }
    ]
  },
  "reserves": { "host": { "Jasmine": 3 }, "opponent": { "Rose": 3 } },
  "version": 12,
  "winnerSeat": null
}
```

`coord` uses the same stride-20 encoding as `BoardCoords`.

### `POST /v1/matches/{matchId}/moves`

Submit one intentional action. Server validates with Domain rules.

```json
{
  "version": 12,
  "action": {
    "type": "place" | "move" | "end_turn" | "resign",
    "pieceType": "Jasmine",
    "fromCoord": null,
    "toCoord": 190,
    "pieceId": null
  }
}
```

Success returns the new snapshot + appended event. Failures:

| Code | Meaning |
|---|---|
| `409 version_conflict` | Client stale; refetch snapshot |
| `422 illegal_action` | Rules rejection (include reason) |
| `403 not_your_turn` | Wrong seat |

### `GET /v1/matches/{matchId}/events?after=12`

Replay/catch-up stream (HTTP poll or WebSocket later).

## Auth

- Bearer JWT (or equivalent) on all match routes.
- Player identity ≠ seat; seats are assigned at join.

## Client mapping (Unity)

| Local today | Online |
|---|---|
| `Player.Host` | seat `host` |
| `Player.Opponent` | seat `opponent` |
| `MovementManager` / Domain | client prediction optional; server authoritative |
| Scene managers | apply snapshots / events; do not own truth |

## Implementation order

1. Keep extracting Domain (placement, harmony, capture, victory) + tests.
2. Stand up minimal API that loads Domain and validates `place`/`move`.
3. Persist matches + events (Postgres or equivalent).
4. Wire Unity client to snapshots (pass-and-play can stay local).
5. Add realtime transport only after async path works.

## Out of scope for v1

- Ranked matchmaking elo
- Spectators / tournaments
- Replay UI polish
- Cross-ruleset migrations beyond `garden-v1`
