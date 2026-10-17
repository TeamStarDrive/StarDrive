# Salvage Ops: derelict ships, salvage stations, recovery

An integration branch `feature/salvage-ops/base`, with sub-branches under it; the patch branch of the day is merged
into `base` as fixes land. Status (2026-10-16): phase 1 (sub-branch 1, derelicts) shipped through
`jupiter-1.60/fixes_49` in PR #445. Sub-branch 2 (salvageable) is merged into `base`, which holds
`jupiter-1.60/fixes_50` too, and sub-branch 3 (`feature/salvage-ops/yards`) is cut from it; the rest is planned.
Decisions 119-176, 178-190, 222-232, 236-266 and 269-302 are all settled; 168-175 came from the review of this plan,
178-190 from the review of sub-branch 1 and Gilad's look at it in game, 222-232 and 236-266 from phase 2, and 269-302
from Gilad's redesign of salvage: one salvage station per empire per system and a platform per derelict, with each
derelict claimed by one empire, replace the yards and their salvage ships. The fixes_49 review settled 210, 212, 213,
216-218 and 220 for derelicts: one lock keeps the derelict list safe on the sim, save and UI threads, a ship leaves at
most one derelict, and a failure to make one is logged.

## The rules (Gilad, 2026-10-15)

### Which deaths make a derelict

- **Who:** military roles only, frigate and above (124, 172). Instant kills can become derelicts too (130). In
  `RoleName` carrier, bomber, support, troopShip and station sort below frigate, so the rule takes both roles:
  - the hull role is frigate, destroyer, cruiser, battleship or capital;
  - the design role is military: a warship, carrier, bomber, support or troop ship. Freighters, colony ships,
    scouts and constructors on those hulls do not count.
- **Stations:** an untethered station-role design (131, 172). Shipyards, subspace projectors and platforms never
  become derelicts (131). Research stations, mining stations, Dyson swarm controllers, salvage stations and Remnant
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
- **Planet crashes (178 B):** a ship that is about to crash on a planet does not roll for a derelict, so crash sites
  stay as common as before.
- **Debug keys (Gilad, 2026-10-15):** in debug mode, Ctrl+X (or Ctrl+Delete) destroys the selected ship as a drifting
  derelict with no roll, and does nothing to a ship that cannot become one. Like any unseen drifting death, a ship that
  is not on screen at system view or closer leaves nothing. Ctrl+Shift+X (or Ctrl+Shift+Delete) destroys it as a
  salvageable derelict, seen or not, and does nothing unless it would stop where a salvageable one can be (in a system,
  clear of radiation, salvage ops on) and the 100 salvageable derelicts are not all there.
- **Where:** the stop point decides, and it is known at death: a salvageable derelict slows linearly to a stop over
  10 s, so it stops 5 s worth of its drift speed from where it died. A stop inside a system makes a **salvageable**
  derelict. Two cases make a **drifting** derelict instead, and only when the death is visible (125):
  - a stop outside every system;
  - a stop inside a star's radiation zone, or not more than 2000 beyond it, the danger margin the game already uses
    (`SolarSystem.InSafeDistanceFromRadiation`, 229), so salvage platforms never work inside radiation.

  An unseen death that stops outside every system, or in a radiation zone or its margin, leaves nothing (125, 170,
  171).
- **How many:**
  - The universe keeps the list of derelicts (129).
  - Base chance 10%, times (1 - salvageable / MaxSalvageableDerelicts), with the limit 100 as a Globals knob
    (129, 142; 35% and 40 until 231). At the limit the chance is 0; a death that rolled just before it was reached leaves a drifting
    derelict if it was seen, and nothing if it was not.
  - At most 100 drifting derelicts at once (142, raised from 20 by 188); past that, a visible death shows the
    normal explosion with no blast (170). The cap counts drifting and sinking derelicts that have a model, so a wreck
    with no model takes no place in it (232). It limits new ones only: after a load or a content reload the wrecks
    that come back get their models when seen, so for at most their 100 s sink more than 100 can have one.
- **What it is:** a derelict is its own lightweight object, not a `Ship` (120).

### How a derelict moves and ends

