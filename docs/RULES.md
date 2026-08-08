# Pai Sho — Garden Rules (House Reference)

**Edition:** Working draft for this project  
**Sources mixed:** IPSA *Basic Pai Sho* + *Ancient Pai Sho* (1st Edition), plus house systems built into the Unity game  
**Not used:** Skud Pai Sho (Arrange/Plant, Accent discard, Clash-forbidden board, Orchid trapping, etc.)

This document is the rules memory for design and play. When code and this doc disagree, update one of them deliberately.

---

## 1. What this game is

Two players cultivate a garden on a circular Pai Sho board. You place and move tiles to form **Harmonies**, capture through **Clash**, and race toward a **Harmony Ring** around the Mid Port — while managing wilt, seasons, momentum, and special tiles.

**Lineage of this ruleset**

| Taken from Basic | Taken from Ancient | House expansions |
|---|---|---|
| Standard board, ports, gardens | Deeper special-tile flavor; harmony/disharmony as living board state | Seasons, wilt, momentum, echo ghosts, hold release |
| Flower moves & harmony wheel | Ceremonial “garden life” feel | Lotus bloom by capture balance |
| Capture by landing on a clash | — | Dragon Orchid as free aggressor |
| Spring opening flowers | — | Shared 6 spring placements, then 7-tile hands |
| Specials unlock after early play | — | Hold pile + Lotus/Orchid after 3 play turns each |

---

## 2. Materials

### Board

- Circular Pai Sho board with a diamond of legal intersections (implemented as a 19×19 point grid with illegal corners cut away).
- **Ports (gates):** Home, Foreign, East, West, and **Mid** (center).
- **White Gardens** and **Red Gardens** (light / dark interiors).
- **Neutral** paths and mixed borders.
- Tiles sit on **intersections**, not in square cells.

**Orientation**

| Player | Home Port | Foreign Port | Own side |
|---|---|---|---|
| Host | South | North | Southern half (not mid-row) |
| Opponent | North | South | Northern half (not mid-row) |

East / West ports are shared entry points. The mid-row is neither side.

### Reserves

Each player has **54 tiles**:

| Group | Tiles | Count each |
|---|---|---|
| White flowers | Jasmine, Lily, Jade | 6 |
| Red flowers | Rose, Chrysanthemum, Rhododendron | 6 |
| Accents | Knotweed, Wheel, Rock, Boat | 3 |
| Specials | White Lotus, Dragon Orchid | 3 |

After spring, each player holds a **hand of 7**. Remaining tiles stay in reserve / hold as below.

---

## 3. Game flow

### Phases

1. **Spring** — opening placement only  
2. **Play** — normal turns  
3. **End** — ring or resignation resolved; scores tallied  

### Spring (opening)

1. Players alternate. Host starts.
2. Each turn: draw from reserve until a **basic flower** appears (non-flowers go to that player’s **hold**).
3. Place that flower on **your side** of the board:
   - Not on a port
   - White flowers may not finish in red-only gardens
   - Red flowers may not finish in white-only gardens
4. After **6** spring placements total (typically 3 each), spring ends.
5. Deal **7-tile hands**. Hold tiles remain locked until hold release.

**Spring flowers do not age or wilt during spring.** Their spring-bud glow lasts until that flower’s first move in Play, or until **3 completed Play turns** have passed — whichever comes first.

### A Play turn

On your turn, take **one** main action:

- **Place** a tile from hand, **or**
- **Move** a tile already on the board, **or**
- Use certain accent actions (Wheel rotate, Boat unload), **or**
- Spend **momentum** (extra move / revive / freeze)

You cannot place and move as one turn (unlike Ancient’s place+move).  
You must act before ending the turn (pass exists when stuck).

**Free moves:** 1 per turn. Further moves cost momentum.

---

## 4. Placing tiles (Play)

| Tile | Where it may be planted |
|---|---|
| **Jasmine** | Home Port only |
| **Rose** | Foreign Port only |
| **Lily / Chrysanthemum** | East or West Port |
| **Jade / Rhododendron** | Mid Port |
| **Knotweed** | Any empty point ≥1 from every port |
| **Wheel** | Neutral garden only |
| **Boat** | Red or White garden (not ports) |
| **Rock** | Any empty legal point |
| **Lotus** | Own side, not ports — **after hold release** |
| **Orchid** | Opponent’s side, not ports — **after hold release** |

