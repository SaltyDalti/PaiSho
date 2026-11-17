# Design

## Problem

A player needs one local match: a title, a spring opening, play, a choice of computer or hotseat, and an ending by harmony ring or forfeit. A reviewer needs the garden rules in a form that can be read and run without the Unity editor.

The match grew inside Unity managers. `GameBootstrap` builds them after `Assets/Scenes/GamePlay.unity` loads. `TitleMenu` is an overlay, not a second scene. `GameStateManager` moves the match through Spring, Play, and End. Placement, movement, harmony, capture, seasons, scoring, wilt, momentum, and the ring check live on those managers and on the board helpers they call. Those types take `UnityEngine`, `BoardManager.Instance`, pots, and the current scene.

`docs/RULES.md` is the house rules text for that match (IPSA Basic and Ancient flavor, plus seasons, wilt, momentum, and echo; Skud Pai Sho is left out). `docs/match-lifecycle-api.md` describes a later server that would validate moves with shared rules code. That API is a sketch. `Host` and `Opponent` are still local seats.

The rules that the scene actually applies are hard to test on a machine that only has `dotnet`. A pure layer, `Assets/PaiSho/Runtime/Domain`, was added so headless tests can compile the rules by themselves. The scene still mutates the match through the managers.

## Constraints

- The editor for this project is Unity `6000.0.47f1`. Rendering is URP (`com.unity.render-pipelines.universal`). The only playable scene is `Assets/Scenes/GamePlay.unity`.
- Product code stays under `Assets/PaiSho`. `docs/STRUCTURE.md` is the layout. `docs/RULES.md` section 13 still names older `Assets/Scripts/Engine` paths. The client is `Assets/PaiSho`.
- `Runtime/Domain` has no `UnityEngine` reference. `tests/PaiSho.Rules.Tests` is a `net8.0` xUnit project that compiles the Domain sources plus `PieceType.cs` by file include. `PaiSho.Runtime` itself references the engine, because the same assembly holds the MonoBehaviours.
- Domain movement and placement take an occupancy function. They do not call `BoardManager`.
- The live board and Domain do not share one integer encoding. `BoardUtils` uses `GridSize` 19: rows are `coordinate / 19`, the mid port constant is `180`, and cardinal steps are `-19, 19, -1, 1`. `BoardCoords` stores garden `x,z` in `[-9, 9]`, keeps points with Manhattan distance at most 12 from center, and encodes with stride 20: `(z + 9) * 20 + (x + 9)`, so center is `189`. `docs/match-lifecycle-api.md` says a future `coord` uses that stride-20 encoding. Changing either encoding means updating the callers and the tests that assume it.
- Managers stay singletons. New Netcode is out of scope until the match contract in `docs/match-lifecycle-api.md` is the one being implemented.
- `docs/SHIP.md` keeps loading on `Resources` keys and leaves Addressables and a separate boot scene for later. Scene, prefab, and `.meta` YAML stay put unless a change needs them.
- Unity Edit Mode tests require the editor. `Assets/PaiSho/Tests/EditMode` currently holds the test assembly and a `.gitkeep`.

## Approach

Leave the playable loop where it is. After the scene loads, `GameBootstrap` ensures the managers, waits on `TitleMenu.WaitUntilMatchRequested`, then applies `GameSession.AiEnabled`. The title buttons are New Game vs Computer and New Game Hotseat. Headless self-play skips the overlay when `HeadlessActionExecutor.IsActive`. Spring setup, turns, the computer player, ring resolution (`VictoryManager`, `HarmonyRingDetector`, `GameEndManager`), and forfeit (`GameManager.ForfeitMatch`) stay on that path. `docs/SHIP.md` treats title → new game → spring → play → ring or forfeit → play again as the slice that has to keep working.

Beside that loop, Domain is a set of static predicates over plain values:

- `Seat`, `MatchPhase`, and `GardenSeason` mirror the local player, the three match phases, and the four garden seasons.
- `BoardPiece` and `PieceStatus` are snapshots. Blooming, wilt, and harmony flags are passed in so Domain does not ask `PotManager` or the lifecycle manager.
- `StartingReserves` holds the opening counts: 6 for each type in `Flowers`, and 3 for each type in `Accents` (Boat, Rock, Knotweed, Wheel, Lotus, Orchid).
- `PlacementRules`, `MovementRules`, `HarmonyRules`, `CaptureRules`, `SeasonRules`, `ScoringRules`, and `VictoryRules` return allow/deny results or lists. Occupancy is a `Func<int, bool>` or a list of snapshots.
- `BoardUtils.ToCoordinate` and `FromCoordinate` forward to `BoardCoords`. Gates, gardens, the stride-19 legal-point table, and piece lookup stay on `BoardUtils` and `BoardManager`.