- It drifts on its velocity at death, capped at the ship's top sublight speed (185). A drifting derelict keeps that
  speed (184); a salvageable one slows to a full stop over 10 s. It keeps an x/y/z tumble with smoke and has its
  lights off. Rotation and smoke update only in the frustum.
- **Smoke (Gilad, 2026-10-15, 187):** at death it smokes from 20% of its modules (at least 6), dead ones first, then
  damaged ones; they go out one by one over 60 s until 6 are left.
- **Breakup (179, 180):** for 3-5 s, a small explosion every 0.15-0.5 s with a flash, sparks, and a few burning junk
  pieces at death. Each blast plays `sd_derelict_breakup`: the `Explo1` weapon explosion sounds at 40% of their
  volume, with a limit of 4 at once of its own, so weapon impacts keep their 8 (the Explosions category's limits stay
  shared). The small detonation sound plays once at death for every size. The lights flicker out over the breakup,
  and a ship that was already dark stays dark. The tumble keeps the tumbling-death rate (181), and a derelict stays
  drawn after the player loses sensor contact (182).
- **Drifting derelict:** after its breakup it starts to sink away from the camera at 50 units/s (186), and is removed at
  Z 5000, which takes 100 s (129).
  As it sinks it shrinks, to 10% of its size at Z 5000 (183): at planet view the camera is 35,000 above the plane,
  so the depth alone would leave it at 88% of its size when it goes.
- **Salvageable derelict:**
  - It stays on the plane, with no Z drift (141).
  - Its timer is 100 turns x ProductionPace.
  - The timer stops once an empire claims the derelict (274, replacing 143), and runs on again if the claim is lost.
  - When the timer runs out it sinks away like a drifting derelict.
  - **On the map (226, 238):** the player sees only the derelicts the player knows of (137). Wherever a ship shows its
    tactical icon, zoomed out past ship view or with Alt held, a known one shows as a grey icon of its hull role, at
    the size a ship's icon has.
  - **Selection (223, 236, 237):** the player selects only a salvageable derelict the player knows of, by its icon or
    its hull, as a ship is selected. Once its turns run out it can no longer be selected, and a drifting derelict never
    can. While one is selected, a right click on a ship does not select the ship, as with a selected planet.
  - **Its panel:** it sits where the ship info panel does, laid out like it: the design's name and role, the hull in
    grey, a column of icons at the top left with tooltips (the original owner's flag, whose tooltip names the empire,
    then a yellow hourglass with the turns left; 240, 241), and on the right, in the ship panel's first bar row, a bar
    of the production remaining out of what it started with (239, 242). The turns and production tooltips link to
    the Codex. The rest of the right side is kept for bars and buttons: the Salvage and Recover buttons (277), and
    once it is claimed, the flag of the empire salvaging it (273).
- **Its model (224, 230):** a derelict from an unseen death, one loaded from a save, or one after a content reload has
  no model. It loads one from its hull on the UI thread the first time it is on screen at system view or closer and
  known to the player, and keeps it until it sinks away. With no model it still drifts and sinks; one the player does
  not know of is gone once it starts to sink, as there is nothing to show. The two caps and the 100 s sink bound them,
  so it needs no 15 s cleanup like ships. A content reload (Apply in the options) takes every model out of the scene; the derelicts
  stay and load theirs again when next seen.
- **A save (225, 230):** every derelict is saved, drifting and sinking ones too, with its kind and depth, whatever the
  salvage switches say. The save keeps all its smoke points while they thin out, then the 6 left, so a loaded
  derelict smokes from the same places. A derelict saved during its breakup comes back dark with no more blasts, and a
  drifting one starts to sink at once.

### Salvage (Gilad, 2026-10-16; 269-302 replace the yards and salvage ships of 122 and 136)

