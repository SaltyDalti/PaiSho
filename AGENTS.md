# Pai Sho — Agent Guide

Unity 6 (URP) local Pai Sho product. Goal: clean vertical slice first, then full-stack matches.

## Layout

| Path | Role |
|---|---|
| `Assets/PaiSho/Runtime/Domain/` | Pure C# rules (no `UnityEngine`). Prefer edits here for board/moves/placement/harmony/victory/capture/seasons/scoring. |
| `Assets/PaiSho/Runtime/Board/` | Unity board adapters (`BoardManager`, layout, lifecycle). |
| `Assets/PaiSho/Runtime/Game/` | Turn flow, AI, trays/pots, input, managers. |
| `Assets/PaiSho/Runtime/Pieces/` | Piece MonoBehaviour + `PieceType` enum. |
| `Assets/PaiSho/Runtime/Presentation/` | Themes, visuals, loaders. |
| `Assets/Scenes/GamePlay.unity` | Only playable scene. |
| `docs/` | RULES / SHIP / STRUCTURE + API sketches. |
| `tests/PaiSho.Rules.Tests/` | `dotnet test` coverage for Domain. |

## Hard rules

1. Keep gameplay rules deterministic and testable. New movement/placement/win logic belongs in `Domain/` with tests.
2. Minimize Unity YAML churn: avoid casual edits to `.meta`, `.unity`, `.prefab`, `.asset` unless the task requires it.
3. `Host` / `Opponent` are local roles until networking lands — do not invent Netcode without an agreed match contract (`docs/match-lifecycle-api.md`).
4. Board encoding uses stride **20** (`BoardCoords.CoordStride`), not 19. Do not “fix” that without updating all callers + tests.
5. After Domain changes: run `dotnet test tests/PaiSho.Rules.Tests`.
6. Product SoT is `Assets/PaiSho/`. Do not reintroduce `Assets/Scripts/` as the game client.
7. Ship gate: `docs/SHIP.md` vertical slice must still complete after gameplay PRs.

## Verification

```bash
dotnet test tests/PaiSho.Rules.Tests
```

Unity EditMode playtests still require the editor on a desktop machine.

## Current maturity

Playable title → spring → play → AI/hotseat → ring/forfeit loop. Domain grafted for headless rules tests; several Unity managers still own match mutation.
