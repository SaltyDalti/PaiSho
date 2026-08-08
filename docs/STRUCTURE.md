# Pai Sho — Repository Structure

Working root: this Unity project (repo root). Docs live in `docs/`.

## Layout

```text
PaiSho/                          # ← open THIS in Unity Hub / clone root
  README.md
  .gitignore
  docs/
    RULES.md                     # house rules (design SoT)
    STRUCTURE.md                 # this file
    SHIP.md                      # boot flow + light ship checklist
  Assets/
    PaiSho/                      # product code
      Runtime/
      Editor/
      Tests/EditMode/
    Resources/                   # Board, PieceVisuals, Scene — keep keys stable
    Prefabs/ Scenes/ Materials/ Furniture/ Settings/
    TextMesh Pro/
  SourceArt/Blender/             # local only (gitignored) — NOT in player builds
  Packages/
  ProjectSettings/
```

## Assemblies

| Assembly | Role |
|----------|------|
| `PaiSho.Runtime` | All play-mode game code |
| `PaiSho.Editor` | Editor-only bakers and menus (references Runtime) |
| `PaiSho.Tests.EditMode` | Edit Mode tests (fill as rules stabilize) |

## Notes

- Runtime visuals load from `Resources/` (`Board/`, `PieceVisuals/`, `Scene/`).
- `Assets/Models/` and `SourceArt/` may exist locally for bakers; they are gitignored duplicates of source inputs.
- Prior Domain/Scripts tip: branch/tag `archive/domain-bd58660`.