You cannot place onto an occupied point (even if you could capture by moving there).

### Hold release

After a player finishes **3 of their own Play turns**:

- Their hold pile returns to reserve
- They may plant **Lotus** and **Dragon Orchid**

---

## 5. Moving tiles

### General

- Move orthogonally unless the tile says otherwise (Lily / Chrysanthemum use an L).
- You may not land on a **port**.
- You may not leap over tiles unless the piece allows it (Lotus, Orchid, Wheel).
- White flowers may not **end** in red-only gardens; red flowers may not end in white-only gardens. Mixed / neutral landings are fine.
- You may not end a move so that one of **your** flowers shares a clear cardinal line with another of **your** clash-partners (**friendly disharmony**).

### Basic flowers

| Tile | Move | Garden landing |
|---|---|---|
| Jasmine | Up to **3** orthogonal ( **4** during Spring *season* ) | Avoid red-only |
| Rose | Up to **3** orthogonal | Avoid white-only |
| Lily | **L: 2 then 2** (cardinal, then perpendicular) | Avoid red-only |
| Chrysanthemum | **L: 2 then 2** | Avoid white-only |
| Jade | Up to **5** orthogonal ( **6** during Spring *season* ) | Avoid red-only |
| Rhododendron | Up to **5** orthogonal | Avoid white-only |

### Accents

| Tile | Move / ability |
|---|---|
| **Rock** | Immovable. Blocks / sits as earth anchor. |
| **Knotweed** | Immovable. Adjacent tiles cannot form harmony. Drains enemy tiles that are currently in harmony (feeds Echo). |
| **Wheel** | Unlimited orthogonal slide; may jump. **Rotate action:** turn adjacent tiles one step around it (Rock / Knotweed / immovables skip). Counts as your move. |
| **Boat** | Unlimited orthogonal; may **push** a movable tile up to **2** along the ray onto an empty point. **Ferry:** load one adjacent friendly flower (free); **unload** adjacent (ends turn). Unload must respect flower garden rules. |

### Specials

| Tile | Move / ability |
|---|---|---|
| **White Lotus** | Up to **3** orthogonal; may jump. Does **not** capture by clash. While **Blooming**, forms harmony with any basic flower of its owner. |
| **Dragon Orchid** | Up to **3** orthogonal; may jump. Forms **no** harmonies. May **capture any** opposing tile it can land on. |

> **Naming:** In Basic IPSA this special is “White Dragon.” In this game it is **Dragon Orchid**. Same role: yin aggressor that does not garden-harmonize.

---

## 6. Harmonies & clashes

### Harmony

Two of **your** flowers form a Harmony when:

1. They are a **harmonic pair** (see wheel below), and  
2. They share a clear **row or column** with **no tile between**, and  
3. Neither sits on a port, and  
4. Neither is adjacent to Knotweed, and  
5. Both are allowed to contribute (see Awakening)

Each harmony is worth scoring attention; a closed ring wins.

**Natural Harmony (Basic flavor):** a red flower in a red garden aligned with a white flower in a white garden is especially strong (prefer +2 when scoring that pair).

### Awakening

Newly planted flowers do **not** contribute to harmony until they have moved at least once — **unless** global awakening has unlocked (after **6** completed Play turns total). Spring placements also start dormant for harmony.

### Clash (disharmony) pairs

Used for **capture** and **friendly landing bans**.

| Tile | Harmonizes with | Clashes with |
|---|---|---|
| Jasmine | Lily, Rhododendron, Lotus | Rose, Orchid |
| Rose | Jade, Chrysanthemum, Lotus | Jasmine, Orchid |
| Lily | Jasmine, Jade, Lotus | Chrysanthemum, Orchid |
| Jade | Lily, Rose, Lotus | Rhododendron, Orchid |
| Chrysanthemum | Rose, Rhododendron, Lotus | Lily, Orchid |
| Rhododendron | Chrysanthemum, Jasmine, Lotus | Jade, Orchid |
| Lotus | All six basics | — |
| Orchid | — | — (does not form pairs; captures freely) |

