# Salvage Ops: derelict ships, salvage stations, recovery

An integration branch `feature/salvage-ops/base`, with sub-branches under it; the patch branch of the day is merged
into `base` as fixes land. Status (2026-10-17): phase 1 (sub-branch 1, derelicts) shipped through
`jupiter-1.60/fixes_49` in PR #445. Sub-branches 2 (salvageable) and 3 (stations) are merged into `base`, which holds
`jupiter-1.60/fixes_50` too, and sub-branch 4 (`feature/salvage-ops/salvaging`) is cut from it; the rest is planned.
Decisions 119-176, 178-190, 222-232, 236-266, 269-302 and 312-426 are all settled; 168-175 came from the review of
this plan, 178-190 from the review of sub-branch 1 and Gilad's look at it in game, 222-232 and 236-266 from phase 2,
269-302 from Gilad's redesign of salvage: one salvage station per empire per system and a platform per derelict, with
each derelict claimed by one empire, replace the yards and their salvage ships, 312-366 from sub-branch 3 and 367-426
from sub-branch 4. The
fixes_49 review settled 210, 212, 213, 216-218 and 220 for derelicts: one lock keeps the derelict list safe on the
sim, save and UI threads, a ship leaves at most one derelict, and a failure to make one is logged.

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
  - It settles a little below the plane, at 25 units/s, as deep as its ship's radius and at most 500, keeping its
    size, so it looks a little distant (Gilad, 2026-10-18; 412, replacing 141). One from an unseen death starts there.
    Clicks, its icon and its selection brackets take its depth into account.
  - Its timer is 100 turns x ProductionPace.
  - The timer stops once an empire claims the derelict (274, replacing 143), and runs on again if the claim is lost.
  - When the timer runs out it sinks away like a drifting derelict, from its rest depth, shrinking from there.
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
    the Codex. Under the bar sits the Build Salvage Station button, or the station order's status in its place
    (452-455), and the Salvage and Recover buttons sit in its drawer (277, 327). Once it is claimed, the flag and name of
    the empire salvaging it show at the bottom, just above the panel's lower border, under the production icon
    (273; Gilad, 2026-10-17; 375 D); the flag and name pulse in that empire's colour, fading out and back, and a
    smaller font fits a long name. The hourglass pulses so while its timer runs, and is grey with its number while the
    timer stands, its tooltip then saying so (375 H).
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
  - It keeps the salvaged production in its cargo. A design with less cargo room than one load, the Salvage
    Platform design's hold (75, following the design; 442), is not a usable salvage station: the picker and the
    Automation dropdown skip it, a race default like that logs an error at load, and the ship designer lists it as a
    design issue, as it does for mining and research stations (`ShipDesignIssues`, `LowRefiningTime` and
    `LowResearchTime`) (302). The load replaced the `SalvageChunkSize` knob in part 4 of sub-branch 4 (405 A).
  - **The design issue (Gilad, 2026-10-17; 340-342):** Low Salvage Cargo, critical, with the research and refining
    icons' layout and production hammers (340 A). Its remedy adds the cargo room one load needs, read from the
    platform design, as other design issues add their numbers (342). Deep Space Build still lists such a design, since the player picks
    it there (341 B). Texts 7159-7162; Codex 100167 and tooltip 7157 say a design with low cargo is skipped, with no
    number (342, 343).
  - It cannot be refit (293), and the orbital pickers ignore it (290).
  - Each race names one in `DefaultSalvageStation`. The picker takes the design with the most salvage modules, then
    the most cargo room, then the sturdiest per slot, as `PickMiningStation` does (295).
  - **The Automation window (Gilad, 2026-10-17; 279, 334-338):**
    - A Salvage Station dropdown, a seventh row in the top table under a plain title, which becomes the Auto Build
      Salvage Stations checkbox with 252 (334 A); and an Auto Pick Salvage Stations checkbox, off in a new game as
      research and mining start (335 A).
    - With auto pick off, the derelict panel's button and the derelicts window order the dropdown's design; Deep Space
      Build orders the design picked in it. The dropdown lists only the salvage stations the player can build, as the
      other dropdowns do, and a choice that can no longer be built falls back to the race's default, then to the best
      pick (336).
    - Both show only with salvage ops on and once a salvage station can be built; the dropdown hides while auto pick
      is on. Texts 7156-7158; Codex 100167 gets the salvage pick rule.
    - The window moves up 70 px, its top at 119 instead of 189, so its last rows clear the minimap at 1680x1050
      (337 A). With Deep Space Build open too it now covers that window's list down to its header; that stays until
      the Automation window becomes a screen of its own (338).
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
    - the Exotic Systems screen (Gilad, 2026-10-17; 328 A, 332), from its rows' button (below);
    - an abort, like `EmpireAI.CancelMiningStation`, removes an order whose station is not built yet, and its
      constructor.
  - **The player's auto-build switch (252)** comes with the AI planner, in the ai sub-branch.
  - **Placement (Gilad, 2026-10-17; 317-320, 322):**
    - In Deep Space Build a salvage station never orbits a planet: near one it shows Cannot Orbit (317).
    - It sits at least `SalvageOps.Clearance` (2000) from every salvageable derelict in the system and from every
      salvage station the empire sees, inside 80% of the system's radius, clear of the star's 20000 and its radiation
      and of planets' orbits (318). With a salvage station picked, Deep Space Build marks the systems where one can be ordered and draws
      those no-placement zones in red.
    - The panel button (318) places it at the average position of the system's derelicts: a random point within 1000
      of it, then within 1500, 2000 and so on, until a point passes the rules above.
    - The button is greyed out, with a tooltip saying why, when no station can be ordered there (319). It is the
      square action button with the Deep Space Build icon (324), with texts 7146-7149 (325). It sits in a drawer that
      slides out to the right of the derelict's panel, like the ship panel's orders, where the Salvage and Recover
      buttons go later (327). A salvage station is scrapped from its own ship panel, which has no refit (326).
    - **In the panel, with the order's status (Gilad, 2026-10-18; 452-456, replacing 324 and the drawer of 327):** the
      button leaves the drawer for the panel itself, under the production bar: a text button, Build Salvage Station
      (7178), at the derelicts window's button size, 168 px, and greyed on the shorter button's grey plate with no
      design to build. Once ordered, its place says Salvage Station Ordered (7179) in wheat until the station is
      built, then Salvage Station Deployed (7164) in the player's colour (452 A), centred across the claimant line's
      width and split onto two lines where the language's text is wider; both carry the 7147 tooltip (454 A). The
      texts were to be Build Station, Station Ordered and Station Deployed, with "Salvage Station" for "Station"
      (453 A); the last is 7164 word for word, so it is reused. The panel has no abort: the derelicts window keeps its
      Abort Deployment. "No free site" shows above the panel's frame, clear of the derelict's name even wrapped to
      four lines (455 A). Codex 100093 names the button and the two statuses. The button's text runs past the
      168 px plate in Russian and Portuguese, as 7163 does on the derelicts window's button. No test covers the
      panel; it is checked by eye (456 B).
    - Salvage stations are queued as urgent orbitals, as research and mining stations are (320).
    - An order not yet built ends, with its constructor, once its system has no salvageable derelict left; a built
      station stays (322).
  - **The Exotic Systems rows (Gilad, 2026-10-17; 328 A, 332):**
    - Each system with salvageable derelicts the player knows of has a salvage row, apart from that system's research
      or Dyson swarm row. The row goes with the system's last such derelict, even with the player's station built there.
    - System: as other rows, with the derelict notification's yellow hourglass to the right of the hostiles warning.
      Star or Planet: how many derelicts ("3 Derelicts"). Resource: the production icon and the production left in
      them, in k or M from 10000. Richness and Owner stay empty.
    - The row's Salvage Ops button opens a window listing the system's derelicts (`SystemDerelictsScreen`). A button
      centred at the bottom of the window's frame, under a list 45 px shorter, orders the station (330 A, 345 C):
      Deploy Salvage Station, placed as the panel button places it, with 7146 as its tooltip; Abort Deployment (4473)
      in the military style once ordered, with no tooltip (347), which also ends an order stuck with no port; Salvage
      Station Deployed in the player's colour once built; and the 7148 grey text when no design can be built. The abort
      runs on the sim thread. Each derelict's row shows, beside its hull and name, a bar of its production left as on
      its panel, the pulsing flag of the empire salvaging it, with its name in the tooltip, and the Salvage button with
      the panel button's rules and tooltips (Gilad, 2026-10-17; 373 A); the Recover button joins it in sub-branch 7.
    - Texts 7150-7155, 7163 and 7164; the derelict passage of 100093 names the rows and the window's deploy and abort
      (331, reworded for 332, 346 and 349), and tooltip 7157 and Codex 100167 name the window as a way to order
      (346); the Codex tooltip inventory is regenerated for them (348).
  - **One kind of station per design (321):** research, refining and salvage modules make three kinds of station, so the
    ship designer refuses a module of one kind in a design that holds another, with a floating message (text 7145).
    The one shipped design that mixed kinds, the Cordrazine C-Mining Station mk3-a, lost its research lab (323).