- **The salvage station:**
  - A station-role design with a salvage module, `<Salvage>True</Salvage>` in the module's data, as a refining
    module makes a mining station (290). The ship designer lets the module go on station hulls only (297), and a
    platform-role design never counts, whatever it carries.
  - Each live salvage module works one derelict at a time. A module that dies launches nothing new; a platform it
    already sent carries on (290).
  - It keeps the salvaged production in its cargo. A design with less cargo room than one chunk
    (`SalvageChunkSize`) is not a usable salvage station: the picker skips it, a race default like that logs an
    error at load, and the ship designer lists it as a design issue, as it does for mining and research stations
    (`ShipDesignIssues`, `LowRefiningTime` and `LowResearchTime`) (302).
  - It cannot be refit (293), and the orbital pickers ignore it (290).
  - Each race names one in `DefaultSalvageStation`. The picker takes the design with the most salvage modules, then
    the most cargo room, then the sturdiest per slot, as `PickMiningStation` does (295), and the Automation window
    gets a dropdown for it (279).
  - Designs per race are Gilad's: the station and the platform (280). The modules are unlocked by Small Stations in
    vanilla (312), by Platforms in Combined Arms and by Starship Construction in Star Trek (139, 153).
- **Ordering a station, as mining stations are ordered:**
  - **The cap (269 A, 278):** one salvage station per empire per system, built and ordered together, so empires
    can each have one in the same system. It is a fixed rule, with no Globals knob.
  - **The goal:** a `SalvageOps` goal like `MiningOps` (`Commands/Goals/MiningOps.cs`). It builds a constructor,
    waits for it, and then the station works. It holds the system and the place to build, and is saved with the
    empire's goals. Every way of ordering a station makes one, and the cap counts them.
  - **Where it sits (249):** where it is placed in the system, not tethered to a planet, as wrecks lie anywhere in a
    system. Where auto salvage ops and the AI place a station is settled in the ai sub-branch.
  - **The ways to order one:**
    - Deep Space Build, only inside a system with a salvageable derelict the empire knows of (163, 251), under the
      cap;
    - a button on the derelict's panel, when the empire has no station in that system, which places the station
      near that derelict (250);
    - rows in the Exotic Systems screen for the systems with salvageable derelicts the empire knows of, with a deploy
      button and an abort, like the mining rows (`ExoticSystemsListScreenItem`);
    - an abort, like `EmpireAI.CancelMiningStation`, removes an order whose station is not built yet, and its
      constructor.
  - **The player's auto-build switch (252)** comes with the AI planner, in the ai sub-branch.
- **The salvage platform (271 A, 280, 283-285):**
  - One fixed design for every race, `Salvage Platform` in `ShipDesigns/Mandatory`, known by its name as the
    subspace projector is (314). No empire builds it from a list, the ship designer lists it only with debug features
    on, the orbital pickers never see it, and it has no dropdown (279, 290). The player cannot order, refit, scrap or
    list it. It cannot be boarded (285), and has no upkeep (284).
  - The Salvage button on the derelict's panel (277) gives the derelict to a free module of the empire's station in
    its system. The buttons are greyed out when the empire has no station there or no free module. The mode is fixed
    once the constructor launches; Recover comes with phase 3.
  - The module launches the empire's current constructor design, the one Deep Space Build uses, at no cost (271 A,
    283). The constructor flies to the derelict and builds the platform next to it. If it dies on the way, the order
    stands and the module launches another the next turn.
- **The claim (272 B, 273, 274):**
  - The derelict becomes an empire's when that empire's constructor starts to build there. From then on no other
    empire can salvage or recover it. A rival constructor on its way turns back, and its module is free again.
  - The derelict keeps its original owner, whose flag its panel shows and whose design gives its tech list, and adds
    the empire salvaging it (273).
  - Its timer stops at the claim and runs on again if the claim is lost (274).
  - An empire sees and salvages only the derelicts it knows of (137, by the knowledge rules below). Derelicts cannot
    be shot (150).
- **Work (276, 282, 286, 292):**
  - A platform strips `SalvageSpeed` production points per turn, a Globals knob with default 10, and keeps all of it
    on board, with no limit, until the derelict is empty. Neither the speed nor the chunk scales with the game's pace:
    the derelict's production already does, so salvage takes longer at a slower pace, as building does. Techs that
    raise the speed come in the last phase (270).
  - Visual shuttles run between the platform and the derelict, like freighter shuttles, and the derelict sparks
    while it is stripped. There is no progress ring (286).
  - When the derelict is empty its hull dissolves. The platform then sends up to `SalvageChunkSize` production per
    turn to its station, a Globals knob with default 100, each chunk with a visual shuttle. It sends what fits, and
    waits while the station is full. Once it is empty it is dismantled, and its module is free.
  - Production that does not fit never turns into credits (replaces 149 and 157).
