# Salvage Ops: derelict ships, salvage yards, recovery

An integration branch `feature/salvage-ops/base` cut from `jupiter-1.60/fixes_49`, with sub-branches under it. One
PR to `main` once the whole feature is complete (140, 154). Status (2026-10-15): planning done, nothing coded.
Decisions 119-176 are all settled; 168-175 came from the review of this plan.

## The rules (Gilad, 2026-10-15)

### Which deaths make a derelict

- **Who:** military roles only, frigate and above (124, 172). Instant kills can become derelicts too (130). In
  `RoleName` carrier, bomber, support, troopShip and station sort below frigate, so the rule takes both roles:
  - the hull role is frigate, destroyer, cruiser, battleship or capital;
  - the design role is military: a warship, carrier, bomber, support or troop ship. Freighters, colony ships,
    scouts and constructors on those hulls do not count.
- **Stations:** an untethered station-role design (131, 172). Shipyards, subspace projectors and platforms never
  become derelicts (131). Research stations, mining stations, Dyson swarm controllers, salvage yards and Remnant
  portals never become derelicts; they die normally, with the main explosion and blast (148, 156, 172). Pirate bases
  count when they are untethered (172).
- **When it is decided:** at death, by the roll alone (170). A ship that rolls a derelict death has no main
  explosion and no death blast (120, 121), wherever it is and whoever sees it, so the camera never changes combat.
  What it leaves behind depends on visibility:
  - visible, under the cap: 3-5 s of smaller explosions, then a derelict (120, 121);
  - visible, over the cap: the normal explosion picture, still with no blast (170);
  - unseen: a derelict at once, with no effects, where a derelict can exist (120), otherwise nothing (125, 170).

  "Visible" is `IsVisibleToPlayer`: on screen, zoomed in to system view or closer, and in the player's sensor range
  (`Ship_Update.cs:42`).
- **Where:** the stop point decides, and it is known at death, since the drift slows linearly to a stop. A stop
  inside a system makes a **salvageable** derelict. Two cases make a **drifting** derelict instead, and only when
  the death is visible (125):
  - a stop outside every system;
  - a stop inside a star's radiation zone.

  An unseen death that stops outside every system or in a radiation zone leaves nothing (125, 170, 171).
- **How many:**
  - The universe keeps the list of derelicts (129).
  - Base chance 35%, times (1 - salvageable / MaxSalvageableDerelicts), with the limit 40 as a Globals knob
    (129, 142).
  - At most 20 drifting derelicts at once (142); past that, a visible death shows the normal explosion with no
    blast (170).
- **What it is:** a derelict is its own lightweight object, not a `Ship` (120).

### How a derelict moves and ends

- It drifts on its velocity for 10 s to a full stop, with the drift speed capped at the ship's top sublight speed.
  It keeps an x/y/z tumble with smoke and has its lights off. Rotation and smoke update only in the frustum.
- **Drifting derelict:** sinks away from the camera at 50 units/s and is removed at Z 5000, which takes 100 s (129).
- **Salvageable derelict:**
  - It stays on the plane, with no Z drift (141).
  - Its timer is 100 turns x ProductionPace.
  - The timer stops while a salvage yard of an empire that has seen the derelict is in its system (143), and runs
    on again if that yard dies (129).
  - When the timer runs out it sinks away like a drifting derelict.

### Salvage

- **Salvage yard:** a station with a salvage module, like the processing units of mining ops (122).
  - Designs per race are Gilad's: the yard and its salvage ship.
  - The modules are unlocked by Platforms in Combined Arms and by Starship Construction in vanilla and Star Trek
    (139, 153).
- **The salvage ship's cycle (122):**
  1. it launches from the yard;
  2. it flies to the derelict;
  3. it runs small shuttles to and from the derelict, like supply shuttles;
  4. it flies back and lands on the yard to hand over the salvage.
- **Work:** a yard works every derelict in its own system that its empire knows of, with no orders, like a mining
  station (136). An empire sees and salvages only derelicts it has seen, through sensors at the death or later
  (137). Derelicts cannot be shot (150).
- **Two modes per derelict (133, 135):**
  - *strip* recovers techs and production;
  - *recover* recovers only the ship.

  The player picks the mode on the derelict's info panel (the ship info panel shows a selected derelict's data).
  The AI picks it from the tech list: it strips when the list holds techs it wants for its progression.