- **The salvage platform (271 A, 280, 283-285):**
  - One fixed design for every race, `Salvage Platform` in `ShipDesigns/Mandatory`, known by its name as the
    subspace projector is (314). No empire builds it from a list, the ship designer lists it only with debug features
    on, the orbital pickers never see it, and it has no dropdown (279, 290). The player cannot order, refit, scrap or
    list it. It cannot be boarded (285), and has no upkeep (284).
  - The Salvage button on the derelict's panel (277) gives the derelict to a free module of the empire's station in
    its system. The buttons are greyed out when the empire has no station there or no free module. The mode is fixed
    once the constructor launches; Recover comes with phase 3.
  - **The Salvage button (Gilad, 2026-10-17; 368-373, 375):**
    - It sits in the drawer's second slot, under Deploy Salvage Station, and since 452 in its first, alone, a square
      action button with the Salvage Unit's art (375 C): lit while it can order, sunken (the pressed back) while the
      order stands, grey otherwise.
    - Each order is a `SalvageDerelict` goal of its own, saved with the empire's goals, holding the derelict by its
      id and its system; a derelict's id comes from the universe's object ids and is saved with it. The station's
      `SalvageOps` goal ends with the station, so the order cannot live there (368 A); since part 4 an order whose
      station is lost ends at its next turn, its platform with it (420, replacing the platforms working on of 291).
    - Free units: the station's live salvage units less the empire's orders in that system, every one of which the
      one station there works, platforms an absorber's station takes over beyond its count included (296). A busy
      unit that dies keeps a free one counted busy until its platform is done (369 A).
    - Greyed, with a tooltip each (371): no built station of the empire's there, one on order or in other hands
      counting as none; every unit busy; another empire salvaging it, named. Lit with its own tooltip once ordered.
      Texts 7166-7171.
    - Orders placed together reach the sim thread one by one, each checked as it arrives: the first ones hold the
      units, and a second for the same derelict ends. An order whose constructor has not launched ends when its
      station is lost or has no unit for it, the latest one first. Launched orders hold their units before those
      waiting to launch, so a busy unit that dies keeps a free one counted busy (369 A; part 2).
    - **The second click (370 B):** it calls the order off on the sim thread, its claim included, so the derelict's
      timer runs on and any empire can salvage it. A platform at work stops, sends what it has stripped to the station
      as if it had finished, and goes as a finished one does; the tech check of an empire's work that ends (287) runs
      too. With the player's salvage on automatic (252, the ai sub-branch) the click does nothing.
    - **While the platform winds down (Gilad, 2026-10-18; 419 A):** the called-off order is no longer the
      derelict's, so its button is lit again, or grey while every unit is busy, the old platform holding its unit
      until it is gone; the player can order the derelict again, a new platform going up while the old one comes
      apart. A rival's claim leaves the old platform to finish, and the old order's end leaves a new order's claim.
    - Whatever ends an order gives up its claim (`SalvageDerelict.OnRemoved`), the clearing of a defeated or merged
      empire's goals included, but an order with a built platform passes first, its claim with it, to the absorber or
      to the rebels (296, 431 A; part 5).
  - The module launches the empire's current constructor design, the one Deep Space Build uses (271 A, 283). The
    constructor flies to the derelict and builds the platform next to it. If it dies on the way, the order stands
    and a unit launches another once its timer allows.
  - **The launch (Gilad, 2026-10-17 and 18; 378-390):**
    - The salvage unit is a hangar bay, as a refining module is a mining bay (384 A): an `IsSalvageBay` that counts
      in `Is(Hangar)`, kept out of the fighter hangars, ship strength and the designer's hangar picker. The bay
      launches the constructor as a hangar launches its ship (383), with the launch animation, the link between bay
      and ship, and docking on return. A hangar ship heads back to its carrier once its launch animation ends,
      unless it is a supply shuttle, a miner or an assault ship, so a constructor is kept out of that too.
    - A launch costs ordnance, as a hangar launch does (385 B, replacing "at no cost" of 271 A): the constructor's
      mass / 5 x `HangarCombatShipCostMultiplier`, 22-26 for the vanilla constructors. The unit carries 60 ordnance,
      the processing unit's figure (387 A). A station low on ordnance asks the nearest planet that can launch supply
      shuttles for one (`RearmShipFromPlanet`), as an untethered station does; a constructor that docks gives its
      ordnance back.
    - Out of combat a platform or station asks a colony for a supply shuttle however low it runs (Gilad, 2026-10-18;
      390 A). Since Mars 1.50 one below 10% took the combat path, which for an orbital only waits for a supply ship
      nearby, so a salvage station far from colonies could run dry for good; the fix is for every orbital, and its
      panel still says Ammo Reserves Critical below 10%. Codex 100105 already said so.
    - A bay waits 30 s after a launch (`hangerTimerConstant`, 386 A), and after a constructor docks, its repair time,
      5 to 30 s; a constructor still flying holds its bay.
    - The platform's site is a random point on a circle of the derelict's radius + 100 around it (378).
    - The platform's construction is the Salvage Platform's cost x the production pace with the empire's bonuses
      (380), never below 100 x the pace (388 B, 389 A). With its own holds it costs 156 at the normal pace, above the
      floor (443 A), so a vanilla constructor, which adds 35 to 51 on its first turn and 100 on each after, builds it
      on its third turn.
    - A rival's constructor on its way when the derelict is claimed flies home and docks (379), and so does one whose
      order the second click calls off, on manual salvage; with salvage on automatic the click does nothing (381).
    - A constructor in flight whose station is lost is scuttled at once (382).
    - As built (part 2): the order is a deep-space build goal for the Salvage Platform, in three steps (launch,
      build, platform; part 4 adds a fourth, the dismantling), and puts no build marker on the map, since the
      Salvage button calls it off. Its constructor is the one a small Deep Space Build job gets (the dropdown design,
      or the cheapest with auto pick on, whatever the platform costs since 446 B), and a salvage bay launches nothing
      else. It flies and builds as any constructor does and claims the derelict as it starts to build, within 200 of the site; the platform it builds takes over the derelict, and the ordnance the
      constructor took is spent. One boarded on its way is its boarder's ship, and its unit is free to launch another;
      one flying home to a station lost on the way is removed, as a miner is, and a station's death blast can destroy
      one just launched beside it. The designer shows the constructor and its launch ordnance under a salvage unit's
      stats, as it shows a refining module's miner.
    - A lost platform ends its order and its claim (275).
    - The Codex: 100093 says that a derelict's turns stand still from the moment a constructor starts to build there
      (376 A), and 100099 adds the salvage unit's constructor to what each kind of bay launches.
