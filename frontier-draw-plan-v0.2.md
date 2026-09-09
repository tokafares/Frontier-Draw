# Frontier Draw — Game Design & Technical Plan (Draft v0.2)

**Reference game analyzed:** *West Gunfighter* (Candy Mobile / Doodle Mobile — Android/iOS/Windows, 100M+ downloads, 4.3★, offline single-player, contains ads + IAP)
**Status:** Planning only. No code has been written yet.
**Name:** Confirmed as **Frontier Draw**.

---

## 0. Decisions locked in so far

*West Gunfighter* is a **single-player, offline, open-world sandbox** — free-roam exploration, story/side missions, mini-games (blackjack, darts, horse racing), an economy, and NPC duels are just *one* of many activities.

Frontier Draw is a **new, standalone project** — not a reskin of the earlier Wild Noon proposal. Wild Noon's design work (proposal doc, character roster, economy structure, backend spec) is a *reference bank* we borrow ideas from where they genuinely fit — it does not define this plan by default. West Gunfighter itself was chosen only as a concrete, playable reference for the *duel mechanic's feel* — not a spec to clone.

**[ASSUMPTION — still the operating default]:**

> Frontier Draw is a **real-time 1v1 online PvP ranked duel game**. We take from West Gunfighter only the duel *feel* — lock-on targeting readability (red cross / green target), circle-strafe positioning, the "wait for the signal, don't jump the draw" tension — and the general Western art direction. We do not build its open-world/missions/mini-games/exploration structure.

If this should instead become a single-player open-world adventure, say so — that's a fundamentally bigger, slower project (12+ months solo vs. a few months for a focused PvP duel MVP), and the whole plan below would need to be re-scoped.

**Design principle — keep the duel module portable.** This might later become part of a collaboration or get folded into someone else's game. That means the duel system (`DuelController`, `PlayerAimController`, `LockOnTarget`, `DrawSignalTimer`) should stay a self-contained module with minimal outside dependencies — it shouldn't reach into Frontier Draw–specific currency, rank, or menu code to function. Build it so it could be dropped into a different project's scene and still run with just a player reference and a "duel started" event. This costs almost nothing now (it's how we'd build it anyway for M1's local prototype) but keeps the door open later. Follow-on effect: don't over-invest art/UI effort on Frontier Draw–specific branding (menus, currency icons) during the MVP — put the polish budget into the duel *feel* itself, since that's the part that's valuable either way this goes.

**Networking approach — DECIDED at M2 (2026-09-06).** The old Wild Noon proposal's stack (custom Node.js/WebSocket server + Redis matchmaking) was scoped for a funded 6–8 week team project and is a serious multi-month undertaking to build solo — ruled out for now.

> **DECISION: Unity NGO (Netcode for GameObjects) + Unity Relay**, not Photon Fusion, not a custom Node.js/WebSocket server.
>
> **Why:** M2's actual goal right now is just proving two real devices can duel over the network — not perfect frame-accurate fairness yet. NGO is first-party, fits our existing Unity 6.3 setup, and has a free tier. We're also learning Unity/C# at the same time, so picking a second, unrelated networking SDK right now would mean learning two hard things at once.
>
> **Known limitation, deliberately deferred:** Photon Fusion has built-in client-side prediction and lag compensation specifically designed for reaction-timing-sensitive games like ours (where the whole mechanic is "who drew first"). NGO does not give us that out of the box — if we want draw-timing to be fair across different latencies, we'll need to build our own prediction/reconciliation on top of NGO later, or migrate that specific part to Fusion.
>
> **When to revisit:** Once we're playtesting with real people (around M6, or whenever real online tests start) — if a player reports losing a duel that felt like they drew first, that's the signal this needs real prediction/lag-compensation work. Don't preemptively build this now; it's a known limitation, not a blocker, for M2.

---

## PHASE 1 — Reference Analysis: West Gunfighter

### What it is
A 3rd-person 3D open-world Western action-adventure. Free-roam a desert/town/canyon/forest/cemetery map on foot or horseback, take on main and side missions (bounty hunts, rescues, deliveries, animal/zombie hunts), duel NPCs, gamble in mini-games, and spend earned currency on gear.