- **What the derelict records:** per empire, the production share it has stripped and its hull recovery progress
  (123, 133).
- **Production:**
  - The yard keeps the stripped production and freighters haul it to colonies (132 B).
  - Production that arrives while the yard's store is full turns into credits at 1 credit per 5 production, so the
    salvage ships keep working (149, 157).
  - The derelict's total production is its design cost, which already includes the production pace
    (`ShipDesign_Stats.cs:191`). It is taken from the original owner and stored at death (160).
  - When the empires' stripped shares add up to that total, the hull dissolves.
- **Techs (159):**
  - The derelict lists every tech its design needs that the salvaging empire has not researched yet.
  - When the hull dissolves, each empire rolls a check per tech: the hull-role chance x `(1 + ModHpModifier)`. This
    is a new check that borrows the reverse-engineering chances: reverse engineering itself advances an unresearched
    hull tech with no roll, and rolls only to unlock a hull whose tech is already researched (`Empire.cs:2136`).
  - A passed check adds the empire's share x 10% of the tech cost x `(1 + ModHpModifier)`. This can complete a
    tech.
  - On an early end (recovered by someone, broken by a failed roll, timer out), the strippers get their tech by
    their share so far (144).
  - Salvage never gives Remnant tech (127).
- **Progress display:** an empire-coloured ring around the derelict, plus stripping sparks while it is being worked
  (136).
- **AI restraint:** the AI does not salvage in a system where a rival it is not at war with, and has no open
  borders treaty with, is present (138).
- **Trust:** when a salvage yard is built in a system where an AI empire owns a colony, and the two have no open
  borders treaty, no alliance and no war, that AI's trust in the builder drops. The amount is a new personality
  value in `DTrait` (`EmpireData.cs:34`), default 5 (152, 161).
- **Placement:** a yard cannot be placed outside a system (163).
- **Empires that leave:** a dead or absorbed empire's shares and recovery progress go to the absorbing empire,
  otherwise they are dropped (162).
- **Notifications (151):**
  - a new salvageable derelict in a system where you hold a colony or a yard, combined per system per turn;
  - the result of each of your recovery rolls.
- **Pirates (176):** pirates salvage too, in *recover* mode only: they take ships, never production or techs. This
  is for now. How they reach a derelict (their bases working as yards, or salvage ships of their own) is settled in
  the ai sub-branch.

### Recovery

- **Ships only;** a station derelict can never come back as a ship (131). Remnant ships can be recovered (127).
  Recovery is possible only while the stripped total is under 50% (144).
