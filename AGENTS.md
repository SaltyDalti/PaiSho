# Pai Sho — Agent Guide

Unity 6 (URP) local Pai Sho prototype. Goal: professional full-stack product (client + authenticated matches + persistence).

## Layout

| Path | Role |
|---|---|
| `Assets/Scripts/Engine/Domain/` | Pure C# rules (no `UnityEngine`). Prefer edits here for board/moves/placement/harmony. |
| `Assets/Scripts/Engine/Board/` | Unity board adapters (`BoardManager`, tiles, spawn). |
| `Assets/Scripts/Engine/Game/` | Turn flow, harmony, capture, seasons, UI-facing managers. |
| `Assets/Scripts/Engine/Pieces/` | Piece MonoBehaviour + `PieceType` enum. |
| `Assets/Scripts/UI/` | uGUI / camera. |
| `Assets/Scenes/SampleScene.unity` | Only playable scene. |
| `docs/` | Architecture and API sketches. |
| `tests/PaiSho.Rules.Tests/` | `dotnet test` coverage for Domain. |

## Hard rules

1. Keep gameplay rules deterministic and testable. New movement/placement/win logic belongs in `Domain/` with tests.
1b. Placement legality goes through `PlacementRules.Evaluate` (Unity: `PlacementValidator`). Spring opening flowers are Host=Jasmine, Opponent=Rose.
1c. Harmony/disharmony goes through `HarmonyRules` (Unity: `HarmonyManager`). Harmony uses Chebyshev distance 1 (includes diagonals); board capture adjacency is still orthogonal-only.
2. Minimize Unity YAML churn: avoid casual edits to `.meta`, `.unity`, `.prefab`, `.asset` unless the task requires it.
3. `Host` / `Opponent` are local roles until networking lands — do not invent Netcode without an agreed match contract (`docs/match-lifecycle-api.md`).
4. Board encoding uses stride **20** (`BoardCoords.CoordStride`), not 19. Do not “fix” that without updating all callers + tests.
5. After Domain changes: run `dotnet test tests/PaiSho.Rules.Tests`.

## Verification

```bash
dotnet test tests/PaiSho.Rules.Tests
```

Unity EditMode playtests still require the editor on a desktop machine.

## Current maturity

Local two-player core loop exists. No auth, matchmaking, server authority, or persistence yet. Several managers still have TODOs/stubs (echo tiles, some visuals).