- **Losses (275, 285, 291, 296, 298):**
  - A destroyed platform releases its claim, and what it held is lost; the derelict keeps what is left of it.
  - A station that is lost (destroyed, boarded or scrapped) leaves its platforms working: they strip on and hold
    their production until their empire has a station in that system again, which takes them over, even beyond its
    module count (291).
  - A boarded station works for its new owner. If the new owner already has a station in that system, the boarders
    launch as troop transports and the station is scuttled (285), as captured stations are after 298.
  - An absorbed empire's platforms and claims go to the absorbing empire. A second station in one system is
    scuttled, and the absorber's station takes over its platforms (296).
- **Knowledge (Gilad, 2026-10-16; 257-265):**
  - Each system holds its salvageable derelicts, and each empire holds the set of systems with salvageable derelicts
    it knows of. An empire that lists a system knows every derelict in it. The AI reads these lists.
  - When a salvageable derelict forms, these empires list its system, checked on the sim thread: its owner; its
    killer (the empire whose fire last hit the ship, from `Ship.LastDamagedBy`, which a ship keeps for 10 s after a
    hit); one with a ship there, any ship (263, 264); one with a colony there, factions too; the allies of its owner
    or killer; one that has infiltrated either of them to level 5 (the raw level, not capped by the infiltrator's own
    limit); one with a mole on a planet there; and, as a backstop for a death seen from outside the system, an empire
    that had the ship in its sensors at its death (265). An empire that already lists the system skips the rules.
  - Later, a ship entering a system with derelicts its empire does not list adds the system to that empire's list
    (259, replacing the per-turn sensor check: being in the system is what counts, not sensors). Entering is found
    where the game assigns each ship its system every step (`UniverseObjectManager.UpdateSolarSystemShips`).
  - A derelict leaves its system once it can no longer be salvaged (its turns ran out, later also stripped or
    recovered), and a system with none left leaves every empire's list.
  - The lists are not saved: on load each system takes back its derelicts, and each empire lists every system with a
    derelict it knows of.
  - Learning later by a new colony, a treaty or a mole after the derelict formed is the dynamic-updates step, after
    this.
- **Two modes per derelict (133, 135):**
  - *strip* recovers techs and production;
  - *recover* recovers only the ship.

  The player picks the mode with the Salvage and Recover buttons on the derelict's panel (277).
  The AI picks it from the tech list: it strips when the list holds techs it wants for its progression.
- **What the derelict records:** per empire, the production share it has stripped, built up claim after claim, since
  a lost claim lets another empire finish the derelict (123, 133, 287).
- **Production:**
  - The station keeps the stripped production and freighters haul it to colonies (132 B).
  - The derelict's total production is half the share of health the ship had left at its death, times its design
    cost before any empire's cost bonus, at the game's production pace (`ShipDesign_Stats.cs`
    `GetCostBeforeBonuses`). It is stored at death (160, 239): a ship that cost 1000 and died with 40% of its health
    leaves 200 to salvage, 20 turns at the default speed.
  - When the shares add up to that total, the hull dissolves.
- **Techs (159, 287):**
  - The derelict lists every tech its design needs that the salvaging empire has not researched yet.
  - When an empire's own work on the derelict ends, finished or with its claim lost, it rolls a check per tech: the
    hull-role chance x `(1 + ModHpModifier)`. This is a new check that borrows the reverse-engineering chances:
    reverse engineering itself advances an unresearched hull tech with no roll, and rolls only to unlock a hull
    whose tech is already researched (`Empire.cs:2136`).
  - A passed check adds the empire's share of the total x 10% of the tech cost x `(1 + ModHpModifier)`. This can
    complete a tech. A claim lost early gives tech by the share so far (144).
  - Salvage never gives Remnant tech (127).
- **AI restraint:** the AI does not salvage in a system where a rival it is not at war with, and has no open
  borders treaty with, is present (138).