- **The race:** the first empire to reach 100% hull recovery rolls for the ship.
- **The chances (145):**
  - base chance by hull role, like reverse engineering: frigate 60, destroyer 50 (the table's default, 173),
    cruiser 40, battleship 30, capital 20, times `1 + ModHpModifier` (Skilled / Haphazard Engineers);
  - on a failed roll, a second roll decides how bad it is: 10% catastrophic, scaled by the same trait;
  - a plain failure breaks the derelict apart;
  - a catastrophic one makes it explode, with blast damage to nearby ships (133, 155).
- **The recovered ship (126, 134, 146, 147, 158, 164):**
  - repair modules, external armour and engines are dead. A repair module is any module with a repair value except
    a command module: Main Engineering, repair-drone bays, Construction Array and the like (174). `RepairRate`
    sums every module with a repair value, command modules included;
  - armour in internal slots is at 2% health;
  - every other module is at 4% health, command modules included;
  - the next module loss may destroy it, and that is accepted;
  - it comes back at crew level 0, keeps its name and joins no fleet.
- **How it gets home:**
  - Command modules carry repair (vanilla: Bridge 500, CIC 1200, Cockpit 200) and work at full rate at 4%, so repair
    brings the engines back.
  - The recovery orders the trip home, to the nearest planet that repairs, for the player and the AI alike (175).
    The resupply logic would not send it on its own: `ResupplyNeededLowHealth` (`ShipResupply.cs:141`) reads the
    share of live internal slots, not their health, and every internal module is alive at 2-4%.
  - A design with no repair at all (possible in mods) gets its engines at 1% instead.
  - Some stations may carry repair drones, per design.

### Switches (Gilad, 2026-10-15)

- **Globals flag `EnableSalvageOps`** (165): default false in C#, set to true in vanilla `Globals.yaml`. Combined
  Arms, Star Trek and every other mod stay off until they set it. A mod inherits every vanilla value it does not set
  (the `GamePlayGlobals` constructor copies `VanillaDefaults` before the mod's file is read), so this one flag is
  not inherited: the constructor clears it after the copy (168).
- **Off, by either switch (166):**
  - every derelict is a drifting one (visual only);
  - the salvage module, salvage hangar and yard designs are hidden from the design screen and build lists;
  - techs stay as they are (no `RequiresSalvageOps` flag for now).
- **Off by the Globals flag, also (166):** the load-time checks for the salvage default ships are skipped.
  - That means the per-race defaults (like `DefaultMiningShip` / `DefaultMiningStation` in
    `CheckForRequiredShipDesigns`, `ResourceManager.cs:2274`) and any global default in its `requiredShips` list.
  - A mod without salvage designs then logs no errors.
- **The rule option when the Globals flag is off (167):** shown greyed out and ticked, with a tooltip that says the
  mod has no salvage content.

Implementation notes:

- **Rule option "Disable Salvage Ops"** in the new-game rule options, saved with the universe, like Disable Mining
  Ops. The pattern to copy:
  - `UniverseParams.DisableMiningOps` (`UniverseParams.cs:88`);
  - the checkbox (`RuleOptionsScreen.cs:72`);
  - modules hidden in the design screen (`ModuleSelectScrollList.cs:44`).

  The tech side (`Tech.RequiresMiningOps`, `TechEntry.cs:685`) is not copied for now. Mining ops has no
  build-list filter to copy, so hiding the yard designs from build lists is new code.
- **Globals flag** for mods that have no salvage content yet, so salvage ops ship in vanilla first and Combined Arms
  and Star Trek follow. Unlike `DisablePirates` in Globals, which only sets the rule option's default
  (`UniverseParams.cs:108`), this flag is a hard switch: when the mod turns salvage ops off, the rule option cannot
  turn them on.
- **Layout:** the rule options column holds 10 checkboxes at 25 px steps (`RuleOptionsScreen.cs:65-74`). An 11th sits
  at +250, where the Mineral Decay slider starts, so the two sliders below move down 25 px. Check it at the
  1680x1050 minimum.
- **Load:** a save made with salvage ops on, loaded under a mod whose flag is off: the hard switch wins, and the
  saved salvageable derelicts sink away like drifting ones (169).

### Phases and branches

- **Phase 1** is visual only (128): every derelict drifts away after it stops, and nothing is saved. The roll
  still covers every eligible death, so a rolled death never blasts; an unseen one leaves nothing in phase 1 (170).
- **Phase 2** adds persistence, salvage and the AI; **phase 3** adds recovery.
- Each sub-branch merges into `feature/salvage-ops/base` through a GitHub PR with the usual gates, and `main` is
  merged into `base` as fixes land. One final PR goes from `base` to `main`, with a merge commit (154).

## What the code already has

- **Tumbling death.** 35% of the kills that are not instant tumble, only in the frustum or before a planet crash
  (`Ship.cs:1657`). Each axis spins at up to 50/SurfaceArea rad/s (`Ship.cs:1678`). `Ship_Update.UpdateDying`
  (`Ship_Update.cs:353`) adds small explosions, flash, lightning, burning junk and module smoke
  (`UpdateWhileDying`), then `Die` -> `CreateExplosionEffects` -> `Spatial.ShipExplode`, the death blast
  (`Ship.cs:1709`). An explosive hit above max(SurfaceArea/2, 200) damage kills at once (`Ship.cs:1654`).
- **Lights.** Ship lights follow power through `ShipSO.EmissiveScale`; a derelict sets it to 0.
- **Build visuals.** `ConstructionShip.AddConstructionEffects`, run from 100% down to 0, reads as a wreck being
  taken apart. It takes a `Ship`, so the derelict needs its own entry point.
- **Visual shuttles.** `CargoShuttles` has the routes Trade, PortToShip, Drop and ShipToShip. They are visual only
  and never saved. Salvage adds a route between a salvage ship and a derelict; the routes take a `Ship` at each
  end, so that route needs a derelict end.
- **Mining ops pattern.** The `MiningOps` goal plus `MiningBays`: hangar ships launch, land back, and the station
  processes per turn. Freighters already deliver supplies TO a mining station (the `SupplyGoodsToStation` goal);
  hauling FROM a station is new.
- **Reverse engineering on scrap.** `Empire.TryReverseEngineer` (`Empire.cs:2131`): while the hull tech is
  unresearched, `TechEntry.UnlockFromScrap` adds 25% x `(1 + ModHpModifier)` of its cost with no roll; once it is
  researched, a roll by hull role times `1 + ModHpModifier` unlocks the foreign hull.
- **Repair.** `ShipStats` sums `RepairRate` from repair modules, and a dead or unpowered one still gives 10% of its
  rate (`ShipStats.cs:84`). There is no base repair without repair modules; `RepairMod` only scales module repair.
  A planet adds its repair only when the ship is tethered to it or in orbit (`Ship_Repair.cs:42`).
- **Border anger.** `Relationship.UpdateAngerBorders` (`Relationship.cs:725`) adds anger from the known military
  STRENGTH of the other empire's ships inside our borders, and is skipped under open borders or an alliance. An
  unarmed yard and its salvage ships add next to nothing, so the 152 trust penalty is the real lever.
- **The death line.** A ship dies when its intact internal slots fall below 50% (`Ship_Events.cs:38`). Internal
  slots are I or IO modules (`ShipModule.cs:254`), armour included: Internal Bulkhead and Reinforced Bulkhead (I),
  Structure and Ancient Armor (IO). The Codex entry "How a ship dies" (id 100093) says only I; sub-branch 1 fixes
  it while it edits that entry.
- **Command.** `HasCommand` needs a live command module that is powered or draws no power (`ShipStats.cs:87-89`);
  platforms and projectors always have it (`ShipStats.cs:69`). Without it the engines give no thrust
  (`ShipEngines.cs:65`) and `Resupply()` returns `NoCommand` (`ShipResupply.cs:87`).

## Sub-branches

1. **derelicts** (phase 1): which ships count (172); the roll, which removes the blast for every rolled death (170);
   the 3-5 s of small explosions; the `Derelict` object; the drift to a stop with its speed cap; tumble and smoke in
   the frustum; lights off; sinking away at 50/s to Z 5000; the cap of 20. Not saved. Globals knobs. Codex ("Stances,
   targets and how a ship dies", GameText.yaml ~22996: no blast for a derelict, and internal slots are I or IO).
   Tests.
2. **salvageable**:
   - the two switches (rule option and Globals flag);
   - derelicts in a system are saved in the universe list, with their design;
   - unseen deaths in a system become derelicts at once;
   - the stop point and radiation decide which kind a derelict is;
   - the timer with the yard pause;
   - the throttle;
   - selection, the info panel and the ring;
   - each empire's knowledge of a derelict;
   - notifications.
3. **yards**:
   - the salvage module, salvage ships and yard designs (Gilad);
   - the salvage hangar;
   - the work cycle with a `CargoShuttles` route;
   - landing on the yard;
   - stripping with production shares and sparks;
   - the hull dissolving;
   - the tech payout with its check;
   - credits for what the yard cannot keep;
   - the trust penalty.
4. **hauling**: freighters pick up a yard's stored production for colonies that import it (132 B).
5. **ai**: the yard planner and design picker, the strip/recover choice from the tech list, the rival rule (138),
   salvage ships kept out of danger and radiation, pirates in recover mode (176).
6. **recovery** (phase 3): hull recovery progress, the first-to-100% roll, break-up and catastrophic explosion, the
   recovered ship's damage rule, the order home (175).

Each sub-branch adds its Codex text and tests.

## Risks and catches

- The stop point, not the death point, decides whether a derelict is in a system or in a radiation zone.
- A recovered ship of a player design breaks on load unless the derelict's design is saved.
- No blast for derelicts shifts combat a little: stacked fleets take fewer death blasts.
- The roll, not visibility, removes the blast, so the camera never changes combat (170). A tumbling ship that did
  not roll a derelict still blasts when it ends (`Ship.cs:1706-1709`).
- Hauling FROM a station does not exist yet; it is the largest new piece of phase 2.
- Saves grow with salvageable derelicts; the throttle and the timer bound them. Each one is small (no modules).
