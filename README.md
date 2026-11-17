# Pai Sho

This is a Unity 6 URP garden-rules Pai Sho match. From the title screen you play spring, then the match itself, against the computer or in hotseat, until a harmony ring or a forfeit ends it.

The same tree is what a reviewer reads for the game-rules code. Product code is under `Assets/PaiSho`. `Assets/PaiSho/Runtime/Domain` does not reference `UnityEngine`. The headless tests cover that Domain layer.

## Open

1. Install Unity **6000.0.47f1** (or the version in `ProjectSettings/ProjectVersion.txt`).
2. Open this folder as the Unity project.
3. Play `Assets/Scenes/GamePlay.unity`.

That scene is the match. The title is an overlay on it: New Game vs Computer, or New Game Hotseat. `ProjectSettings/ProjectVersion.txt` records editor `6000.0.47f1`. The Universal Render Pipeline package is `com.unity.render-pipelines.universal`.

## Verify rules

```bash
dotnet test tests/PaiSho.Rules.Tests
```

`tests/PaiSho.Rules.Tests` compiles `Assets/PaiSho/Runtime/Domain` and `Assets/PaiSho/Runtime/Pieces/PieceType.cs` and runs them with xUnit on .NET 8, outside the Unity editor. Unity Edit Mode tests still need the editor on a desktop machine. `.github/workflows/rules-tests.yml` runs the same `dotnet test` on pushes to `master` or `main`, and on pull requests.

## Docs

- [docs/RULES.md](docs/RULES.md) — house garden rules for this match
- [docs/STRUCTURE.md](docs/STRUCTURE.md) — repository layout and assemblies
- [docs/SHIP.md](docs/SHIP.md) — title-to-forfeit checklist
- [docs/match-lifecycle-api.md](docs/match-lifecycle-api.md) — sketch for a future match API
- [docs/DESIGN.md](docs/DESIGN.md) — why the rules code is split the way it is

## Layout

- `Assets/PaiSho/` — product code (Runtime, Editor, Tests)
- `Assets/PaiSho/Runtime/Domain/` — pure rules, covered by the headless tests
- `Assets/PaiSho/Runtime/Game/` and `Assets/PaiSho/Runtime/Board/` — the match you play (turns, AI, trays, board)
- `tests/PaiSho.Rules.Tests/` — `dotnet test` project for Domain
- `SourceArt/` — Blender sources (local and gitignored; not required to play)

## Ship gate

[docs/SHIP.md](docs/SHIP.md) is the vertical slice: title, spring, play, AI or hotseat, ring or forfeit, then play again. Feature work should leave that path completable.

## Archive

The previous Domain/Scripts tip is preserved on branch and tag `archive/domain-bd58660`.
