# Pai Sho (Unity 6 game)

A 3D, offline/hot-seat implementation of the board game **Pai Sho**, built with Unity.
There is no backend, database, or network layer — the entire product runs in-process in the
Unity Editor / a built player.

- Engine version: `6000.0.47f1` (pinned in `ProjectSettings/ProjectVersion.txt`)
- Render pipeline: Universal Render Pipeline (URP)
- Language: C# under `Assets/Scripts/` (namespaces `PaiSho.*`)
- **Playable scene: `Assets/Scenes/SampleScene.unity`** (contains GameManager, BoardSpawner, UI, etc.)
- `Assets/Scenes/GamePlay.unity` is an empty stub (camera/light/plane only) — do not use it to run the game
- Package manager: Unity Package Manager (`Packages/manifest.json` + `Packages/packages-lock.json`)

## Cursor Cloud specific instructions

The Unity Editor is the only "service". It must be **licensed** before it will do anything
(import, compile, build, or Play Mode). Everything below assumes the environment snapshot /
update script has already installed the editor and system libraries.

### Where things live (installed outside the repo, persisted via the VM snapshot)
- Unity Editor binary: `/opt/unity/editor/Editor/Unity` (version `6000.0.47f1`)
- Helper scripts: `/opt/unity/bin/unity-activate.sh`, `/opt/unity/bin/unity-run.sh`
- Licensing Client: `/opt/unity/editor/Editor/Data/Resources/Licensing/Client/Unity.Licensing.Client`
- Manual activation file (`.alf`): `/opt/unity/Unity_v6000.0.47f1.alf`
- License cache: `~/.config/unity3d/Unity/licenses/`

### Licensing (REQUIRED first step, and the main gotcha)
Unity has **no anonymous license**. A free **Personal** license is enough (no serial required).

**Important:** In Unity 6, `Unity -batchmode -username … -password …` alone logs in but does **not**
grant a Personal entitlement (`com.unity.editor.headless was not found`). Always activate via the
helper, which uses the Licensing Client for Personal:

```bash
UNITY_EMAIL=... UNITY_PASSWORD=... /opt/unity/bin/unity-activate.sh
# under the hood: Unity.Licensing.Client --activate-all --include-personal --username … --password …
```

- Pro/Plus: also set `UNITY_SERIAL=...` (helper then uses the editor `-serial` path)
- Offline/manual: upload `/opt/unity/Unity_v6000.0.47f1.alf` at `https://license.unity3d.com/manual`
  to obtain a `.ulf`, then `UNITY_ULF_PATH=/path/to.ulf /opt/unity/bin/unity-activate.sh`
- Verify: `…/Unity.Licensing.Client --showEntitlements` should list `com.unity.editor`
  (and usually `com.unity.editor.headless`)

### Run / build / "lint"
There is no separate lint step; the compiler is the lint. Headless GL uses Mesa
`llvmpipe` (`LIBGL_ALWAYS_SOFTWARE=1`, already set by the helpers).

- Import + compile scripts (fastest correctness check):
  `/opt/unity/bin/unity-run.sh import`
- Board encoding smoke (no Play Mode):
  ```bash
  xvfb-run -a -s "-screen 0 1280x720x24" \
    /opt/unity/editor/Editor/Unity -batchmode -nographics -projectPath /workspace \
    -executeMethod BoardUtilsSmokeTests.Run -logFile /tmp/unity_boardutils.log -quit
  ```
- **Core loop smoke (Spring place + move — preferred verification):**
  ```bash
  xvfb-run -a -s "-screen 0 1280x720x24" \
    /opt/unity/editor/Editor/Unity -batchmode -projectPath /workspace \
    -executeMethod CoreLoopSmokeTest.Run -logFile /tmp/unity_coreloop.log
  ```
  Result file: `/opt/cursor/artifacts/core_loop_smoke.txt` (`SUCCESS` / `FAIL`).
  Needs a virtual display (do **not** pass `-nographics`). Do not pass `-quit`; the method exits itself.
- Interactive editor on the Desktop (`DISPLAY=:1`):
  `DISPLAY=:1 LIBGL_ALWAYS_SOFTWARE=1 /opt/unity/editor/Editor/Unity -projectPath /workspace`
  Then open **SampleScene** and press Play.
- Batch/Xvfb editor helper: `/opt/unity/bin/unity-run.sh editor`

### Do NOT use Sonnet / computerUse GUI agents for playtesting
Cursor Task `computerUse` agents (especially Sonnet) often hang forever with an empty
**“no thinking”** state and never recover. That path is banned for this repo.

Verify gameplay with:
1. `CoreLoopSmokeTest.Run` / `BoardUtilsSmokeTests.Run` (above), or
2. Direct Desktop editor Play Mode + your own observation.

Do **not** launch `Task`/`computerUse` subagents to click the Unity Game view.

### Other notes / gotchas
- Always wrap headless editor invocations in `xvfb-run` (the helpers do this). `-nographics` is
  fine for import; drop it (and use a virtual/Desktop display) to render Play Mode.
- Play Mode `-executeMethod` must re-bind callbacks **after** `EnteredPlayMode` (domain reload
  clears statics). `CoreLoopSmokeTest` already does this and polls a result file.
- There is **no `BuildScript`/`-executeMethod` build entry point** in the repo. Add a static
  `BuildPipeline.BuildPlayer(...)` editor method first, or build interactively.
- Nested Blender piece prefabs may show as Missing at runtime; `PiecePlacementManager` falls back
  to procedural placeholders so the rules loop stays playable.
- `Assets/Scripts.zip` and `minimal-kiwi-2.0.1.vsix` in the repo root are stray artifacts,
  unrelated to running the game.