---

## 7. Capturing

1. Capture happens **only by landing on** an enemy tile. Adjacent clash does **not** capture.  
2. The target must be capturable (not on a port; seasonal immunities may apply).  
3. For basic flowers: you may capture only if that enemy is in your **clash** list.  
4. Captured tiles go to **the Pot** (owned by the capturer for scoring / bloom / echo).

### Special capture rules

| Attacker / situation | Rule |
|---|---|
| **Dragon Orchid** | May capture **any** opposing tile it can legally land on |
| **Lotus** | Does **not** capture by clash |
| **Summer season** | Boat and Knotweed cannot be captured |
| **Autumn season** | Rose, Chrysanthemum, Rhododendron cannot be clash-captured |
| Accents | Generally do not clash-capture; Boat pushes instead |

### Lotus Blooming

A Lotus is **Blooming** when its owner has **more captures in the Pot** than the opponent (handicap for the player who has lost more material).

While blooming:

- It may harmonize with any of that player’s basic flowers  
- It scores a bloom bonus  

---

## 8. Victory

The game ends when:

1. **Harmony Ring** — a player forms a closed chain of at least **4** of their own harmonizing flowers whose cycle **encloses the Mid Port** → that player wins immediately, or  
2. A player **forfeits**, or  
3. (Optional timed / points goal — not required for the digital default)

If play continues to a scored ending without a ring, tally live score (harmonies, seasons, wilt recovery, pot, lotus protection flavor).

### Live score (digital default)

Per owned non-ghost tile:

- Base point value (wilt can reduce to 0 or −1)  
- +2 if blooming Lotus  
- +1 if currently in-season  
- Recovery bonuses when wilt improves  
- Chain bonus if many tiles are in harmony  

Plus seasonal award bonuses and pot-related bloom logic above.

---

## 9. House systems (garden life)

These are intentional expansions — keep them.

### Seasons

Seasons rotate every **6 turns**, starting in **Spring**:

| Season | Favored tiles | Notable effect |
|---|---|---|
| Spring | Jasmine, Lily, Jade | Extra slide range for Jasmine / Jade |
| Summer | Boat, Knotweed | Those accents cannot be captured |
| Autumn | Rose, Chrysanthemum, Rhododendron | Those reds resist clash-capture |
| Winter | Rock, Wheel, Lotus | Score bump for those types |

Finishing a turn as the season changes can award small score / momentum bonuses for relevant play (placements, harmonies, revives, composed single moves).

### Wilt

Tiles that neither move nor stay in harmony age:

| Neglect turns | Wilt | Point value |
|---|---|---|
| 0–2 | Healthy | 1 |
| 3 | Wilted | 0 |
| 4+ | Fully wilted | −1 |

- No wilt aging during **Spring phase**  
- Momentum **Freeze** skips one aging tick  
- Momentum **Revive** clears wilt  

### Momentum

At turn start you may gain momentum for:

- Having an in-season piece on the board  
- Having a strong harmony presence (≥3 tiles in harmony)

Spend momentum to:

- Tap **Extra Move** (dock) to spend 1 Momentum and allow a **second move** this turn — buy it before your first move; turns still auto-end after your allowed moves (no separate End Turn)
- **Revive** a wilted tile  
- **Freeze** wilt on a tile for a beat  

Place, Wheel rotate, Revive, and Freeze still end the turn immediately (no Extra Move after those).

Enemy flowers adjacent to **Knotweed** are **drained**: they cannot move and do not keep harmony until they leave that adjacency (or the Knotweed is gone).

### Echo (ghost flowers)

Revival points accumulate from wilt recovery and Knotweed drains.  
Every **10** points: a **Ghost Echo** of one of your captured basic flowers returns near the board.

- **Human players** choose which eligible flower type to summon  
- **AI / headless** auto-picks (prefer a type already useful on the board, else first eligible)  
- Ghosts are worth more when they awaken  
- Ghosts do not harmonize or score until they have moved  
- HUD shows `Echo N/10` for each player  

**Player tools that match the computer:** dock **Revive** / **Freeze** (Momentum), Boat load/unload, Wheel **Rotate**. The AI can also choose Boat load/unload and Wheel rotate actions.