### Core gameplay loop
Explore → accept mission/encounter → combat or duel → earn gold/diamonds → spend on weapons/outfits/horses → repeat, with occasional detours into mini-games (blackjack, darts, horse racing).

### The duel mechanic specifically (most relevant to us)
1. Player and opponent face off at a fixed distance.
2. Both circle-strafe — moving opposite to the opponent keeps you in their blind spot and them in your effective range.
3. A **lock-on reticle** shows **red cross = out of range/can't hit**, **green target = in range/can hit**.
4. Player must wait for the game's draw signal — tapping/shooting early is a hard fail.
5. On signal, fastest accurate shot wins.
6. **Known weakness (from player reviews):** the lock-on can't be disengaged manually and always targets center-mass, and the game gives **no tutorial** for the duel specifically — players report being confused the first time. Both are worth explicitly avoiding in our version — the draw sequence must be taught to the player, and if we keep a lock-on assist we make it a deliberate, tunable choice, not a forced auto-aim.

### Feature breakdown (grouped, with relevance to us)

| Category | What West Gunfighter does | Relevant to Frontier Draw MVP? | Difficulty |
|---|---|---|---|
| Movement/Camera | Free-roam 3rd-person, horse mount, standard touch joystick + camera drag | No — we don't need open-world traversal | — |
| Duel mechanic | Circle-strafe, lock-on (red/green), wait-for-signal, tap-to-shoot | **Yes — this is our core loop** | High |
| Combat outside duels | Free-aim shooting at roaming enemies/animals | No | — |
| Missions | Main story + side quests, fetch/hunt/rescue | No (not in MVP; maybe later as PvE training scenarios) | — |
| Progression | Mission-based XP, RPG-lite stat upgrades | Partial — borrowing the idea of weapon/skill upgrades + rank tiers from Wild Noon's earlier spec | Medium |
| Economy | Dual currency (gold earned, diamonds paid), store, ads for currency | Partial — borrowing the gold/paid-currency, no-pay-to-win idea from Wild Noon's earlier spec | Medium |
| Customization | 8 outfit sets, layered clothing per body part | Partial — borrowing the PNG layer system idea from Wild Noon's earlier spec | Medium |
| Mini-games | Blackjack, darts, horse racing | No — out of scope, pure distraction from the core PvP loop | — |
| Weapons | 20+ guns/rifles/shotguns + knife, varying stats | Partial — a smaller curated arsenal, not 20+ | Low–Medium |
| UI/HUD | Lock-on reticle, mission log, currency counters, minimap | Partial — we need reticle + rank/currency HUD, not minimap/mission log | Medium |
| Multiplayer | **None** — fully offline, single-player | **This is the biggest gap** — Frontier Draw's entire value prop (real-time PvP netcode, matchmaking, anti-cheat) has zero equivalent in the reference and must be designed largely from scratch, with ideas adapted from Wild Noon's earlier spec | Very High |
| Monetization | Ads (AdMob/Unity Ads) + IAP for currency/cosmetics | Partial — borrowing the approach from Wild Noon's earlier spec | Medium |