The headless project covers those Domain types: coordinates, placement, movement, harmony, capture, seasons, scoring, and victory. `.github/workflows/rules-tests.yml` runs `dotnet test` on that project. The comments on the rules types describe them as the shared rules for managers and tests. The call sites today are the tests plus `BoardUtils`.

## Trade-offs

The scene and Domain both speak garden Pai Sho, and they already decide different questions.

Play harmony (`HarmonyManager`, `PieceHarmonyProfiles`) follows `docs/RULES.md`: a harmonic pair of your own flowers on a clear row or column, blocked by gates, knotweed, and tiles that have not awakened. Domain harmony (`HarmonyRules`) is same-type flowers, or a blooming Lotus with any flower, at Chebyshev distance 1, including diagonals. The headless tests lock the Domain rule.

Play placement (`PlacementValidator`) checks side of the board, gardens, ports, accent sites, and hold release for Lotus and Orchid. Domain placement checks phase, the seat’s opening flower (Host Jasmine, Opponent Rose), an empty legal stride-20 point, and reserve in Play. Ports and gardens stay in `PlacementValidator`.

Play capture lands on a clash partner. Domain capture (`CaptureRules`) is orthogonal adjacency plus “types that do not harmonize,” with Summer immunity for Boat and Knotweed. `CanBeDisharmonized` documents Autumn resistance for Rose, Chrysanthemum, and Rhododendron and states that `CaptureManager` does not apply it.

Play movement is `LegalMoveCalculator`, which is what the match calls. Orchid and Lotus slide 3 and may pass over pieces. Lily and Chrysanthemum take an L of 2 then 2 (`LShapeLegSteps`). Wheel slides any distance on a ray and may jump. Domain `MovementRules` uses other tables: Orchid slides 6 and may jump, Lily and Chrysanthemum use a 2-by-1 L, Lotus slides 2 on orthogonal and diagonal rays, and Wheel slides any distance orthogonally with jumps disabled.

The ring check differs in the same way. Both use a minimum of 4. `HarmonyRingDetector` builds a graph with `HarmonyManager.IsHarmony` among that player’s pieces already marked in harmony, then keeps a cycle whose polygon contains `BoardUtils.MiddleGate` (`180`). `VictoryRules` walks orthogonal neighbors that pass `HarmonyRules.IsHarmony`, and treats the center as enclosed when the cycle contains every orthogonal neighbor of the Domain center (`189`).

Two coordinate spaces are the sharp edge. A stride-19 point and a stride-20 point are different integers for the same garden intersection (mid is `180` on the live board and `189` in Domain). Keeping both avoids a silent renumber of every gate, garden cell, and saved scene reference. It also means a Domain result cannot be dropped onto `BoardManager` without a conversion that does not exist yet.

`SeasonRules` and `ScoringRules` are the headless season calendar (rotate every 6 turns), in-season sets, wilt-aware piece scores, and seasonal turn bonuses. `SeasonManager` and `ScoringManager` still own the live match. Passing a Domain test does not by itself show that the scene agrees with `docs/RULES.md`.

That split is what makes `dotnet test` possible on a machine without the editor, and what leaves two rulebooks to edit. Singletons and `Resources` loaders have the matching cost: the slice keeps its current boot path, and a server or a second scene cannot yet treat Domain as the match.

## Result

The repository is a local Unity 6 URP match with the title → spring → play → AI or hotseat → ring or forfeit loop described in `docs/SHIP.md`, and a separate pure rules library under `Assets/PaiSho/Runtime/Domain`.

Headless coverage is the xUnit project `tests/PaiSho.Rules.Tests`, which compiles Domain and `PieceType` and is what CI runs. The editor test assembly is present and has no fixtures yet. Online play is the sketch in `docs/match-lifecycle-api.md`. The previous scripts tip remains on `archive/domain-bd58660`.

`Runtime/Domain` is inside the product assembly and is free of `UnityEngine`. The match the player sees is still applied by the Game and Board managers. `BoardUtils` coordinate conversion is the live call into Domain.

## What I'd do next

Bring the scene and Domain onto one set of predicates, one behavior at a time, with a headless test for each behavior that `docs/RULES.md` already states: the harmony wheel and clear cardinal lines, placement by port and garden and hold release, capture by landing, and a ring of at least 4 that encloses the mid port. Point the managers at those predicates when the predicate matches the scene, so a rules change has one home.

Keep stride 20 as the Domain and future API encoding, and convert at the board boundary. Leave the live `180` / stride-19 table in place until that conversion has callers and tests. Update the code anchors in `docs/RULES.md` when that file is next edited, so they name `Assets/PaiSho/Runtime` instead of `Assets/Scripts/Engine`.

Follow the order already written in `docs/match-lifecycle-api.md`: Domain validates `place` and `move`, a small API persists matches, the Unity client applies snapshots, and realtime transport waits until the async path works. Pass-and-play can stay local. Leave Addressables and a dedicated boot scene until `docs/SHIP.md` is the checklist being protected, not a migration in progress.
