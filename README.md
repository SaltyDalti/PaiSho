# Pai Sho

Unity 6 (URP) Pai Sho prototype — local two-player core loop, expanding toward authenticated online matches.

## Quick start (Unity)

1. Open the project in **Unity 6000.0.47f1**.
2. Open `Assets/Scenes/SampleScene.unity` (the only playable scene).
3. Enter Play Mode.

## Agent / CI tooling

| Path | Purpose |
|---|---|
| `AGENTS.md` | How Cursor/Cloud Agents should work in this repo |
| `.cursor/` | Rules, Bugbot policy, Cloud Agent environment |
| `Assets/Scripts/Engine/Domain/` | Pure rules (no UnityEngine) |
| `tests/PaiSho.Rules.Tests/` | Headless rules tests |
| `docs/match-lifecycle-api.md` | Future server match contract |

```bash
dotnet test tests/PaiSho.Rules.Tests
```

## Status

Playable local match scaffolding exists. Online auth, matchmaking, and persistence are not implemented yet — see the match lifecycle sketch before adding networking.