- **Trust:** when a salvage station is built in a system where an AI empire owns a colony, and the two have no open
  borders treaty, no alliance and no war, that AI's trust in the builder drops. The amount is a new personality
  value in `DTrait` (`EmpireData.cs:34`), default 5 (152, 161).
- **Placement:** a station cannot be placed outside a system (163).
- **Empires that leave:** a dead empire's shares are dropped, and an absorbed one's go to the absorbing empire
  (162, 296).
- **Notifications (151, 294, 299-301):** each kind has a switch in a new alerts tab of the Automation window, which
  takes every alert checkbox out of the window's one tall table (279, 289):
  - a system joining your list of systems with salvageable derelicts (see Knowledge), wherever it is (260), told at
    once on the sim thread, with no per-step check (256 as reworked by 257):
    - it names the derelict that brought the system onto your list, whoever's ship it was, your own included (245);
    - its icon is the hull's icon with the yellow hourglass over it at 0.8 of its size (246);
    - a click takes the camera to where the derelict comes to rest and selects it; once it can no longer be salvaged,
      the click shows the system (244);
    - it never pauses the game (253), and has its own switch, for players who find it a nuisance (299, reversing
      254);
  - a station built: research, mining and salvage stations share one switch (300);
  - a platform that finished stripping, with its production and the tech results, and a claim lost, to a destroyed
    platform or to a rival that started first: one switch for both (301);
  - the result of each of your recovery rolls.
- **Pirates (176):** pirates salvage too, in *recover* mode only: they take ships, never production or techs. This
  is for now. How they reach a derelict is settled in the ai sub-branch; later they may raid a salvage station to
  capture it, and recover the derelicts in its system with it.

### Recovery

- **Ships only;** a station derelict can never come back as a ship (131). Remnant ships can be recovered (127).
  Recovery is possible only while less than half of the derelict's production has been stripped (144).
- **The work (288):** the Recover button sends a platform as the Salvage button does, so only the claiming empire
  works on it. Recovery takes as much work as the derelict's whole production, at the same salvage speed, and then
  that empire rolls for the ship.
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
  - the salvage module and the salvage station and platform designs are hidden from the design screen and build
    lists;
  - techs stay as they are (no `RequiresSalvageOps` flag for now).
- **Off by the Globals flag, also (166):** the load-time checks for the salvage default ships are skipped.
  - That means the per-race default `DefaultSalvageStation`, checked like `DefaultMiningStation` in
    `CheckForRequiredShipDesigns` (`ResourceManager.cs:2274`), and the `Salvage Platform` design, required like the
    designs in its `requiredShips` list (314).
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
  build-list filter to copy, so hiding the station and platform designs from build lists is new code.
- **Globals flag** for mods that have no salvage content yet, so salvage ops ship in vanilla first and Combined Arms
  and Star Trek follow. Unlike `DisablePirates` in Globals, which only sets the rule option's default
  (`UniverseParams.cs:108`), this flag is a hard switch: when the mod turns salvage ops off, the rule option cannot
  turn them on.
- **Layout:** the rule options column holds 10 checkboxes at 25 px steps (`RuleOptionsScreen.cs:65-74`). An 11th sits
  at +250, where the Mineral Decay slider starts, so the two sliders below move down 25 px. Check it at the
  1680x1050 minimum.
- **Load:** a save made with salvage ops on, loaded under a mod whose flag is off: the hard switch wins, and the
  saved salvageable derelicts sink away like drifting ones (169), and one the player does not know of is gone.

### Phases and branches

- **Phase 1** is visual only (128): every derelict is a drifting one, which keeps its speed and sinks away after its
  breakup, and nothing is saved. The roll still covers every eligible death, so a rolled death never blasts; an
  unseen one leaves nothing in phase 1 (170).
- **Phase 2** adds persistence, salvage and the AI; **phase 3** adds recovery.
- Each sub-branch merges into `feature/salvage-ops/base`, and the patch branch of the day is merged into `base` as
  fixes land.
- **Shipping (190, replaces the single final PR of 140 and 154):** phase 1 merged into `jupiter-1.60/fixes_49` with a
  merge commit and shipped in that patch batch, through PR #445 to `main` with the usual gates. Later phases go the
  same way, each into the patch branch of its day: phase 2 once sub-branches 2-6 are all in `base`, since sub-branch 2
  alone leaves derelicts that nobody can salvage.
