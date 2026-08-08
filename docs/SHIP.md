# Pai Sho — Light ship notes

Working root: this Unity project repo. Docs live in `docs/`.

This is a **plan and checklist**, not a migration in progress. Do not rip `Resources` until Waves 1–4 feel solid in playtests.

---

## Boot / title → match flow (current)

There is **no separate Boot scene**. Build Settings currently use a single scene:

- `Assets/Scenes/GamePlay.unity`

Runtime chain:

1. Scene loads → `GameBootstrap` auto-creates (`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`).
2. `Awake` ensures all managers, including `TitleMenu` and `GameUI`.
3. `Start` coroutine: board setup → `TitleMenu.WaitUntilMatchRequested()` → apply `GameSession.AiEnabled` → spring prep → first AI turn if needed.
4. Title is a **HUD overlay** on GamePlay (`TitleMenu`), not a scene load.
5. New Game / AI / hotseat dismisses the overlay via `GameSession.MarkMatchStarted()`.
6. Headless / self-play skips the title block when `HeadlessActionExecutor.IsActive`.

Key scripts:

| Script | Role |
|--------|------|
| `Runtime/Game/GameBootstrap.cs` | Scene entry, manager wire-up |
| `Runtime/Game/TitleMenu.cs` | Title overlay UI |
| `Runtime/Game/GameSession.cs` | AI flag, match-started, audio prefs |

### Optional later (not this phase)

- Dedicated `Boot.unity` that only shows branding then loads GamePlay async.
- Keep title overlay if Boot is unnecessary for mobile.

---

## Addressables migration plan (do not execute yet)

Today loaders use **`Resources`** with stable keys under:

- `Resources/Board/`
- `Resources/PieceVisuals/`
- `Resources/Scene/`
- `Resources/Game/` (if present)

**Rules until migration:**

1. Do not rename `Resources` keys without updating every loader.
2. Prefer GUID-safe asset moves (keep `.meta`).
3. Ship Addressables as a **parallel** load path first; cut over when both work.

### Suggested phases

| Phase | Work |
|-------|------|
| A | Add `com.unity.addressables`; create groups for pieces, board, scene props, UI fonts |
| B | Wrap `PieceVisualLoader` / board prefab loads behind an interface: `Resources` impl + Addressables impl |
| C | Label remote vs local groups (tiles local; optional DLC remote) |
| D | Remove `Resources` copies once Addressables path is proven on device |
| E | CI: Addressables build step before player builds |

### Why wait

Addressables changes bake pipelines, first-load latency, and editor workflows. Gameplay polish (integrity, AI tempo, Momentum/Echo, accent AI) should stay unblocked.

---

## Pre-ship checklist (light)

- [ ] Play Title → New Game (AI) → spring → Play → ring or forfeit → Play Again
- [ ] Confirm self-play digests show Host/Opponent winners (not Incomplete) after rings
- [ ] Audio mute / volume from title Settings
- [ ] Mobile touch: drag place, drag move, Echo chooser, End Turn
- [ ] Build Readiness validator (`Pai Sho → Build Readiness` if present)
- [ ] `.gitignore` excludes `Logs/`, Library, local digests
- [ ] Optional: sync Desktop working tree → T5 backup when structure is stable

---

## Source of truth

- **Active work tree:** Desktop `PaiSho` (or your clone).
- **Rules:** `Docs/RULES.md`
- **Layout:** `Docs/STRUCTURE.md`
- T5 copies are backups unless you deliberately switch roots.