**[ASSUMPTION]** — Anything about server code, netcode tick rate, or anti-cheat cannot be learned from West Gunfighter (it's offline). Where useful, I'm drawing on ideas from the earlier Wild Noon proposal doc (Node.js/WebSocket server-authoritative netcode, Redis matchmaking) rather than inventing new ones from zero — but see the open networking question in Section 0.

---

## PHASE 2 — Game Definition (our version)

| Item | Decision |
|---|---|
| Working title | **Frontier Draw** |
| Genre | Real-time 1v1 PvP Western duel, competitive ladder |
| Platform | Mobile (iOS + Android) |
| Camera | Third-person, fixed/near-fixed duel camera (not free-roam) |
| Art direction | Stylized Western, arcade-y not gritty |
| Core loop | Queue → matchmake → duel (circle-strafe, lock-on readability, wait-for-signal, draw) → result → rank change → currency reward → menu |
| Session length | 1–3 minutes per duel, several duels per sitting |
| Win condition | Land the fatal hit first without a false start |
| Lose condition | Draw early (false start), get hit first, or time out |

---

## PHASE 3 — Scope: MVP / V1 / Future

**Reality check:** a fully-loaded version of this idea (multiple characters, voice chat, seasons, admin panel, anti-cheat, kill-clip sharing — per the scale of the old Wild Noon proposal) is an 8-week, multi-thousand-dollar, small-team scope. As a solo personal project, the MVP needs to be far smaller — one playable loop, proven fun, before any of the rest.

| Priority | Meaning | Examples |
|---|---|---|
| **P0** | Can't ship without it | Draw-to-shoot core mechanic, 1 character (no customization yet), 1 arena, local 2-player test mode, basic win/lose |
| **P1** | Needed for a real online MVP | Server-authoritative netcode, basic matchmaking (no rank tiers yet), 1 currency, simple weapon stat variation |
| **P2** | Nice to have, post-MVP | Ranked ladder/seasons, cosmetic customization, 2nd–3rd character, leaderboards |
| **P3** | Future | Voice chat, kill-clip sharing, admin panel, full character roster, power-ups, multiple arenas |

### MVP definition
A single online 1v1 duel: two real players matched, circle-strafe + lock-on + draw-signal + shoot, one of them wins, both return to a menu. No missions, no economy yet, no customization yet. **This is the smallest version that proves the core mechanic is fun and the netcode works** — everything else builds on top of it.

### What NOT to build initially
- Any open-world/exploration elements from West Gunfighter (missions, mini-games, free-roam)
- Voice chat, social/friends system
- Admin web panel
- More than 1 arena, more than 1 character
- Seasonal rank system (start with a simple win/loss record)
- Ads/IAP integration (add once the core loop is validated with real players)

---

## PHASE 4 — Unity Project Architecture

| Item | Recommendation | Why |
|---|---|---|
| Unity version | **Unity 6.3 LTS** (currently the latest LTS, supported to Dec 2027) | Long-term support, stable for a multi-month solo project |
| Render Pipeline | **URP** (Universal Render Pipeline) | Mobile-first, good performance headroom on mid-tier phones |
| Input System | New **Input System package** | Needed for touch + eventual gyro/accel input cleanly, event-driven, easier to test with a keyboard/mouse "local test" mode before touch is polished |
| Networking | **TBD — open question**, see Section 0 | Custom server-authoritative vs. an existing multiplayer service; decided at M2, not before |
| Scene architecture | Additive scenes: `Bootstrap` → `MainMenu` → `Duel` (loaded/unloaded per match) | Keeps persistent systems (network, audio, save) alive while duel scenes are cheap to reload |
| Save system | Local JSON (PlayerPrefs for small flags, JSON file for profile/loadout) → later synced to backend | Simple offline-first, syncs once accounts exist |
| Game state | Central `GameStateManager` (menu / matchmaking / in-duel / result) as a plain C# class, not a god MonoBehaviour | Keeps state transitions predictable without becoming a dumping ground |

### Folder structure

```
Assets/
    _Project/
        Scenes/
            Bootstrap.unity
            MainMenu.unity
            Duel.unity
        Scripts/
            Core/              # GameStateManager, ServiceLocator, bootstrap
            Duel/               # DuelController, DrawSignal, LockOnTarget, PositioningRules
            Networking/         # NetClient, MatchmakingClient, StatePrediction
            Player/             # PlayerInputHandler, PlayerAimController
            UI/                 # per-panel controllers
            Data/               # ScriptableObject definitions (WeaponData, CharacterData)
            Audio/              # AudioManager
        Prefabs/
            Characters/
            Weapons/
            VFX/
            UI/
        Art/
            Characters/
            Environments/
            Weapons/
        Audio/
            SFX/
            Music/
        UI/
            Sprites/
            Fonts/
        ScriptableObjects/
            WeaponData/
            CharacterData/
        Animations/
    Plugins/                    # third-party SDKs only (kept isolated)
```

`_Project` is prefixed with an underscore so it always sorts to the top of the Assets folder — a small habit that saves time once the project grows. `Plugins` stays isolated so an SDK update never touches project code.

---

## PHASE 5 — Asset Checklist (MVP scope only)

| Asset | Qty | 2D/3D | MVP? | Notes |
|---|---|---|---|---|
| Player character model + rig | 1 | 3D | MVP | Reuse for both duelists at first (recolor/mirror) |
| Draw/aim/fire/hit-react/death animations | 1 set (~6 clips) | 3D anim | MVP | The core feel of the game lives here — worth the most polish time per hour spent |
| Duel arena environment | 1 | 3D | MVP | Small, simple — a street or clearing, not an open world |
| Pistol model | 1 | 3D | MVP | |
| Muzzle flash / hit spark VFX | 2 | VFX | MVP | |
| Lock-on reticle (red/green states) | 1 | UI/2D | MVP | Directly informed by West Gunfighter's readability |
| "DRAW!" signal UI/animation | 1 | UI | MVP | Needs to be unmistakable — false starts should feel like *player* error, not UI confusion |
| HUD: win/lose banner | 1 | UI | MVP | |
| Menu buttons/panels art | 1 set | UI | MVP | |
| Gunshot / draw / footstep SFX | ~5 | Audio | MVP | |
| Ambient Western music loop | 1 | Audio | P1 | |
| Additional characters (2nd, 3rd) | 2 | 3D | P2 | |
| Outfit layers (hat/jacket/pants) | — | 2D/3D | P2 | Layered customization, idea borrowed from Wild Noon's earlier spec |
| Additional arenas | 2–3 | 3D | P2 | |
| Weapon variety (rifles etc.) | 5+ | 3D | P3 | |

---

## PHASE 6 — UI / Panels (MVP scope)

| Panel | Purpose | Key elements | Pauses game? | Persists across scenes? |
|---|---|---|---|---|
| MainMenu | Entry point, play button, basic profile | Play button, win/loss record, settings icon | N/A | No |
| MatchmakingPanel | Show searching state | Spinner, cancel button, estimated wait | N/A | No |
| DuelHUD | In-duel readout | Lock-on reticle, "DRAW!" prompt, health/hit indicator | No | No |
| ResultPanel | Post-duel outcome | Win/Lose text, rematch/menu buttons | Yes (overlay) | No |
| SettingsPanel | Audio/controls toggles | Sliders, sensitivity | Yes (overlay) | Yes (persistent canvas) |

**UI flow:** `MainMenu → MatchmakingPanel → (Duel scene loads) → DuelHUD → ResultPanel → back to MainMenu`

---

## PHASE 8 — Script Architecture (MVP)

| Script | Type | Responsibility | Depends on |
|---|---|---|---|
| `GameStateManager` | Plain C# (singleton via ServiceLocator, not a MonoBehaviour god-object) | Tracks current app state, fires state-change events | — |
| `NetClient` | MonoBehaviour | Network connection to backend, sends/receives duel state | `GameStateManager` |
| `MatchmakingClient` | Plain C# | Requests a match, listens for match-found event | `NetClient` |
| `DuelController` | MonoBehaviour (on a `DuelManager` GameObject) | Owns duel state machine: positioning → signal → draw window → resolution | `NetClient`, `PlayerInputHandler` |
| `DrawSignalTimer` | Plain C# | Randomized delay before the "DRAW!" signal fires, prevents pattern-reading | `DuelController` |
| `PlayerInputHandler` | MonoBehaviour | Reads touch/tap input, forwards draw/shoot events | Input System |
| `LockOnTarget` | MonoBehaviour | Computes in-range/out-of-range state, drives reticle color | `DuelController` |
| `PlayerAimController` | MonoBehaviour | Moves/aims player model based on circle-strafe input | `PlayerInputHandler` |
| `WeaponData` | ScriptableObject | Fire rate, accuracy stat, reload — data only | — |
| `CharacterData` | ScriptableObject | Character stats/visual reference — data only | — |
| `AudioManager` | MonoBehaviour (persistent) | Plays SFX/music on event | — |
| `UIManager` | MonoBehaviour (persistent) | Shows/hides panels based on `GameStateManager` events | `GameStateManager` |

### Dependency diagram (text)
```
GameStateManager
    |
NetClient -> MatchmakingClient
    |
DuelController -> DrawSignalTimer
    |              |
PlayerInputHandler  LockOnTarget
    |
PlayerAimController
```
`UIManager` and `AudioManager` listen to `GameStateManager` events independently — they don't sit in the main dependency chain, which is exactly why they're separate persistent scripts instead of being wired into `DuelController` directly.

---

## PHASE 9 — Scenes

| Scene | Purpose | DontDestroyOnLoad? |
|---|---|---|
| `Bootstrap` | Initializes `GameStateManager`, `NetClient`, `AudioManager`, `UIManager`, then loads `MainMenu` | These 4 managers persist — everything else reloads freely |
| `MainMenu` | Menu, matchmaking entry | No |
| `Duel` | The actual duel — loaded fresh per match, unloaded after | No — deliberately not persistent, so a new duel always starts clean with no leftover state |

---

## PHASE 10 — Prefabs (MVP)

```
Player.prefab
├── Model + Animator
├── CharacterController (or Rigidbody, TBD once movement feel is prototyped)
├── PlayerAimController
├── PlayerInputHandler (local player only)
├── LockOnTarget
└── AudioSource (footsteps, gunshot)

DuelManager.prefab
├── DuelController
├── DrawSignalTimer
└── (references both Player instances at runtime)

DrawSignalUI.prefab
├── Canvas (world-space or screen-space overlay)
└── Animator (flash/pulse on signal)
```

---

## PHASE 12 — Development Roadmap (Milestones)

| Milestone | Goal | Definition of Done |
|---|---|---|
| **M0 — Project Setup** | Empty Unity project, folder structure, Git repo, URP configured | Project opens clean, folders match Phase 4 |
| **M1 — Local Draw Prototype** | Prove the core mechanic is fun with **two local players on one device/keyboard**, no networking yet | Two "characters" can circle-strafe, see red/green lock-on, wait for signal, tap to win/lose — all offline |
| **M2 — Networking Layer** | Same duel, but over the network between two real devices | Two phones/emulators can duel each other through the backend — using **Unity NGO + Relay** (decided per Section 0); no prediction/lag-compensation yet, that's a deferred known limitation. **Raw connection confirmed working 2026-09-06** (Editor client ↔ standalone host, verified via `OnClientConnectedCallback` + live role/connected-clients readout on both sides). **Scope extended (2026-09-06):** M2 now also covers syncing actual duel gameplay — `DuelController` and the players become `NetworkObject`s, with position, the draw signal, and hit resolution synced over the network — not just a raw connection test. |
| ~~**M3 — Matchmaking**~~ | ~~Players queue and get auto-matched~~ | **SKIPPED for now (decided 2026-09-06).** Not needed for the collaborator demo — manual two-instance join-code connection (from M2) is enough. Revisit if/when real matchmaking is actually needed. |
| **M4 — Polish Pass 1** | Animations, VFX, SFX, "DRAW!" signal feel | Duel feels satisfying, not placeholder-gray |
| **M5 — Currency & Basic Menu** | Win/loss record, simple currency reward | Persistent profile across sessions |
| **M6 — MVP Playtest** | Real players test the MVP | Feedback collected before investing in P1/P2 features |

*(Milestones beyond M6 — ranked seasons, customization, monetization, additional characters/arenas — get scoped in detail once the MVP is validated: don't build the rest until we know the core is fun.)*

---

## What We Build First

**Milestone 0, then the very first script of Milestone 1: local draw-signal prototype — no networking, no art, just two boxes/capsules that can circle-strafe and react to a random-timed "DRAW!" signal.** This is the single riskiest assumption in the whole plan (is the duel *feel* actually fun?) and the cheapest thing to test. Everything else — netcode, art, economy — is wasted effort if this core 5-second loop isn't satisfying.

Next: M0 step by step — exact Unity project settings, exact folder creation, first script file path and filename, complete code, where to attach it, what to enter in the Inspector, and how to test it — one step, waiting for confirmation before the next.