---

## 10. Glossary

| Term | Meaning |
|---|---|
| **Port / Gate** | Entry triangle / center point; tiles do not move onto ports |
| **Garden** | Colored board region controlling flower landings |
| **Spring Flowers** | The six opening flowers placed before Play |
| **Hold** | Non-flowers drawn during spring; unlock after 3 play turns |
| **Harmony** | Two allied harmonic flowers on a clear cardinal line |
| **Clash** | Pair that may capture by landing; also bans friendly alignment |
| **Pot** | Captured tiles |
| **Blooming** | Lotus state when you lead in captures |
| **Harmony Ring** | Closed harmony chain enclosing Mid — wins the game |
| **Wilt** | Neglect decay on idle / unharmonized tiles |
| **Momentum** | Spendable tempo / care resource |
| **Echo** | Ghost return of a captured basic flower |

---

## 11. Quick reference — numbers

| Constant | Value |
|---|---|
| Spring placements | 6 total |
| Hand size | 7 |
| Reserve | 54 per player |
| Hold / special unlock | After **3** of that player’s Play turns |
| Global harmony awakening | After **6** Play turns completed |
| Spring bud glow | Until first move **or** 3 Play turns |
| Season length | 6 turns |
| Harmony ring minimum | 4 tiles enclosing Mid |
| Boat push | Up to 2 |
| L-move | 2 + 2 |
| Jasmine / Rose | 3 (Jasmine 4 in Spring season) |
| Jade / Rhododendron | 5 (Jade 6 in Spring season) |
| Lotus / Orchid slide | 3, may jump |
| Echo threshold | 10 revival points |

---

## 12. Design notes / open decisions

Keep these flagged so we don’t silently drift:

1. **Orchid vs Basic “White Dragon”** — We use Dragon Orchid naming; free capture is intentional. Basic also allows any-tile capture for White Dragon; we match that spirit, not Skud’s “wild only with blooming Lotus” rule.  
2. **Lotus does not capture** — Matches our harmony-focused Lotus; do not “fix any.”  
3. **Shared spring of 6** — Not “3 each required”; total board placements.  
4. **Lily spring bonus** — Piece range helper exists; L-path currently stays 2+2 in the mover. Decide later whether Lily gets a spring length bump.  
5. **Ancient inverted gardens** — Helper exists in code but Play uses **Basic** garden orientation (white on white, red on red). Ancient’s “white starts on red” is **not** active unless we explicitly switch.  
6. **Rock on ports** — Placement currently does not hard-block ports; consider forbidding if it feels wrong.  
7. **This doc wins arguments** — When changing a constant in `PieceRules.cs` / managers, update this file in the same change.

---

## 13. Code anchors

| Topic | Primary files |
|---|---|
| Constants | `Assets/Scripts/Engine/Pieces/PieceRules.cs` |
| Harmony / clash lists | `Assets/Scripts/Engine/Pieces/PieceHarmonyProfiles.cs` |
| Legal moves / capture | `Assets/Scripts/Engine/Game/LegalMoveCalculator.cs` |
| Placement | `Assets/Scripts/Engine/Game/PlacementValidator.cs` |
| Board geometry | `Assets/Scripts/Engine/Board/BoardUtils.cs` |
| Phases / turns | `Assets/Scripts/Engine/Game/GameManager.cs`, `GameStateManager.cs` |
| Seasons | `Assets/Scripts/Engine/Game/SeasonManager.cs` |
| Wilt | `Assets/Scripts/Engine/Board/TileLifecycleManager.cs` |
| Momentum | `Assets/Scripts/Engine/Game/MomentumManager.cs` |
| Echo | `Assets/Scripts/Engine/Game/EchoTileManager.cs` |
| Ring victory | `Assets/Scripts/Engine/Game/HarmonyRingDetector.cs`, `GameEndManager.cs` |
| Scoring | `Assets/Scripts/Engine/Game/ScoringManager.cs` |

---

*Drawn from IPSA “Pai Sho: Official Rules & Gameplay” 1st Edition (Basic + Ancient chapters), Creative Commons BY 4.0, mixed with this project’s implemented garden systems. Skud Pai Sho deliberately omitted.*
