# Bugbot review policy — PaiSho

## Must flag

- Rules/movement/placement/win-condition changes in `Assets/Scripts/Engine/` without matching updates under `tests/PaiSho.Rules.Tests/` (or Domain extraction).
- New `UnityEngine` usage inside `Assets/Scripts/Engine/Domain/`.
- Hardcoded secrets, tokens, or credentials.
- Accidental mass changes to `.meta`, `.unity`, `.prefab`, or `.asset` files unrelated to the PR goal.
- “Fixes” that change `BoardCoords.CoordStride` / encoding without migrating all callers and tests.

## Nice to flag

- Duplicate capture/placement managers that diverge in behavior.
- Networking or persistence added without following `docs/match-lifecycle-api.md`.
- Public API renames that break Inspector-serialized references.