- **The claim (272 B, 273, 274):**
  - The derelict becomes an empire's when that empire's constructor starts to build there. From then on no other
    empire can salvage or recover it. A rival constructor on its way turns back, and its module is free again.
  - The derelict keeps its original owner, whose flag its panel shows and whose design gives its tech list, and adds
    the empire salvaging it (273).
  - Its timer stops at the claim and runs on again if the claim is lost (274).
  - The derelict holds its claimant, saved with it: `Derelict.TryClaim`, which fails while another empire holds it,
    and `ReleaseClaim`, both on the sim thread (372). An order on a derelict another empire claimed ends at its next
    evaluation, freeing its unit.
  - An empire sees and salvages only the derelicts it knows of (137, by the knowledge rules below). Derelicts cannot
    be shot (150).
- **Work (276, 282, 286, 292; Gilad, 2026-10-18; 391-411):**
  - A platform strips `SalvageSpeed` production per turn, a Globals knob with default 12 (394, was 10), into its hold
    as production cargo, and strips only what fits there (391, which drops the "no limit" of 282). Its hold takes 75,
    in cargo modules of its own (442).
  - Each turn it either strips or, with its hold full, sends its whole load to its station, as much as the station
    has room for; the rest stays aboard, and it strips again once it has room (398 A, 404, 406 A). The load replaces
    the chunk: `SalvageChunkSize` goes, and a salvage station needs cargo room for one platform's hold (405 A). The
    sending came with part 4 (399 B).
  - Neither the speed nor the load scales with the game's pace: the derelict's production already does, so salvage
    takes longer at a slower pace, as building does. Techs that raise the speed come in the last phase (270).
  - Visual shuttles run between the platform and the derelict: one for every 3 production stripped that turn,
    rounded up, each from the platform to a random point on the hull and back, in the empire's colour (394). They
    launch 0.2 to 2 s apart after the turn's stripping and take 2 to 2.9 s each way, three times the first draft's
    time, turning back at the hull at once (Gilad, 2026-10-18; 411).
  - While a platform strips it, turn after turn, the derelict flashes at random points on its hull and small pieces of
    junk fly off it in random directions, without a break between turns, stopping once the platform stops stripping,
    as with its hold full (409 B, replacing 395 and 401 B). Per second, at random intervals around the rate: 0.66
    flashes per production of the salvage speed, rounded up, 8 at the default speed; and 0.32 pieces of junk per
    production of the salvage speed, 3.84 at the default speed, each at 30 to 80 a second (410, doubled by Gilad,
    2026-10-18; 425).
  - The shuttles, flashes and junk all show only while the player sees the platform and the derelict is on screen, as
    a supply ship's shuttles do (408 B): the shuttles from planet view in, as other cargo shuttles; the flashes and
    junk from system view in, as the derelict's smoke and breakup. There is no progress ring (286). The derelict stays
    out of the game objects, so its junk has an entry point of its own.
  - The selected platform's panel (392 C, 393 A, 400 A, 402 B, 403 A): its status reads "Salvaging" and the
    derelict's design, or "Salvage hold full"; two bars, as a miner's, each with a tooltip: its load out of its hold,
    and the derelict's production left, hidden once the derelict is empty; and its load above the panel, as a
    freighter's. The derelict's own panel keeps its bars, its production bar red as on the platform's panel, and so
    does each row of the derelicts window (Gilad, 2026-10-18; 424).
  - When the derelict is empty it can no longer be salvaged: it leaves every salvage list, selection and
    notification, and sinks away as one whose turns ran out, at twice the speed (396 B).
  - Once the platform has sent all it holds, it takes itself apart, as its build run backwards, and dissolves in the
    end, freeing its unit (403; shown as a build since 423).
  - **Loads and dismantling (Gilad, 2026-10-18; 413-426):**
    - A load's shuttles: one per 15 production, rounded up, at most 20 (413; 15 since 442, was 10), flying one way
      from the platform to the station and ending there with a flash (415 A). They fly at the normal pace of cargo shuttles, not the hull
      shuttles' third (414), taken as 600 a second on average, three times the hull shuttles' 200 or so: the trip's
      time follows its distance, each flight takes 0.42 to 0.6 of it and launches within its first 35%, as a supply
      ship's shuttles do. Each is drawn wherever it is on screen, from planet view in, and a load launches them only
      where the player sees the platform or the station (416 A). A shuttle whose ship is gone before it launches never
      launches, on every ship-to-ship route, a supply ship's included, so a load's last shuttles do not launch from
      the empty space a platform left on a long trip (Gilad, 2026-10-18; 422 A). A load's shuttles are twice the
      size of the others, their glow and trail alike: each shuttle takes a size when it is sent, which multiplies the
      glow's and trail's scales, 1 for every other route (426).
    - Once its derelict is stripped empty or its order is called off, the platform sends its station all it holds,
      full or not, and starts to come apart in the turn its last load leaves; with no room in the station it waits.
      It takes two turns (417 B), shown as a build from its start to its end: the build's blue sparks, sparks,
      lightning, flashes and parts from 0% to 100%, seen from ship view in where the player sees the platform (Gilad,
      2026-10-18; 423, replacing the build run backwards of 418 A). Once complete it vanishes in a flash and its
      parts fade, as a finished build's do, and its order ends, freeing its unit.
    - With no salvage station of its empire's in the system, a platform is gone at its order's next turn, at work or
      coming apart, its load with it, and its claim is given up: it never works on without a station (Gilad,
      2026-10-18; 420, replacing the platforms working on of 291).
    - Its statuses (420 A): "Station hold full" while its load waits for room, "Salvage finished" once the work is
      over with a load aboard, and "Dismantling"; "Salvage hold full" stays for the turn before a full load leaves.
      The derelict's bar hides once the work is over, the second click included (421 A).
    - Until hauling (sub-branch 5) nothing empties a station, so a station that fills stops its platforms: their
      loads wait aboard, and a finished one waits to come apart.
    - As built (part 4): texts 7175-7177; the Codex section in 100093 gains the loads, the dismantling, the freed
      unit, the lost station and the second click's wind-down. Texts 7143 and 7167 are now true. A mod whose
      Globals.yaml still sets `SalvageChunkSize` loads with a warning in the log.
  - **The platform's holds (Gilad, 2026-10-18; 442-451):**
    - The Salvage Platform carries cargo modules of its own: four `SalvageCargo2x2` (15 cargo, cost 40) and six
      `SalvageCargo1x1` (2.5, cost 10), a hold of 75, Gilad having halved his first 30 and 5 (442).
    - Its category is Unclassified, no longer Civilian: its panel drops " - Civilian", its low-health warning starts
      at 85% of its internal health instead of 97.5%, and the evade stance it loses never applied to an unarmed ship.
    - No tech unlocks the two, so no empire has them and the ship designer never lists them; nothing needs them
      unlocked, since no empire builds the platform from a list. The design is no longer unlockable either, so with
      debug features on the designer's Load list leaves it out; the developer sandbox lists it greyed out, and only
      its Load button opens it.
    - A load's shuttles are one per 15 production, rounded up (442), so a full hold sends 5, under the cap of 20.
    - Their costs stand (443 A): the platform costs 224, 156 at the orbital 70%, above the floor of 100.
    - They keep the vanilla Small and Medium Cargo Hold names and descriptions, which only the debug designer shows
      (444 A), and no test pins that no tech unlocks them (445 B).
    - At 12 a turn a full hold takes 7 turns, and a salvage station needs cargo room for 75; every race's basic
      station holds 316.
    - With auto pick a salvage unit launches the cheapest constructor whatever the platform costs (446 B): the
      platform's 156 is more than twice a constructor's cost of 78 or less, and a Deep Space Build job that size takes
      the fastest design with the most construction modules instead.
    - Review (447-451): the shuttle and construction tests were tightened (447) and the plan's wording fixed (448).
      Left as they are: the 2x2 hold's vanilla `Name` and `Description` lines, which the loader ignores (449), and the
      designer's debug-features clause for the platform, which a mod's platform of unlockable modules still needs
      (450). Combined Arms uses no vanilla modules, so its platform needs holds of its own once it turns salvage on;
      that waits until the feature is complete for BB (451).
  - Production that does not fit never turns into credits (replaces 149 and 157).
  - As built (part 3): texts 7172-7174 (the two statuses and the load bar's tooltip, hooked to 100093; the derelict
    bar reuses the derelict panel's 7130), and the Codex section "Salvaging a derelict" in 100093, after "How a ship
    dies", with the Salvage button, its units, the claim and the work (374 A, 397 A). The tooltip of the second
    click (7167) and the salvage unit's description (7143) already say the platform sends its load to the station,
    which part 4 made true, and the Salvage button's tooltip (7166) promises technology, which part 6 brings; nothing
    ships before them, so they stay as they are (407 A).
  - The hold's room is what its cargo container still takes (`Ship.CargoRoom`): a container keeps the limit it had
    when first loaded, while the cargo room changes as cargo modules die and are repaired. Since part 5 no boarder
    takes a platform (285, 427 A), so only the debug tools put one in other hands; one that is ends its order and its
    claim, as a lost one does, and stays with its new owner, whole if it was coming apart.
- **Losses (275, 285, 291, 296, 298, 356-360, 362):**
  - A destroyed platform releases its claim, and what it held is lost; the derelict keeps what is left of it.
  - A station that is lost (destroyed, boarded or scrapped) takes its platforms with it, at work or coming apart,
    with their loads and claims (420, replacing 291, which left them working until their empire had a station there
    again; built in part 4).
  - A station is taken over at the moment it changes hands, by boarding or by a gift, a merger's included
    (`SalvageOps.TakeOver`, called from `LoyaltyChanges`): its old owner's order ends, and its new owner gets one from
    the salvage step on, whatever the old owner was, so a defeated empire's station works for its rebels and one taken
    from pirates works for its boarder. The change of hands runs on the sim thread, and is queued for it when a save
    or the UI applies it; a queued one that the station's loss or a later change of hands has overtaken does nothing.
    A save applies the pending changes of hands on the sim thread before it starts (`UniverseScreen.Save.cs`), so the
    save worker, which applies them too, writes none half done.
    An order that finds its station in other hands, as after a save that caught a change of hands halfway, hands it
    on. `UniverseObjectManager.UpdateLists` cleans up a ship removed as it changes hands, as an evacuated station is.
  - Taking one over brings no built notification and costs no trust, which is for building one (350-354). An order of
    the new owner's there not yet built is dropped with its constructor (356 A). If the new owner already has a station
    in that system that still stands, a boarded station's boarders launch as troop transports, it is evacuated at once,
    as an extra research station is, and a player is told, with text 7165 (285, 357 A). Pirates keep one they board,
    for now, though they scuttle a projector, research or mining station (358 B).
  - An absorbed empire's platforms and claims go to the absorbing empire, and so do its stations, which change hands
    a step after the merger; an order of the absorber's there not yet built is dropped (356 A). Where the absorber
    already has a station, the absorbed one is removed at once with its troops, as an absorbed research station is,
    and no one is told (296, 359), though by then its troops are the absorber's and could launch (kept, 362 A). The
    absorber's station takes over the removed one's platforms (296).
  - The Codex says so in 100086 (Deep Space Building), and 100130 (Federations) adds the salvage stations to the
    ships a merger loses (360).
  - **Losses and takeovers (Gilad, 2026-10-18; 427-441):**
    - A platform is never a boarding target (427 A): carriers and troop ships never pick it, a right-click on it with
      a troop ship gives the refusal click, transporters pass it over, a planet sends no boarders at it, and a shuttle
      already on its way drops the order with its troop aboard, as with a target that takes off. Pirates never pick
      one for a raid, as they never pick a ship taking off (428 A): an unarmed platform would be a cheap kill that
      levels them up, and their raids on salvage come with the ai sub-branch.
    - An absorbed empire's orders with a built platform pass to the absorber, at work or coming apart, with their
      claims; the platforms change hands a step after the merger, and their orders wait for it. Its orders not yet
      built end and give up their claims (429 A). A constructor launched from a salvage unit is a hangar ship, which
      the merger's scrap order cannot take, so an order that ends with its empire gone scuttles its constructor
      itself, and 100130's lost constructors hold for it.
    - The absorber's own order on a derelict that a passed platform works ends at its next turn, its constructor
      flying home, as 356 A drops the absorber's unbuilt station order (430 A): an order not yet built gives way to
      another of its empire's with a platform at work on its derelict, checked at its own turn, since a merger can
      run while the absorber's goals are being evaluated (436 A).
    - A defeated empire's platforms go to its rebels with their orders and claims, as in a merger, and work for the
      rebels' station (431 A); the rebels, a faction, run their goals and are never defeated for having no planets.
    - An absorbed empire's shares go to the absorber and a defeated one's are dropped, at the merger or the defeat
      (162; 432 A, 435 A), so the rebels earn tech only for what they strip themselves.
    - A merger called off the sim thread, as a federation accepted in the diplomacy screen is, queues itself onto it,
      which also takes away the race of planets and ships changing hands while the sim updates them (434 A). The sim
      thread drains its queue while the game is paused under the diplomacy screen, so the merger lands at once.
    - The Codex (433 A): 100093's salvaging section says a platform cannot be boarded, that an empire joining yours
      brings its platforms and the derelicts they salvage, working for your station there, and drops an order of
      yours on one of them not yet built, and that a defeated empire's station and platforms go to its rebels;
      100114 (Capturing Ships) says a salvage platform cannot be boarded; 100130 adds that the absorbed empire's
      salvage platforms work on for your station in their system.
    - The absorber learns of the derelicts where its passed ships are, as a ship changing hands there counts as
      entering the system (437 A; see Knowledge); Codex 100093 says so.
    - From the review (Gilad, 2026-10-18): a platform built in the very step of a merger or defeat joins its empire's
      ships a step later, so the merger does not queue its change of hands, and its passed order ends with the
      platform left to the empire that is gone; so does one in a second merger the same step. Left as is (438 B).
      The right-click refusals differ: a single troop ship hears the affirmative click before the refusal, as for a
      constructor; a fleet's troop ships lose their orders before each is refused with a click; a group's is silent and
      keeps its order. Logged for a later fix (439). A merger queued from the diplomacy screen can land while an async
      save is still writing, as every queued action can; kept (440 A). The save helper of the takeover tests is a copy
      of the stations' one, as each salvage test class keeps its own helpers (441 A).
    - As built (part 5): `Ship.CanBeBoarded`, `SalvageDerelict.PassOn` (called by `Empire.AbsorbEmpire` and
      `Empire.SetAsDefeated` before the goals are cleared, the rebels now created first), `Derelicts.PassShares`,
      `Derelicts.ChangedHands`, and `LoyaltyChanges.ChangesTo`, which tells a passed order its platform is still
      changing hands.
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
  - A ship that changes hands inside a system counts as entering it for its new owner, by a merger, a defeat's
    rebels, a boarding or a gift alike, with the notification for the player (Gilad, 2026-10-18; 437 A). Without it
    an absorber could hold a station and platforms in a system whose derelicts it never knew of, and the player could
    neither see nor call off a passed order. `Derelicts.ChangedHands`, from `LoyaltyChanges`, on the sim thread.
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
    leaves 200 to salvage, 17 turns of stripping at the default speed. Every salvageable derelict leaves at least 50,
    times the production pace (`Derelict.MinSalvageProduction`; Gilad, 2026-10-17; 355), as the Codex derelict
    passage of 100093 says.
  - When the shares add up to that total, the hull sinks away (396 B).
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
- **Trust (Gilad, 2026-10-17; 152, 161, 350-354):** when a salvage station is built, each AI empire with a colony in
  its system that knows the builder (350 A), and has no open borders treaty, no alliance and no war with it, trusts
  the builder less, once per station. The amount is `DTrait.SalvageStationTrustLoss`, 5 for every personality, as
  `DiplomaticTraits.xml` sets none (351 B). Against the player it is multiplied by the difficulty's
  `DifficultyModifiers.SalvageStationTrustLossVsPlayer` (352), 1 at Normal, 1.25, 1.5 and 1.75 at Insane, the scale of
  `AngerMultiplierVsPlayer` (354). The player is not told (353 A); a one-time diplomacy warning, like the
  colonisation warnings, is logged for later (353 B). It is an AI reaction, so the Codex says nothing of it.
- **Placement:** a station cannot be placed outside a system (163).
- **Empires that leave:** a dead empire's shares are dropped, and an absorbed one's go to the absorbing empire
  (162, 296; built in part 5, 432 A, 435 A).
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
  capture it, and recover the derelicts in its system with it. A salvage station pirates board already stays theirs
  (358 B), but does nothing for them until then.

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
- **Done (227):** with salvage ops off:
  - the module list leaves out the salvage unit;
  - the design load screen leaves out the salvage stations and the platform, work in progress included and debug
    features or not; only the developer sandbox still shows them;
  - `CanBeAddedToBuildableShips` keeps salvage stations out of every empire's buildable ships.

  When ordering is built, it checks `SalvageOps` itself, since a save keeps its buildable ships.
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
  that route needs a derelict end. A load from a platform to its station has a route of its own, timed by its
  distance (414, 416 A).
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
  timer (`Ship.PiratePostChangeLoyalty`). The troops aboard used to die with it; since 298, fixed on
  `jupiter-1.60/fixes_50`, they launch first and fly home. A salvage station pirates board is kept (358 B).

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
     design (314), their load checks, the station picker and its Automation dropdown (279, 295, 334-338); the design
     issue for too little cargo (302, 340-344); no refit (293); the orbital pickers ignore both (290);
   - the switches hide the salvage module and the station and platform designs, and the Globals flag skips their
     load checks (227);
   - ordering a station: the `SalvageOps` goal, one per empire per system, Deep Space Build, the button on the
     derelict's panel, the Exotic Systems rows and their derelicts window, and the abort (248-251, 269, 278,
     330, 345-347);
   - a boarded station working for its new owner, whose order there is dropped, or evacuated after its boarders
     leave when the new owner already has one there, and pirates keeping one they board (285, 356-358); an absorbed
     empire's stations, and its second station in a system (296, 359); the Codex text for both (360);
   - the trust penalty (152, 161, 350-354);
   - a salvageable derelict's production never below 50, times the production pace (355);
   - the station-built notification, under the switch it shares (300);
   - left from sub-branch 2:
     - Gilad's check in game that a ship entering a system with derelicts brings the notification, with debug mode
       off, since debug mode shows the player every ship and so a debug kill is always known as it forms: done, ok
       for now (Gilad, 2026-10-17);
     - 267: saves made on the salvageable branch before `c1e1ef38a`, whose salvageable derelicts load with no system,
       get no fallback, as no save made during feature work does (Gilad, 2026-10-17; 361);
     - 268: the level 5 description on the infiltration screen (6339), like each level's other benefits, says that
       its network learns of the salvageable derelicts left by their ships and by ships they destroy, and Codex 100196
       adds it to level 5's rewards, holding above the limit level as the dossier lines do (Gilad, 2026-10-17). The
       Russian and Portuguese are kept to five lines, clear of the progress bar at 1680x1050.
   - logged for later from the review before the merge into base (Gilad, 2026-10-17; 366):
     - the ship designer refuses a salvage unit whose footprint would clear a research lab or refinery away from the
       cursor, as it counts only the module under the cursor as replaced; it is only ever stricter than needed;
     - per-frame UI work: Deep Space Build walks every ship each frame while a salvage station is being placed, and
       the derelict panel and the derelicts window run the station picker each frame;
     - text 7143 (the salvage module's description) already speaks of platforms and stripping, which come with
       sub-branch 4; for the Codex review stage.
4. **salvaging** (branch `feature/salvage-ops/salvaging`, 247), in parts (Gilad, 2026-10-17; 367): 1, the button and
   the claim; 2, the constructor and the platform, with the Codex sentence of 100093's derelict passage that a
   claim stops the timer, as claims first happen then (376 A); 3, the work, with the rest of the Codex text for the
   button and the claim (374 A), in a section of its own in 100093 (397 A); 4, the loads and the dismantling (399 B);
   5, losses and takeovers; 6, the tech payout; 7, the notifications:
   - the Salvage button on the derelict's panel and the derelicts window's rows, with the order, its units and its
     second click (277, 368-373, 375; part 1);
   - the module's free constructor and the platform it builds (271, 283);
   - the claim, the "salvaged by" flag and the timer's stop (272-274; part 1, the claim made by the constructor in
     part 2);
   - the platform's work: stripping at `SalvageSpeed` into its hold, with a `CargoShuttles` route to the derelict and
     flashes and junk on it (282, 286, 391, 394, 395); the selected platform's panel shows the salvage progress, as a
     selected mining ship's shows its own (392, 393, 402, 403); the player can select a platform but gives it no
     orders (314);
   - the hull sinking away once empty, after which it can no longer be selected, nor reached from its notification,
     though its turns have not run out (396);
   - the loads to the station with their shuttles, and the platform taken apart (276, 282, 292, 398, 403-406,
     413-421; part 4);
   - losses and takeovers (275; 291 as replaced by 420 in part 4), boarders never taking a platform (285), and the absorber's station taking
     over the platforms of an absorbed second station (296); 427-441, part 5;
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
8. **Codex review** (Gilad, 2026-10-17; 333): the last stage. Gilad reviews the Codex text of the whole feature.

Each sub-branch adds its Codex text and tests.

## Risks and catches

- The stop point, not the death point, decides whether a derelict is in a system or in a radiation zone.
- A recovered ship of a player design breaks on load unless the derelict's design is saved.
- No blast for derelicts shifts combat a little: stacked fleets take fewer death blasts.
- The roll, not visibility, removes the blast, so the camera never changes combat (170). A tumbling ship that did
  not roll a derelict still blasts when it ends (`Ship.cs:1760-1765`).
- Hauling FROM a station does not exist yet; it is the largest new piece of phase 2.
- A platform keeps what it stripped in its hold until it sends a load, so losing one loses that load (275).
- Saves grow with derelicts: the throttle and the timer bound the salvageable ones, and the cap of 100 the drifting
  ones. Each one is small (no modules).
