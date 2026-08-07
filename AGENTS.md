# Pai Sho (Unity 6 game)

A 3D, offline/hot-seat implementation of the board game **Pai Sho**, built with Unity.
There is no backend, database, network layer, or automated test suite — the entire product
runs in-process in the Unity Editor / a built player.

- Engine version: `6000.0.47f1` (pinned in `ProjectSettings/ProjectVersion.txt`)
- Render pipeline: Universal Render Pipeline (URP)
- Language: C# under `Assets/Scripts/` (namespaces `PaiSho.*`)
- Main scene: `Assets/Scenes/GamePlay.unity`
- Package manager: Unity Package Manager (`Packages/manifest.json` + `Packages/packages-lock.json`)

## Cursor Cloud specific instructions

The Unity Editor is the only "service". It must be **licensed** before it will do anything
(import, compile, build, or Play Mode). Everything below assumes the environment snapshot /
update script has already installed the editor and system libraries.

### Where things live (installed outside the repo, persisted via the VM snapshot)
- Unity Editor binary: `/opt/unity/editor/Editor/Unity` (version `6000.0.47f1`)
- Helper scripts: `/opt/unity/bin/unity-activate.sh`, `/opt/unity/bin/unity-run.sh`
- Manual activation file (`.alf`): `/opt/unity/Unity_v6000.0.47f1.alf`

### Licensing (REQUIRED first step, and the main gotcha)
Unity has **no anonymous license**; a Unity account or license file is mandatory. Without it
every headless run ends with `No valid Unity Editor license found`. Provide credentials as
secrets, then activate. Credentials are consumed only during activation; the resulting license
is cached under `~/.local/share/unity3d/` and `~/.config/unity3d/`.

- Credential activation (Personal — omit serial; Pro/Plus — set `UNITY_SERIAL`):
  `UNITY_EMAIL=... UNITY_PASSWORD=... /opt/unity/bin/unity-activate.sh`
- Offline/manual activation: upload `/opt/unity/Unity_v6000.0.47f1.alf` at
  `https://license.unity3d.com/manual` to obtain a `.ulf`, then
  `UNITY_ULF_PATH=/path/to.ulf /opt/unity/bin/unity-activate.sh`

### Run / build / "lint" (all headless via Xvfb)
There is no separate lint step; the compiler is the lint. Headless GL uses Mesa `llvmpipe`
(set `LIBGL_ALWAYS_SOFTWARE=1`, already set by the helper).

- Import + compile scripts (fastest correctness check):
  `/opt/unity/bin/unity-run.sh import`
- Interactive/headless editor (virtual display): `/opt/unity/bin/unity-run.sh editor`
- Raw editor invocation: `/opt/unity/bin/unity-run.sh raw -batchmode -quit -projectPath /workspace ...`

Notes / gotchas:
- Always wrap editor invocations in `xvfb-run` (the helpers do this). `-nographics` is fine for
  import/build; drop it and use a real virtual display to render Play Mode / the game window.
- There is **no `BuildScript`/`-executeMethod` build entry point in the repo**. To produce a
  standalone player you must add a static `BuildPipeline.BuildPlayer(...)` editor method first,
  or build interactively. Play Mode via the editor is the simplest way to run the game.
- `Assets/Scripts.zip` and `minimal-kiwi-2.0.1.vsix` in the repo root are stray artifacts,
  unrelated to running the game.
