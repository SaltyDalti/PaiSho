# Pai Sho

Unity 6 URP project for a garden-rules Pai Sho match (title → spring → play → AI/hotseat → ring/forfeit).

## Open

1. Install Unity **6000.0.47f1** (or the version in `ProjectSettings/ProjectVersion.txt`).
2. Open this folder as the Unity project.
3. Play `Assets/Scenes/GamePlay.unity`.

## Layout

- `Assets/PaiSho/` — product code (Runtime / Editor / Tests)
  - `Runtime/Domain/` — pure rules (no UnityEngine); covered by headless tests
- `docs/` — RULES, SHIP checklist, STRUCTURE
- `tests/PaiSho.Rules.Tests/` — `dotnet test` for Domain
- `SourceArt/` — Blender sources (local/gitignored; not required to play)

## Verify rules

```bash
dotnet test tests/PaiSho.Rules.Tests
```

## Ship gate

See `docs/SHIP.md`. Feature work should not land unless the vertical slice still completes.

## Archive

Previous Domain/Scripts tip is preserved on branch/tag `archive/domain-bd58660`.