- **Drifting derelicts and the chance (189 A):** only salvageable derelicts lower the chance (129, 142); drifting ones
  exist only for deaths the player saw, so counting them would let the camera change the blast odds (170). They have
  the hard cap of 100 alone.

## What the code already has

- **Tumbling death.** 35% of the kills that are not instant tumble, only in the frustum or before a planet crash
  (`Ship.cs:1698`). Each axis spins at up to 50/SurfaceArea rad/s (`Ship.cs:1732`). `Ship_Update.UpdateDying`
  (`Ship_Update.cs:359`) adds small explosions, flash, lightning, burning junk and module smoke
  (`UpdateWhileDying`), then `Die` -> `CreateExplosionEffects` -> `Spatial.ShipExplode`, the death blast
  (`Ship.cs:1765`). An explosive hit above max(SurfaceArea/2, 200) damage kills at once (`Ship.cs:1696`).
- **Lights.** Ship lights follow power through `ShipSO.EmissiveScale`; a derelict sets it to 0.
- **Build visuals.** `ConstructionShip.AddConstructionEffects`, run from 100% down to 0, reads as a wreck being
  taken apart. It takes a `Ship`, so the derelict needs its own entry point.
- **Visual shuttles.** `CargoShuttles` has the routes Trade, PortToShip, Drop and ShipToShip. They are visual only
  and never saved. Salvage adds a route between a platform and a derelict; the routes take a `Ship` at each end, so
  that route needs a derelict end. A chunk from a platform to its station is a ShipToShip route.
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
  unarmed station and its platforms add next to nothing, so the 152 trust penalty is the real lever.
- **The death line.** A ship dies when its intact internal slots fall below 50% (`Ship_Events.cs:38`). Internal
  slots are I or IO modules (`ShipModule.cs:254`), armour included: Internal Bulkhead and Reinforced Bulkhead (I),
  Structure and Ancient Armor (IO). The Codex entry "How a ship dies" (id 100093) said only I; sub-branch 1 fixed
  it while it edited that entry.
- **Command.** `HasCommand` needs a live command module that is powered or draws no power (`ShipStats.cs:87-89`);
  platforms and projectors always have it (`ShipStats.cs:69`). Without it the engines give no thrust
  (`ShipEngines.cs:65`) and `Resupply()` returns `NoCommand` (`ShipResupply.cs:87`).
- **Captured stations.** Pirates scuttle a captured subspace projector, research station or mining station on a 20 s
  timer (`Ship.PiratePostChangeLoyalty`), and the troops aboard die with it. That is a bug (298): the troops are to
  launch first, as planets launch invading troops after peace (`Planet.ForceLaunchInvadingTroops`), and fly to their
  nearest planet, or home for pirates. The fix goes on `jupiter-1.60/fixes_50`.

## Sub-branches

1. **derelicts** (phase 1): which ships count (172); the roll, which removes the blast for every rolled death (170);
   the 3-5 s of small explosions; the `Derelict` object; the drift with its speed cap; tumble and smoke in
   the frustum; lights off; sinking away at 50/s to Z 5000; the cap of 100. Not saved. Globals knobs. Codex ("Stances,
   targets and how a ship dies", GameText.yaml ~22996: no blast for a derelict, and internal slots are I or IO).
   Tests.
2. **salvageable**:
   - the two switches (rule option and Globals flag): off, every derelict is a drifting one; the rule option with its
     greyed-out state (227);
   - every derelict is saved in the universe list, with its design and its smoke points: all while they thin out, then
     the 6 left (225, 230);
   - unseen deaths in a system become derelicts at once;
   - the stop point and radiation decide which kind a derelict is;
   - the timer, which counts the player's turns and runs out to a sinking; it ticks where the player's turn is found,
     not in `EndOfTurnUpdate`, which also runs at start and on load (266; its stop at a claim comes with salvaging,
     274);
   - the throttle;
   - the model loaded from the hull on the UI thread (224);
   - selection with its highlight, and the info panel with the derelict's data: design, original owner, turns left
     (223);
   - the grey icon at sector and galaxy view (226);
   - each empire's knowledge of derelicts, by system (see Knowledge): the systems' and empires' derelict lists, and
     the notification of a system joining the player's list (137, 223, 243-246, 253, 254, 256-266).
3. **stations** (branch `feature/salvage-ops/yards`, 247):
   - first, on `jupiter-1.60/fixes_50` and merged in from there: the Automation window's alerts tab with every alert
     checkbox, and in it the one switch for research and mining station deployments that salvage stations join
     later (289, 300); the troops leaving a captured station before the pirate scuttle (298);
   - the salvage module, `<Salvage>True</Salvage>`, on station hulls only (290, 297);
   - the station and platform designs (Gilad, 280); each race's `DefaultSalvageStation` and the one `Salvage Platform`
     design (314), their load checks, the station picker and its Automation dropdown (279, 295); the design issue for
     too little cargo (302); no refit (293); the orbital pickers ignore both (290);
   - the switches hide the salvage module and the station and platform designs, and the Globals flag skips their
     load checks (227);
   - ordering a station: the `SalvageOps` goal, one per empire per system, Deep Space Build, the button on the
     derelict's panel, the Exotic Systems rows and the abort (248-251, 269, 278);
   - a boarded station working for its new owner, or scuttled after its boarders leave when the new owner already
     has one there (285); an absorbed empire's second station (296);
   - the trust penalty;
   - the station-built notification, under the switch it shares (300);
   - left from sub-branch 2:
     - Gilad's check in game that a ship entering a system with derelicts brings the notification, with debug mode
       off, since debug mode shows the player every ship and so a debug kill is always known as it forms;
     - 267: whether saves made on the salvageable branch before `c1e1ef38a`, whose salvageable derelicts load with no
       system, get a fallback;
     - 268: whether the infiltration Codex entries (100190, 100196, 100199) mention that the top level shows a rival's
       derelicts.
4. **salvaging** (247):
   - the Salvage button on the derelict's panel (277);
   - the module's free constructor and the platform it builds (271, 283);
   - the claim, the "salvaged by" flag and the timer's stop (272-274);
   - the platform's work: stripping at `SalvageSpeed` and keeping it on board, with a `CargoShuttles` route to the
     derelict and sparks on it (282, 286); the selected platform's panel shows the salvage progress, as a selected
     mining ship's shows its own; the player can select a platform but gives it no orders (314);
   - the hull dissolving, after which it can no longer be selected, nor reached from its notification, though its
     turns have not run out;
   - the chunks to the station with their shuttles, and the platform dismantled (276, 282, 292);
   - losses and takeovers (275, 291), and boarders never taking a platform (285);
   - the tech payout with its check (159, 287);
   - the platform notifications and their switch, and the switch for the derelict notification (294, 299, 301).
5. **hauling**: freighters pick up a station's stored production for colonies that import it (132 B).
6. **ai**: the station planner and design picker, where auto salvage ops and the AI place a station (249), the
   player's auto-build switch (252), the strip/recover choice from the tech list, the rival rule (138), constructors
   and platforms kept out of danger, pirates in recover mode (176) with salvage station designs of their own, since
   they have no small station hull (315), and later their raids to capture a salvage station.
7. **recovery** (phase 3): the Recover button, recovery work at the salvage speed (288), the roll, break-up and
   catastrophic explosion, the recovered ship's damage rule, the order home (175), the recovery notification, and
   the techs that raise the salvage speed (270).

Each sub-branch adds its Codex text and tests.

## Risks and catches

- The stop point, not the death point, decides whether a derelict is in a system or in a radiation zone.
- A recovered ship of a player design breaks on load unless the derelict's design is saved.
- No blast for derelicts shifts combat a little: stacked fleets take fewer death blasts.
- The roll, not visibility, removes the blast, so the camera never changes combat (170). A tumbling ship that did
  not roll a derelict still blasts when it ends (`Ship.cs:1760-1765`).
- Hauling FROM a station does not exist yet; it is the largest new piece of phase 2.
- A platform keeps all it stripped on board until the derelict is empty, so losing one loses the whole job (275).
- Saves grow with derelicts: the throttle and the timer bound the salvageable ones, and the cap of 100 the drifting
  ones. Each one is small (no modules).
