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
- **When it is decided:** at death, by the roll alone (170), then by a second roll, by its hull, that makes it a
  drifting one (Gilad, 2026-10-21; 680-685): 75% for a frigate, 50% for a cruiser, 25% for a battleship, 10% for a
  capital and 70% for any other, a destroyer or a station among them (`Derelict.DriftingChance`, hard-coded). With
  salvage ops off only a death whose second roll makes it drift is a derelict one; any other dies as usual, with its
  blast. A ship that rolls a derelict death has no main explosion and no death blast (120, 121), wherever it is and
  whoever sees it, so the camera never changes combat. What it leaves behind depends on visibility:
  - visible, under the cap: 3-5 s of smaller explosions, then a derelict (120, 121);
  - visible, over the drifting cap: it takes the place of the oldest drifting derelict with no model, which leaves the
    list (683 A), and with none, the normal explosion picture, still with no blast (170);
  - unseen: a derelict at once, with no effects (120): a salvageable one at its stop point, a drifting one where the
    ship died, going on from there as a seen one would. Outside every system a drifting one is left only if the player
    owned the ship, destroyed it or had it in sensor range as it died (681); otherwise, and over the drifting cap,
    nothing (125, 170). The sensor range is that of the player's own ships and planets, read from its sensor nodes
    (693 A): the game shows the player its allies' ships and what they scan, but neither they, an ally's kill nor a
    mole count, so a death seen only through an ally's sensors outside every system shows the normal explosion.

  "Visible" is `IsVisibleToPlayer`: on screen, zoomed in to system view or closer, and in the player's sensor range
  (`Ship_Update.cs:42`).
- **Planet crashes (178 B):** a ship that is about to crash on a planet does not roll for a derelict, so crash sites
  stay as common as before.
- **Debug keys (Gilad, 2026-10-15):** in debug mode, Ctrl+X (or Ctrl+Delete) destroys the selected ship as a drifting
  derelict with no roll, and does nothing to a ship that cannot become one; unseen, it leaves a drifting derelict with
  no model wherever an unseen drifting death does. Ctrl+Shift+X (or Ctrl+Shift+Delete) destroys it as a
  salvageable derelict, seen or not, and does nothing unless it would stop where a salvageable one can be (in a system,
  clear of radiation, salvage ops on) and the salvageable cap is not full.
- **Where:** the stop point decides, and it is known at death: a salvageable derelict slows linearly to a stop over
  10 s, so it stops 5 s worth of its drift speed from where it died. A stop inside a system makes a **salvageable**
  derelict, unless the hull's roll makes it drift. Two cases make a **drifting** derelict instead (125):
  - a stop outside every system;
  - a stop inside a star's radiation zone, or not more than 2000 beyond it, the danger margin the game already uses
    (`SolarSystem.InSafeDistanceFromRadiation`, 229), so salvage platforms never work inside radiation.

  Since 681 an unseen death that stops in a radiation zone or its margin leaves a drifting derelict too, and one that
  stops outside every system leaves one only as above (replacing the nothing of 125, 170 and 171).
- **How many:**
  - The universe keeps the list of derelicts (129).
  - Base chance 10%, times (1 - salvageable / the salvageable cap) (129, 142; 35% and 40 until 231). At the limit the
    chance is 0; a death that rolled just before it was reached leaves a drifting derelict if it was seen, and nothing
    if it was not.
  - Each cap is the galaxy's star systems times its Globals multiplier, `SalvageableDerelictCapMultiplier` and
    `DriftingDerelictCapMultiplier`, default 1 and used from 0.1 to 10, the cap rounded up and at least 1 (Gilad,
    2026-10-21; 676-679 and 686, replacing the fixed 100 of 142 and 188): about 23 in a tiny galaxy of normal stars
    with 3 opponents and 89 in a medium one with 7, as each major empire's home system comes on top of the count the
    new game screen shows. A multiplier over 10 is used as 10 and one under 0.1 as 0.1 (692 A), with a warning in the
    log as the game or the mod loads (`GlobalStats.LoadModInfo`), not as the mod lists read each mod's Globals.yaml.
    The cap is worked out from the systems' count wherever it is read, so it holds once the universe is made, after a
    load and in tests. A mod's Globals.yaml that still sets the old `MaxDriftingDerelicts` or `MaxSalvageableDerelicts`
    loads with a warning in the log.
  - At most the drifting cap of drifting derelicts at once (142, 20 until 188). It counts every derelict past
    salvage, drifting or sinking, with a model or not, but none rising for its ship (683 A, replacing 232, which
    counted only those with a model). Past it a visible death takes the place of the oldest drifting derelict with no
    model, and with none shows the normal explosion with no blast (170); an unseen one leaves nothing.
- **What it is:** a derelict is its own lightweight object, not a `Ship` (120).

### How a derelict moves and ends

- It drifts on its velocity at death, capped at the ship's top sublight speed (185), and slows to a full stop over
  10 s, drifting or salvageable (680, replacing 184, which kept a drifting one's speed). A drifting one sinks from the
  end of its breakup, as before, so it lasts about 105 s. It keeps an x/y/z tumble with smoke and has its
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
  - Its timer is 200 turns x ProductionPace (797 B, Gilad, 2026-10-23; it was 100).
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
  known to the player, and keeps it until it sinks away. With no model it still drifts and sinks; a salvageable one
  the player does not know of is gone once it starts to sink, as there is nothing to show, while a drifting one sinks
  on, so a player who comes upon it in time sees it (681). The player learns of a drifting one in a system as of a
  salvageable one there, as it forms (`Derelicts.LearnOfDrifting`, on the sim thread), when a ship of the player's
  enters the system or becomes the player's there (`Derelicts.Enter`), or once the player lists the system, as it
  then knows every derelict there (694 A). A drifting one never lists its system, brings
  no notification and cannot be selected. The two caps and the 100 s sink bound them,
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
      built, then Salvage Station Deployed (7164) in the player's colour (452 A), centered across the claimant line's
      width and split onto two lines where the language's text is wider; both carry the 7147 tooltip (454 A). The
      texts were to be Build Station, Station Ordered and Station Deployed, with "Salvage Station" for "Station"
      (453 A); the last is 7164 word for word, so it is reused. The panel has no abort: the derelicts window keeps its
      Abort Deployment. "No free site" shows above the panel's frame, clear of the derelict's name even wrapped to
      four lines (455 A). Codex 100093 names the button and the two statuses. The button's text runs past the
      168 px plate in Russian and Portuguese, as 7163 does on the derelicts window's button. No test covers the
      panel; it is checked by eye (456 B). Part 6 moves the button and the status 10 px up, for the techs under them
      (481; see Techs).
    - Salvage stations are queued as urgent orbitals, as research and mining stations are (320).
    - An order not yet built ends, with its constructor, once its system has no derelict left for its empire to
      salvage (322, 726 B); a built station ends once nothing is left to salvage there (718 A).
  - **The Exotic Systems rows (Gilad, 2026-10-17; 328 A, 332):**
    - Each system with salvageable derelicts the player knows of has a salvage row, apart from that system's research
      or Dyson swarm row. The row goes with the system's last such derelict, even with the player's station built there.
    - System: as other rows, with the derelict notification's yellow hourglass to the right of the hostiles warning.
      Star or Planet: how many derelicts ("3 Derelicts"). Resource: the production icon and the production left in
      them, in k or M from 10000. Richness and Owner stay empty.
    - The row's Salvage Ops button opens a window listing the system's derelicts (`SystemDerelictsScreen`). A button
      centered at the bottom of the window's frame, under a list 45 px shorter, orders the station (330 A, 345 C):
      Deploy Salvage Station, placed as the panel button places it, with 7146 as its tooltip; Abort Deployment (4473)
      in the military style once ordered, with no tooltip (347), which also ends an order stuck with no port; Salvage
      Station Deployed in the player's colour once built; and the 7148 grey text when no design can be built. The abort
      runs on the sim thread. Each derelict's row shows, beside its hull and name, a bar of its production left as on
      its panel, the pulsing flag of the empire salvaging it, with its name in the tooltip, and the Salvage button with
      the panel button's rules and tooltips (Gilad, 2026-10-17; 373 A); the Recover button joins it in sub-branch 5,
      recovery (465). Part 6 widens the window to 800 px and adds column titles, the derelict's techs and its
      salvage's status (479, 484, 492; see Techs).
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
    once the constructor launches; Recover comes with sub-branch 5, recovery.
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
      empire's goals included, but an order with a built platform passes first to the absorber or to the rebels, with
      its claim while its platform is still at work (296, 431 A; part 5; 502 A).
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
    takes longer at a slower pace, as building does. The Salvage Speed tech raises it (270, 516; sub-branch 8).
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
    - A station that fills stops its platforms: their loads wait aboard, and a finished one waits to come apart,
      until freighters haul its production away (sub-branch 6).
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
    - An absorbed empire's shares went to the absorber and a defeated one's were dropped (162; 432 A, 435 A); part 6
      removed the shares (457 A), since a load pays its tech as it reaches the station, so a passed order pays its new
      empire for the loads it sends from then on.
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
      `Empire.SetAsDefeated` before the goals are cleared, the rebels now created first), `Derelicts.PassShares` (gone
      in part 6), `Derelicts.ChangedHands`, and `LoyaltyChanges.ChangesTo`, which tells a passed order its platform is
      still changing hands.
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
  The AI picks it by the ship's strength against the designs it can build (743, 750-753 A).
- **What the derelict records:** its whole production and what is left of it, and its techs, picked at its death (see
  Techs). The per-empire shares of 123, 133 and 287 went in part 6 (457 A): a lost claim lets another empire finish
  the derelict, and its loads pay by their share of the whole production, so the next empire earns only for the rest.
- **Production:**
  - The station keeps the stripped production and freighters haul it to colonies (132 B).
  - The derelict's total production is half the share of health the ship had left at its death, times its design
    cost before any empire's cost bonus, at the game's production pace (`ShipDesign_Stats.cs`
    `GetCostBeforeBonuses`). It is stored at death (160, 239): a ship that cost 1000 and died with 40% of its health
    leaves 200 to salvage, 17 turns of stripping at the default speed. Every salvageable derelict leaves at least 50,
    times the production pace (`Derelict.MinSalvageProduction`; Gilad, 2026-10-17; 355), as the Codex derelict
    passage of 100093 says.
  - When it is all stripped, the hull sinks away (396 B).
- **Techs (Gilad, 2026-10-19; 457-471, replacing the per-tech roll and the shares of 159 and 287):**
  - A salvageable derelict holds a few techs, picked at random at its death and saved with it, the same for every
    empire: of the techs that unlock its design's modules and its hull, root techs left out (458 A, the hull's tech
    included as 467 asked; 470 A), 2 for a frigate, 3 for a destroyer, 4 for a cruiser, 5 for a battleship, 6 for a
    capital and 3 for an untethered station, or all of them when the design has fewer (460 B). It reads the modules
    and not the design's `TechsNeeded`, which holds every ancestor of those techs and, for a design saved during a
    game, only its hull's techs until the next load.
  - Each load a platform sends to its station, in the step it leaves whatever its shuttles show, advances each of
    those techs the empire has not researched and sees on its tree by 20% of what the tech has left to research, times
    the load's share of the derelict's whole production (459). What is left is measured at the empire's own cost, race
    modifiers included, as research measures it, and a partial load pays only for what the station took. A
    multi-level tech is paid only before its first level.
  - Every empire gets the same 20% (472 A, replacing 468 A): `ModHpModifier` is the empire's whole module health
    bonus, which Durability and Nanoweave research, artifacts, Remnant gifts and the AI's difficulty bonus raise as
    well as the Engineers trait's 0.2, so a late-game empire near +2.1 would have been paid about 62% of what is left.
  - The 20% is the Globals knob `SalvageTechShare`, default 0.2 (469 A). It is not clamped (473 B): at 1 or more a
    derelict stripped in one load fills a tech, which the next research turn completes, and below 0 it takes research
    away, as its comment in Globals.yaml warns.
  - Salvage never completes a tech: each load takes 20% of what is left, so the returns shrink over loads and wrecks
    and the empire's own research finishes it (459, 462). A tech whose parents are unresearched is paid as any other.
    Example (Gilad's): a tech of 1000 with 1000 left and a wreck of 300 production; a load of 50 pays 20% of 1000 x
    50/300 = 33.3, and with 600 left by the next load of 50, 20. A whole wreck gives a fresh tech a little under 20%
    of its cost: about 18.5% in loads of 75.
  - Skipped: techs hidden from the empire's race (`TechEntry.IsHidden`) and secret ones it has not discovered (463 A).
    A Remnant wreck holds none (127). The wreck of another empire that researched Remnant tech can hold those techs,
    and an empire that has discovered them gains on them (475 A); the Remnant techs are secret, so others are skipped.
  - A load lost with its platform pays nothing. The order keeps its derelict's techs and whole production, saved with
    it, since a derelict can sink away before its last load leaves while a full station holds it back; a passed order
    keeps them too. A tech researched meanwhile is paid no more.
  - No roll: salvage never unlocks a hull (461), nor a module without its tech (the module route was dropped: a
    design's techs, not its modules, decide what it can build, and the AI and mergers go by techs too). A recovered
    ship never unlocks its hull either; scrapping it runs the reverse-engineering roll (471, 514 A).
  - Since salvage completes nothing, there is no completion to tell (466); part 7's platform notification lists the
    points each tech gained. Each tech's node in the research tree already shows its progress and the cost left.
  - **On the panel and in the derelicts window (Gilad, 2026-10-19; 464, 478-484, after three preview sheets):**
    - A research icon under the panel's turns icon always shows a count (478 B): every tech the derelict holds until
      the player sees its techs, then only those the player can learn, so it drops as the player researches them. Its
      tooltips (7180, 7181) say which, without naming the techs.
    - The player sees a derelict's techs once a salvage station of the player's is built in its system, and from then
      on, even after the station is lost (483 B). Each turn `Derelicts.UpdateTurn` marks a salvageable derelict for
      every empire with a station built in its system, in `Derelict.TechsShown`, saved by empire as `KnownBy` is, and
      `SalvageDerelict.SeesTechs` adds the station standing now, so a station shows them the moment it is built. A
      derelict that comes to rest after the station is lost hides its techs until another is built. The turn looks up
      each empire's built stations once, in one pass over its goals (`SalvageOps.StationSystems`; 490).
    - Once seen, the icons of the techs the player can learn sit under the station order, 30x25 and 34 apart, on the
      research notification's background, each over a 4 px bar of how far the player has researched it (478 B). The
      Build Salvage Station button and the station's status moved 10 px up to make room (481). While the player's
      salvage of the derelict stands, each icon has under it the research points that order's loads have given the
      tech (481, the third picture), whole below 10,000, then in thousands with one decimal, and in whole thousands
      from 100,000 (+990, +12.5k, +100k), so six fit side by side in the game's Arial8Bold.
      `SalvageDerelict.Gained` keeps them by tech, saved, and an order passed on with its platform starts again from
      none (482 A). A tooltip over an icon gives the tech's name, how far it is researched (7182) and the points
      (7183). No floating text when a load pays (480 A).
    - The derelicts window is 800 px wide, from 400, with column titles in wheat at the rows' size (Arial12Bold;
      7127 Derelict, 7184 Salvager, 1406 Technologies, 7185 Status; 492 B, after a sheet of 760 and 800 px and a
      smaller title font) over the hull and name with the production bar, the salvager's pulsing flag in a 70 px
      column, the techs (the count until the player sees them, then the icons and bars 32 apart, with the panel's
      tooltips) and the status (479): Platform ordered (7186) while a unit waits to launch the constructor, Building
      platform (7187) while the
      constructor flies out and builds, then Salvaging (7188), in wheat for the player's salvage and in the salvaging
      empire's colour for another's (484). A status too wide for its column, which ends 6 px short of the Salvage
      button, takes the smaller font, and two lines of it where even that is too wide (486); the panel's station
      status fits the same way (`FittedStatus`), so a two-line status stays clear of the techs under it (487).
    - The research tree's icon lookup moved to `Technology.Icon`, which the panel and the window share.
    - Review (485-491): the Codex window sentence names the icons of the techs you can learn (485), the statuses fit
      their space (486, 487), 7180 says only those are counted, as the window shows icons in place of the count (488),
      the Salvager header reads Империя, Імперія and Império (489), the turn's station lookup (490), and
      `TechEntry.TechCost` stamps its cache after the cost, as the panel now reads it from the UI thread (491).
    - Open: the Derelict title (7127) and the panel's first tooltips (7128-7130) are still English only.
  - As built (part 6): `Derelict.Techs`, `TechsHeld`, `TechsOf` and the pick; `SalvageDerelict.Techs`,
    `DerelictProduction`, `AdvanceTechs` and `CanLearn`; the shares and `Derelicts.PassShares` removed, and
    `Derelicts.Strip` takes no empire. Codex 100093 gains the rule, and the salvage unit's description (7143) strips
    for production and technology in all four languages, as the Salvage button's tooltip (7166) does (474 A). Saves
    made on this branch before part 6 load with no techs on their wrecks, with no fallback.
  - Review (472-475): the in-house review found the module health bonus behind the Engineers scaling (472 A), the
    unclamped knob (473 B), 7143 (474 A) and the Remnant wording (475 A); a test now covers a partial load.
- **AI restraint:** the AI builds no salvage station in a system where a rival it is neither at war with nor holds
  open borders or an alliance with has a colony (138, 760 A); a station it already has there orders on.
- **Trust (Gilad, 2026-10-17; 152, 161, 350-354):** when a salvage station is built, each AI empire with a colony in
  its system that knows the builder (350 A), and has no open borders treaty, no alliance and no war with it, trusts
  the builder less, once per station. The amount is `DTrait.SalvageStationTrustLoss`, 5 for every personality, as
  `DiplomaticTraits.xml` sets none (351 B). Against the player it is multiplied by the difficulty's
  `DifficultyModifiers.SalvageStationTrustLossVsPlayer` (352), 1 at Normal, 1.25, 1.5 and 1.75 at Insane, the scale of
  `AngerMultiplierVsPlayer` (354). The player is not told (353 A); a one-time diplomacy warning, like the
  colonisation warnings, is logged for later (353 B). It is an AI reaction, so the Codex says nothing of it.
- **Placement:** a station cannot be placed outside a system (163).
- **Empires that leave:** an absorbed empire's platforms pass with their orders, and a defeated one's go to its rebels
  (162, 296; built in part 5, 429 A, 431 A); the shares passed with them until part 6 removed them (457 A).
- **Notifications (151, 294, 299-301):** each kind has a switch in a new alerts tab of the Automation window, which
  takes every alert checkbox out of the window's one tall table (279, 289):
  - a system joining your list of systems with salvageable derelicts (see Knowledge), wherever it is (260), told at
    once on the sim thread, with no per-step check (256 as reworked by 257):
    - it names the derelict that brought the system onto your list, whoever's ship it was, your own included (245);
    - its icon is the hull's icon with the yellow hourglass over it at 0.8 of its size (246);
    - a click takes the camera to where the derelict comes to rest and selects it; once it can no longer be salvaged,
      the click shows the system (244);
    - it never pauses the game (253), and has its own switch, Derelict Alerts (7189, 7190), for players who find it
      a nuisance (299, reversing 254);
  - a station built: research, mining and salvage stations share one switch (300);
  - a salvage's end, under one switch, Salvage Alerts (7191, 7192; 301), none of them pausing the game (Gilad,
    2026-10-19; 493-499, after a preview sheet of in-game renders):
    - a platform that has sent its station its last load and starts to come apart, its derelict stripped empty or its
      order called off (493 B): the derelict's hull icon with the research symbol over it at 0.8 (495 A), a header
      saying which (7193, 7194), the production its loads brought (7195), then a line for each tech, its name and
      then its points, whole with thousands separators (496, layout 3), leaving out a tech whose points show as 0; a
      click shows the system, as the derelict has sunk away;
    - a salvage that ends early (494): its platform destroyed while at work or sending its last loads (7196, the
      platform's icon), a rival's constructor starting to build first (7197, the hull icon under the rival's flag),
      its station lost, told once for each order there (7198, the station's icon, or the derelict's when the order
      has not found its station since a load), and the derelict sinking away before the claim (7199, the hull icon
      with the hourglass). A click selects the derelict while it can still be salvaged, or shows the system, and
      the one for a sunk derelict shows the system (498);
    - not told: a constructor lost on its way, an order called off before its platform is built, a platform lost
      while it comes apart, salvage ops turned off, an order giving way to a platform passed on to its empire,
      another empire's salvage, a station the player scrapped or scuttled, which leaves its scuttle timer set (500 A),
      and an order beyond the station's live units once a salvage unit is lost, which is rare (501 A);
    - the order keeps the production its loads brought (`SalvageDerelict.Delivered`, which starts again from none
      on a passed order, as the points do) and its derelict's design name and hull icon (`DerelictName`,
      `DerelictIcon`), all saved, as the derelict may have sunk away before the last load;
    - the text is white, since a notification's text is red only when it pauses the game (497);
    - review (500, 501): the in-house review found a scrapped station told as lost once for each order there
      (500 A, silent now) and the silent end of an order beyond the live units (501 A, kept silent);
  - both new switches sit under Station Built Alerts, ticked by default and shown only with salvage ops on (499),
    saved as `UniverseParams.DisableDerelictWarning` and `DisableSalvageWarning`. Codex 100173 describes both boxes
    and 100093 the notifications;
  - the result of each of your recovery rolls, under Salvage Alerts (513 A); a recovery called off is not told, even
    after an earlier salvage brought production (628 A). As built in part 5 (Gilad, 2026-10-21; 623-635, after a
    preview sheet of in-game renders in BlackBoxPlus-references\previews\s5p5):
    - the success is told as the ship appears, from the derelict, so it comes even with the platform or the station
      lost during the rise and never when no ship could be made, to the player whenever the ship is the player's
      (623 A): "{0}: the {1} is recovered" (7215), the hull icon with the crossed hammers in green, a click
      selecting the ship, or once it is gone showing the system (624 A), with the colonized chime (626 A);
    - a plain failure is told at the roll, "your recovery of the {1} failed, and it broke apart" (7216), the hammers
      in yellow, with the encounter sound; a catastrophe at the roll too, "the {1} exploded as your platform worked to
      recover it, destroying the platform" (7217), the hammers in red, with the warning sound, and no notification of
      the platform's loss; a click on either shows the system (625 A, 626 A, 627: a C, b B, c C);
    - none pauses the game; the hammers are the Recover button's, at 0.8 of the symbol as the hourglass and the
      research symbol;
    - not told: the production and research an earlier salvage brought (629 A), the platform or the station lost once
      the roll has succeeded (630 A), a ship that could not be made, which the log keeps (634 A), and a rival's ship;
    - 7192, the Salvage Alerts tooltip, and Codex 100173 name the recovery's outcomes, and 100093 tells of them.
- **Pirates (176):** pirates salvage too, in *recover* mode only: they take ships, never production or techs. This
  is for now. How they reach a derelict is settled in the ai sub-branch; later they may raid a salvage station to
  capture it, and recover the derelicts in its system with it. A salvage station pirates board already stays theirs
  (358 B), but does nothing for them until then.

### Recovery

Gilad, 2026-10-20; 513-536, replacing 144 and the work and chances of 288 and 145:

- **Ships only;** a station derelict can never come back as a ship (131). Remnant ships can be recovered (127), and so
  can the empire's own lost ships.
- **The order (288, 518 A):** a recovery is a saved mode of the salvage order, so the claim, passing on, call-offs and
  the notifications work as they do for salvage. The Recover button sends a platform as the Salvage button does, so
  only the claiming empire works on it, and its second click calls the recovery off.
- **Switching (520, 533 A, 558):** the same platform switches between the modes, at any time; the stop once half the
  production was stripped (144) is gone. Recover on a derelict the platform strips sends what it holds to the station
  first, then starts the break-in the next turn. Salvage during a recovery turns the platform to stripping and keeps the
  work done, which a later Recover takes up again after a new break-in, at a chance lowered by what was stripped
  meanwhile.
- **The work (Gilad, 2026-10-20; 519, 530 A, 532 A, 536 A, 554, 556 A, 557 B, 558 B, 559; 554 replaces the design
  cost of 519 and the 36 a turn of 531 B):** time only, nothing is paid.
  - The platform first breaks into the derelict for `SalvageRecoveryBreakInTurns` (Globals, default 10, at least 0)
    turns, whatever the ship's size and the production pace (559); the Salvage Speed tech does not shorten it. Only
    turns of work count: a turn spent sending the hold does not. Turning the order to salvage starts the break-in
    again for a later recovery, and passing the order on keeps it.
  - The work is the share of health the ship had lost at its death, times its slots (the design's surface area), times
    the production pace, times `SalvageRecoveryWorkFactor` (Globals, default 1.1), times 1 minus the race's Engineers
    value (below). The derelict saves its health at death for it.
  - The platform works it at twice `SalvageSpeed` a turn, 24 by default, not scaled by the pace, so a slower pace
    takes longer, as salvage does (557 B). The Salvage Speed tech (sub-branch 8) raises both.
  - A cruiser of 310 slots that died with 40% of its health needs 205, 9 turns after its 10 of break-in; the median
    vanilla designs take 13 turns for a frigate, 19 for a cruiser, 30 for a battleship and 54 for a capital in all,
    and the Behemoth's 4096 slots 123.
  - Losing the station or the platform, or calling the recovery off, loses the work (522 A); passing the order on
    keeps it (548).
- **The chances (145, 173, 515 C, 520, 528 A, 529 A):**
  - base chance by hull role, as reverse engineering's: frigate 60, destroyer 50 (the table's default), cruiser 40,
    battleship 30, capital 20;
  - times 1 plus the race's Engineers value, +0.2 for Skilled Engineers and -0.2 for Haphazard ones, read from the
    race traits the empire was made with, which saves already keep (`EmpireData.SelectedTraitSet`; Gilad, 2026-10-20;
    544 B, replacing a value saved at the empire's start). `ModHpModifier` holds research, artifacts, Remnant gifts
    and the AI's difficulty bonus too (472), and none of them count. Rebels keep the traits of the race they rise
    from, saves from before salvage ops read them as well, and an Assimilator keeps its own Engineers value, as
    conquering changes no trait list (528 A dropped). `Empire.RaceEngineers` reads it once, and again only when the
    traits change. A mod race that sets the module health bonus in its race file rather than through a trait gets
    none;
  - times the share of the derelict's production not yet stripped, by any empire: 0.8 with 20 of 100 stripped;
  - on a failed roll, a second roll decides how bad it is: 10% times 1 minus the Engineers value is catastrophic, 8%
    for Skilled Engineers and 12% for Haphazard ones;
  - the roll comes the same turn as the last of the work, as the bar reaches 100% (593, Gilad, in place of 571 A's
    turn after it), at the chance of that moment; a recovery with no work to do rolls the turn after its break-in;
  - both rolls use the empire's luck, draws from 0 to 1 that are its own for the derelict: each empire picks a luck
    seed as it is made, at the game's start or as rebels rise, saved with it, and `Empire.Luck(key, index)` mixes the
    seed, the empire's id, the derelict's id and which draw, so loading a save never changes the outcome while the
    chance stays the same, and one empire's outcome tells nothing of another's; empires in saves from before have no
    seed and still draw apart by their ids; a draw passes under its percent, as a dice roll does (Gilad, 2026-10-21:
    618 B, as the empire's random, seeded from the clock and not saved, rolled anew after each load; 619 A, as one
    draw for the derelict told the player a rival's outcome after a reload; 620 A, the seed the empire's rather than
    the derelict's, for other rolls to use later); the Codex says so;
  - a plain failure breaks the derelict apart as a drifting one breaks up, 3 to 5 seconds of flashes and lightning
    on its hull but none of the small blasts, which are left to a catastrophe (585 B), with one ShipSlow sound at half
    the break-up's volume as it gives way where the player sees it (583, 584 B), and it sinks away at a stripped
    derelict's speed; the Codex says it breaks up without explosions (592 A). Nothing
    is left to strip; the order ends, and the platform takes itself apart over two turns as after a salvage, freeing
    its unit (517 A);
  - a catastrophic one makes it explode with its hull role's biggest blast, the death blast's cap
    (`Ship.ExplosionDamageCap`: 40 times its radius for a frigate, 60 for a cruiser, 80 for a battleship, 100 for
    a capital), which catches the ships near it; the ship's explosion and sound play where the player sees it, the
    blast hits seen or not, and the derelict is gone at once. The platform beside it is always destroyed, whatever
    a platform's usual chance to dodge a blast (572 A). Nobody is blamed: no kill credit and no relations change
    with the empires whose ships it hits (573 A). The blast has no source, as other sourceless damage: armor's
    resistance to explosions does not lessen it, the modules it destroys do not blow up, and it clears the last
    attacker of each ship it hits, so a ship it kills is nobody's kill (577 B). The platform dies as any ship does,
    its own small death blast its owner's (578 A). One notification tells of it (133, 155, 526 A, 527 A; part 5);
  - a success brings the derelict up from its rest depth to a ship's depth, evenly over 3 seconds (576 C), with the
    Afterburner sound where the player sees it (586 H), running on past the rise and fading out over its last 2
    seconds to end at 6 (587 C, a file of its own, 589 A), and the ship takes its place as it reaches that depth (595 A;
    see The recovered ship).
- **On the panel and in the derelicts window (523, 524, 534 A, 535 A, 549-553):** during a recovery the tech icons
  give way to a yellow bar of the work done, with its percent rounded down, on the panel and in the window's
  Technologies column, from the order on, at 0% until the work starts; the tech count stays. On the platform's own
  panel the bar takes the place of the derelict's production bar, and the platform's hold bar turns from yellow to
  brown, as the colony screen's stored production (555 A: the constructor's construction bar stays yellow). The bar's
  tooltip gives the work done and in all, rounded, the speed, the break-in's turns and the turns left in all (560 A).
  The window's status and the platform's read Breaking in (turns left: N), then Recovering, and the platform's names
  the ship (564 A); a rival's recovery reads Recovering in its colour from its claim on, its platform's status too,
  with no bar and its break-in unshown (561 A, 562 A), and the claim flag's tooltip says the empire recovers the
  derelict (7210, 563 A). The chance shows in the Recover button's
  tooltip and on the panel's left side below the hourglass and the tech count, before any order, not in the status
  (551).
- **The visual (525 B, 547, 554):** the platform's shuttles fly each turn as in salvage, one for every 6 slots of the
  turn's work, 4 at 24, and a full turn's during the break-in, but none on the turn that finishes the work, whose roll's
  outcome shows at once with no new effect (594 A); in place of salvage's flashes and junk the derelict
  shows a station build's effect (`ConstructionShip.AddEffects`: sparks, lightning, flashes and flying parts) at the
  recovery's progress, spreading out from its center to half its radius as the progress rises, so the break-in shows
  a tight knot of sparks at its center (580 A), from planet view or closer, where the shuttles show (581 B). Each
  break-in turn plays the shield static once, quietly, where the player sees the effect (583). The roll ends the
  effect: its parts rise with the derelict and shrink away over its rise on a success (590 A), fade out as a
  finished build's on a plain failure, and go at once in a blast. The effect sits on the hull's surface facing the
  camera, as the derelict lies, its top or, upside down, its back (588 A), as its smoke does, since particles inside
  the hull are hidden; so do a derelict's break-up flashes, lightning and blasts, at its death too, and its strip
  flashes (582 A).
- **Steadied for the work (Gilad, 2026-10-20; 570, 574, part 3):** over the break-in's turns the derelict's tumbling
  slows evenly, smoothly between turns, until it stops, and it turns level, its pitch and roll back to a ship's at
  rest and its heading kept, so it is ready to fly if recovered; its smoke goes on. Once still it stays still, also
  after a turn to salvage or a call-off; a break-in cut short keeps the steadying it reached, and a break-in of 0
  turns steadies it at once. The steadying is saved with the derelict. It stays at its rest depth until a successful
  roll (576 C).
- **No hull unlock (471, 514 A):** recovery never unlocks the ship's hull. Scrapping the recovered ship at a shipyard
  runs the game's reverse-engineering roll, or advances the hull's tech by 25% when it is not researched.
- **The recovered ship (126, 134, 146, 147, 158, 164, 575; Gilad, 2026-10-20, 595-603):** it appears where the derelict
  rests, facing the same way, at a ship's normal depth, which the derelict rose to after the roll (576 C), not its
  sunken one, as the rise ends 3 seconds after the roll: it takes the derelict's place in the same frame, its model
  and all, and the platform comes apart as after a salvage from the next turn, without the salvage notification (595 A).
  - repair modules, external armour and engines are dead. A repair module is any module with a repair value except
    a command module: Main Engineering, repair-drone bays, Construction Array and the like (174). `RepairRate`
    sums every module with a repair value, command modules included, and a dead one still gives a tenth of its repair,
    as in any ship (`ShipStats`). Main Engineering, a reactor too, dies as well, so a design whose only reactor it is
    (vanilla's Dodaving mk1-d, mk1-e, mk2-a, mk2-b and mk3) comes back without power, dark, its command modules
    repairing at a tenth until Main Engineering works again (605 B);
  - an engine is a module of the Engine type, `Is(ShipModuleType.Engine)`, vanilla's 18 (600 A); armour is
    `Is(ShipModuleType.Armor)`, and external armour is armour on the hull's outer edge as designed, the game's
    external flag at full health, so the armour the dead edge exposes stays at 2% (601 A);
  - armour in internal slots is at 2% health;
  - every other module is at 4% health, command modules included;
  - a module that comes back alive keeps at least 1 health, below which the game kills a module a second later, so
    a small one in a mod, such as a Combined Arms conduit of 20, comes back at 1 (604 A);
  - the modules die quietly, with no blasts, debris or sounds and no death check as the ship appears, so a design
    whose dead modules take half its internal slots comes back below the game's 50% line, and its next module loss
    destroys it; the next module loss may destroy it, and that is accepted;
  - it comes back at crew level 0, keeps its name and joins no fleet, with no ordnance, no troops and empty hangars,
    which its bays and supply refill as usual; its shields and stored power are empty, recharging as usual (603 A).
    The derelict saves the ship's name at its death for it;
  - it appears dark, and its lights flicker on over 2 seconds as a refinery's do once it has power for a second, as
    any ship's after its power fails, about the time the success sound fades (596 C);
  - every module is below the game's 15% fire line, so it burns and smokes all over until repair takes each past 15%,
    and the derelict's smoke ends as it appears;
  - the empire whose ship it was is not told and does not mind.
- **During the rise (597 B, 598 A, 599 A):** the derelict can no longer be salvaged and leaves its system's list, but
  stays selected, its panel and its row in the derelicts window showing the recovery's full bar with no buttons, so
  nothing switches or calls off a recovery whose roll succeeded; its tooltip shows no turns left. If the derelict is
  still selected as the ship appears, the ship is selected in its place when it is the player's, and a rival's
  derelict is deselected (606 A). Once the roll succeeds the ship is the
  empire's: it comes even if the platform or the station is lost during the rise, passing on with an order passed
  on then, and an empire gone by then gets nothing, the derelict sinking away as one whose turns ran out.
- **How it gets home:**
  - Command modules carry repair (vanilla: Bridge 500, CIC 1200, Cockpit 200) and work at full rate at 4% while
    powered, a tenth without, so repair brings the engines back.
  - The recovery orders the trip home, to the nearest planet that repairs, for the player and the AI alike (175):
    the nearest of the empire's planets with ship repair, a safe one first, as a damaged ship's resupply picks the
    nearest safe planet (602 A); in vanilla every colony repairs (an Outpost 20, a Capital or Space Port 40, more with
    the colony's level). With none it waits where it is. The resupply logic would not send it on its own:
    `ResupplyNeededLowHealth` (`ShipResupply.cs:141`) reads the share of live internal slots, not their health, and
    every internal module is alive at 2-4%.
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
  - the Salvage Speed tech is hidden, by its `RequiresSalvageOps` flag (808 A).
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
- **Old saves and incomplete mods (Gilad, 2026-10-23; 900-904):** a game saved before salvage ops existed, vanilla or
  a mod's, loads with them off: every save now marks its rule options as saved with salvage ops, and one without the
  mark ticks Disable Salvage Ops (900; saved setups are YAML and untouched; this branch's own earlier saves load with
  them off too). So an old save never meets an empire with no default salvage station. A mod that turns the flag on
  without a Salvage Platform design or any salvage module has salvage ops off, with a warning, and still loads;
  vanilla is not checked (901 A). The check for a platform missing modules stays as a guard, though such a design is
  dropped at load (903 B). The Codex says nothing of old saves (902 B). Found on the way: Disable Pirates and Disable
  Remnant Story, unticked against a mod's Globals that tick them, came back ticked on load, a save leaving out the
  unticked value; they are always written now (904 B).
- **Recovery chance and the station pick (Gilad, 2026-10-24; 910-913):** a Remnant ship's recovery chance is halved
  and a ship that was the recovering empire's own gets a quarter more, for every empire; the player's chance is also
  multiplied by 1, 0.75, 0.5 or 0.25 at Normal, Hard, Brutal or Insane, the AI's never (a difficulty modifier). A
  salvage station is usable only with cargo room for a platform load for each of its salvage units, for the pickers,
  the Automation window's list and the race default's check at load; the ship designer flags one red under that,
  yellow under one and a half loads a unit and green under two, the red remedy saying the pickers skip it. The pick
  for a system counts the derelicts the empire would work there, the planner's own test for the AI and the player
  alike (known, salvageable, a fifth of their turns left, not worked from another empire's station there; 911 A), and
  takes the most salvage units up to that count, then the fewest beyond it, then the most cargo room, then the
  strongest per slot; with no system in mind, the most units as before. The planner, the derelict panel and window
  and an order building its station each pick for their system. The design issue's red and yellow-or-green remedies
  are 7231 and 7232 (912 A); the chance tooltip and the Codex name the three factors in words (913 A). These ride in
  PR #472 on feature/salvage-ops/base (910 A).
- **Pirates keep Remnant ships (Gilad, 2026-10-24; 914):** a pirate base never takes in a ship of a Remnant design, matched by
  the design's style against the Remnant race's ship type, as the design cannot be renamed and a ship's vanity name
  can; it guards the base instead, so a Remnant ship another empire recovered, lost again and pirates recovered stays
  with them too. The recovery chance's half for a Remnant ship follows the design too, whoever lost it, and a
  Remnant ship of an empire's own gets both its half and its quarter more (915 A).

### Phases and branches

- **Phase 1** is visual only (128): every derelict is a drifting one, which keeps its speed and sinks away after its
  breakup, and nothing is saved. The roll still covers every eligible death, so a rolled death never blasts; an
  unseen one leaves nothing in phase 1 (170).
- **Phase 2** adds persistence, salvage, recovery and the AI. Recovery was phase 3 until Gilad moved it ahead of
  hauling and the AI (2026-10-19; 465): it needs neither, and the AI's strip-or-recover choice then has both sides
  in place.
- Each sub-branch merges into `feature/salvage-ops/base`, and the patch branch of the day is merged into `base` as
  fixes land.
- **Shipping (190, replaces the single final PR of 140 and 154):** phase 1 merged into `jupiter-1.60/fixes_49` with a
  merge commit and shipped in that patch batch, through PR #445 to `main` with the usual gates. Later phases go the
  same way, each into the patch branch of its day: phase 2 once sub-branches 2-8 are all in `base`, recovery
  included (465), since sub-branch 2 alone leaves derelicts that nobody can salvage.
- **Drifting derelicts and the chance (189 A):** only salvageable derelicts lower the chance (129, 142); drifting ones
  exist only for deaths the player saw, so counting them would let the camera change the blast odds (170). They have
  the drifting cap alone.

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
     the 6 left (225, 230). Since 476 A (Gilad, 2026-10-19) the derelict saves itself with `[StarData]` on its own
     fields, as a ship does, instead of through a `Derelict.SaveData` copy: a `[StarDataSerialize]` hook writes the
     smoke points, and a `[StarDataDeserialized]` hook swaps in the loaded design, sets the mesh offset and the
     universe, and with salvage ops off runs its turns out. The save stops the sim's turns but not its queued
     actions, so it reads the live derelict, and a click queued then can land halfway through it (505 A); the list
     is copied under its lock;
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
   - the tech payout per load (457-471, replacing the check of 159 and 287), and the techs on the panel and in the
     derelicts window, with the window's status column (464, 478-484);
   - the platform notifications and their switch, and the switch for the derelict notification (294, 299, 301;
     493-499, part 7).
   - the review before the merge into base (Gilad, 2026-10-19; 502-512): the Codex adversarial review and three
     in-house reviewers, one each for the order lifecycle, the work and saves, and the UI and texts, found no blocker.
     Fixed:
     - an absorbed empire's called-off order no longer carries over a claim that belongs to an order of that empire's
       not yet built, which ends; the absorber kept a claim with no live order, the derelict's timer stopped for
       good (502 A; reachable once AIs salvage);
     - a constructor taken by another empire on its way home, after its order ended, kept its salvage unit busy
       while it lived; the next order looking for a free bay now lets it go, as an order lets go of its own
       constructor boarded on its way out (503 A);
     - a platform at work takes back its derelict's claim each turn, stripping or waiting to send its load, should a
       call-off that a save caught halfway have dropped it (505 A);
     - hull shuttles no longer launch from a platform destroyed during their launch delay, as 422 A did for loads,
       and those already out still fly back (506 A);
     - `Ship.CargoRoom` reads its cargo container once, as the UI reads it every frame (508);
     - the Codex counts of a wreck's techs are maxima, and scuttling a station tells nothing too (509); the second
       click's tooltip (7167) holds for an order with no platform yet (510 A); and two test nits (511).
     Logged, not fixed:
     - a mod that turns salvage on with a Salvage Platform design that does not load throws on the Salvage click;
       Gilad would rather stop the game at load with an assertion (504 B);
     - a long design name in the derelicts window runs under the salvager's flag (507 B);
     - `OnPlatformBuilt` writes the station's ordnance in space from a parallel AI worker, as a miner's return does,
       so two platforms finishing in the same step can lose one update (512);
     - the derelicts window and the panel scan the empire's goals each frame for each row, with 366's per-frame
       work (512);
     - the salvager tooltips (`DerelictSalvagedBy`) are raw strings with no Codex link, as `DerelictOriginalOwner`
       is (512).
5. **recovery** (was 7 and phase 3; Gilad, 2026-10-19; 465), with its rules settled in 513-536 (see Recovery), in
   parts (518):
   1. the Recover button and the recovery mode of the salvage order, the switch between the modes, the race's
      Engineers value and the chance on the panel;
   2. the work and its progress bar, with the build effect;
   3. the roll, the break-up and the catastrophic blast, and the derelict steadied during the break-in (570); parts 3
      and 4 are built back to back, as separate commits, and play-tested once both are in (569 A);
   4. the recovered ship: its damage, its saved design and the order home (175);
   5. the notification, and what is left of the Codex; each part writes the Codex text of what it builds.

   Part 1 (Gilad, 2026-10-20; 537-546, after a preview sheet and in-game renders in BlackBoxPlus-references\previews\s5p1):
   - the Recover button sits under Salvage in the drawer, with the crossed hammers of the construction map symbol
     (537 C), and beside it at the end of each row of the derelicts window, which widens to 840 px (539 B);
   - both buttons are grey until clicked, a clickable one lights up under the cursor, the button of the standing
     order's work stays lit, a click on it calls the order off and a click on the other turns the order to that work;
     a button that cannot order looks as grey and does not light up (540 A). With no salvage station of the player's
     built in the derelict's system there are no buttons and no drawer, unless an order or claim of the player's
     stands, which they give up;
   - the chance shows on the panel under the hourglass and the tech count, with the crossed hammers (538 B), for any
     ship derelict, station built or not (541 A), and in the Recover button's tooltip;
   - a recovering platform strips nothing and first sends what it holds; the work comes with part 2;
   - texts 7200-7205; 7168, the no-station tooltip, goes, as no button shows then. Codex 100093 gains Recovering a
     derelict;
   - review (542-546): the Engineers value comes from the race's traits (544 B), which makes the rebels' value
     (542) and old saves right; the Codex places the window's button (543); a claim kept without its order shows the
     call-off tooltip on both buttons (545 A); the buttons read the derelict's block and order once a frame (546 A).
     Logged for later parts: the texts already tell of the break-up, the blast and the work kept or lost, a called-off
     recovery still sends the salvage notification (part 5), and a recovering platform's status reads Salvaging
     (part 2, done).

   Part 2 (Gilad, 2026-10-20; 547-568, after a preview sheet of in-game renders in
   BlackBoxPlus-references\previews\s5p2):
   - the derelict saves its health at death; the order saves the work done and the break-in's turns, and passes both
     on (548); the platform breaks in, then works at twice the salvage speed until all the work is done, and holds
     there until part 3's roll;
   - the slots replace the design cost after a table of vanilla turns by hull role (554, 556 A, 557 B, 558 B), and
     `SalvageRecoverySpeed` gives way to twice `SalvageSpeed`, with `SalvageRecoveryBreakInTurns` in Globals, not
     scaled by the pace (559);
   - the yellow bar on the panel, in the window and on the platform's panel, the platform's hold bar brown, the
     statuses Breaking in (turns) and Recovering, a rival's Recovering in its colour (549-553, 555 A);
   - the station build's effect over the derelict, with its parts kept as the platform's, and the shuttles one per 6
     slots of work (547, 554);
   - texts 7206-7210, and 7202 and 7203 reworded for the break-in; Codex 100093 gains the work, the break-in, the bar
     and the statuses;
   - review (560-568): the tooltip rounds the work (560 A); a rival's recovery reads Recovering from its claim on and
     on its platform's panel, its break-in unshown (561 A, 562 A), with its own claim tooltip (563 A); the turns left
     follow a colon, so no plural after 1 (564 A); the knobs' ranges in the Globals comments, the break-in floored at
     0 and the work at nothing (565 A); the build effect runs out as the strip effects do (566 B); 7202 tells of the
     break-in (567 A); the mode turns before the break-in resets, and the salvage files spell center (568 A).

   Part 3 (Gilad, 2026-10-20; 569-579):
   - the roll comes the turn after all the work is done (571 A; later with the last of it, 593); a success brings the
     derelict up to a ship's depth over 3 seconds (576 C) and holds there, saved, until part 4;
   - a plain failure breaks the derelict up as a drifting one does, saved, and it leaves its system and sinks at a
     stripped one's speed; the order's work is over, and its platform takes itself apart without the salvage
     notification;
   - a catastrophe takes the derelict out at once, with a ship's death explosion where the player sees it and its
     hull role's death-blast cap through the death blast's own loop with no source (573 A); the platform is always
     destroyed (572 A) and the order ends untold until part 5. `Ship.ExplosionDamageCapPerRadius`,
     `ExplosionEffectScale` and `DieSoundEffect` and `SpatialManager.DerelictExplode` share the ship's death blast
     with a derelict. A destroyer hull falls to the death blast's default of 15 times its radius, as a ship's does;
     the rules' earlier "40 for a frigate or destroyer" read the table wrong;
   - the steadying over the break-in, saved (570, 574);
   - Codex 100093 gains the roll, the break-up, the blast and the steadying, and "How a ship dies" now says a
     derelict gives no blast as it forms;
   - review (577-579): the blast stays sourceless and clears the last attacker of the ships it hits (577 B); the
     platform's own death blast stays its owner's (578 A); the tooltip's turns left count the roll's turn (until
     593), the tumbling is a step the tests reach, a break-in of no turns is tested, and the ship-only overload of
     `IsStackedOnExplosion` is gone (579 A). Logged for part 4: a held success turned to salvage and back would show
     Breaking in for good, and a held success's tooltip shows a turn left for good, which part 4's spawn on the roll
     turn removes.

   Play-test fixes (Gilad, 2026-10-20; 580-592, after a listening page in BlackBoxPlus-references\previews\s5p2):
   - the recovery's effect was hidden inside the derelict's hull, at its center for the whole break-in, so only the
     shuttles showed each turn; it now sits on the hull's surface facing the camera, keeps spreading from the center
     with the progress (580 A), and shows from planet view, as the shuttles do (581 B); the break-up's and the strip's
     particles move onto the hull too (582 A);
   - sounds (583, 584 B): the shield static once each break-in turn where the player sees the effect
     (`sd_derelict_break_in`), and ShipSlow once, at half the break-up's volume, as a plain failure gives way where
     the player sees it (`sd_derelict_give_way`); the catastrophe keeps the ship's death sound, and a success plays
     the Afterburner sound once as it rises, where the player sees it (`sd_derelict_recovered`, 586 H), cut to 6
     seconds of its 23 with a 2-second fade (587 C);
   - a plain failure's break-up drops its small blasts and their sound, leaving flashes and lightning (585 B); a
     ship's death break-up keeps both;
   - after a play-test (Gilad, 2026-10-20): the break-in static at half the volume first set (0.4); the roll ends the
     work's effect, and the parts it left fade out as a finished build's on a plain failure, or go at once in a
     catastrophe's blast;
   - review (588-592): a hull keeps its back surface too, so the effect shows on an upside-down derelict, picked by
     which side faces the camera, at most 0.4 MB for vanilla's 125 hulls and 1.5 MB for Star Trek's 352, whatever
     the number of ships (588 A); the success's sound is a file of its own, the Afterburner's first 6 seconds with
     the fade in it, cut with the game's own NAudio, so nothing in the game times it (589 A); a success's parts rise
     with the derelict and shrink away over its 3-second rise rather than fade unseen under it (590 A); a test that
     the roll ends the work's effect (591 A); the Codex says a failed recovery breaks up without explosions (592 A);
   - the roll comes with the last of the work rather than the turn after it (593, Gilad, 2026-10-20), so the
     tooltip's turns left no longer add a turn for it, but for a recovery with no work left, and the Codex says so;
     that turn sends no shuttles and starts no effect, the outcome showing at once (594 A).

   Part 4 (Gilad, 2026-10-20; 595-603, after a preview page of the lights in
   BlackBoxPlus-references\previews\s5p4):
   - the ship takes the derelict's place as its rise ends, with its model, and the order's work is over then, so the
     platform comes apart without the salvage notification; the success's hold is gone, and with it the turn left in
     its tooltip and a Breaking in after a turn to salvage and back, which nothing can do once the roll succeeds
     (595 A);
   - the damage of 174, read as an engine of the Engine type and armour on the hull's outer edge as designed (600 A,
     601 A), its shields and stored power empty (603 A), its lights flickering on (596 C), its name saved with the
     derelict, and the order home to the nearest planet that repairs (602 A);
   - during the rise the panel shows the full bar with no buttons and the selection passes to the ship (597 B, 598 A);
     the ship comes for the empire whatever happens to the platform, passes on with the order, and an empire gone by
     then gets nothing (599 A);
   - Codex 100093 gains the recovered ship;
   - review (604-613): a module alive keeps at least 1 health (604 A); Main Engineering stays dead, the Codex telling
     of a ship whose only reactor it is coming back dark (605 B); the selection passes only to the player's own ship
     (606 A); the ship's strength is read again with its damage (607 A); a ship that cannot be made, or fails to,
     leaves its derelict to sink, logged, and the other derelicts risen that frame still get theirs (609 A, 610 A);
     the Codex says a safe planet first (611 A); armor spelled as the code spells it (612 A); tests of the 1 health,
     the design's repair, the 3 seconds and an order saved after its ship appeared (613 A). Logged, not fixed: the
     model blinks out for the one frame before the new ship knows it is on screen (608); during the rise the bar's
     tooltip (7206) still says losing the platform loses the work, and a platform lost then still tells of its loss
     (part 5); after a recovery the platform's status reads Salvage finished, as after a failed one (part 5); the
     derelicts window keeps a recovered derelict's row, back to its tech icons; a content reload in the frame the ship
     appears could miss its model;
   - then (Gilad, 2026-10-20; 614-617): a derelict rising for its ship no longer counts toward the drifting cap, which
     would refuse a ship dying on screen in those 3 seconds a drifting derelict at the cap's edge (614 A); a design
     cannot be overwritten or deleted while a derelict of it may yet come back as a ship, one that can be salvaged or
     rises for its ship and is not a station, as with a ship of it in the universe (615 A, 617 A), with texts 7211 and
     7212 that name the derelict (616 A); Codex 100207 adds the derelict to the names a design cannot take;
   - review of the fixes: a ship that fails halfway is taken out of the universe again, its model handed over only
     at the very end; the Codex says the command modules repair at full rate while they are powered, as each module's
     power decides; the Russian and Ukrainian texts call a design a дизайн, as the design screen does. Logged for
     part 5: the order's `Recovered` is also true when no ship could be made, so the recovery's notification must
     follow the ship's arrival, not `Recovered` alone.

   Part 5 (Gilad, 2026-10-21; 623-635, after a preview sheet of in-game renders in
   BlackBoxPlus-references\previews\s5p5):
   - the notifications of each outcome under Salvage Alerts (see Notifications, 623-630, 634); a called-off recovery
     no longer sends the salvage notification (628 A), nor does a platform or station lost during the rise (630 A);
   - during the rise the bar's tooltip says the ship takes the derelict's place and is the empire's whatever happens to
     the platform or the station (7213, 631 A), in place of 7206, which told of the work lost with them;
   - from the ship's arrival until it starts to come apart the platform's status reads Ship recovered (7214, 632 A),
     where it read Salvage finished; a failed one reads Dismantling at once, as before;
   - a row of the derelicts window leaves it once its derelict can no longer be selected: recovered as its ship
     appears, broken apart, blown up, stripped or sunk away; a rising derelict's row stays until its ship appears
     (633 A);
   - Codex 100093 tells of the notifications, that losing the platform or the station loses nothing once the roll
     succeeds, the rising derelict's row and rows leaving the list; 100173 names the recovery's outcomes; the rest of
     Recovering a derelict was checked against the code (635 A). Not tested, as text swaps: 631 and 632. Still
     logged: the model's one-frame blink (608) and a content reload in the frame the ship appears;
   - review (636-642): the success is told inside the ship's making, so a failure to tell cannot cost the other
     derelicts risen that frame their ships, and keeps the ship (636 A); another empire's platform, an ally's shown,
     has no tooltip during the rise, which would call the ship yours (637 A); the platform reading Ship recovered for
     up to a turn when no ship could be made is left (638 B); the Codex says a rising derelict's row leaves once its
     ship appears (639 A), and 100173 that losing the platform or the station after a successful roll tells nothing
     (640 A); the tooltip inventory's scan misses the claim tooltips, `DerelictSalvagedBy` since `ClaimTip` and
     `DerelictRecoveredBy`, both unhooked, and is left (641 A); the tests check which notification came, and the
     alerts switch's test that the notifications come with it on (642 A).

   Pre-merge review (Gilad, 2026-10-21; 643-658, after the Codex adversarial review and three in-house area reviews,
   recorded in BlackBoxPlus-references\reviews\recovery_premerge_review_2026-10-21.md):
   - the derelicts risen in a frame get their ships even when another derelict's update throws, the hand-off run
     from a finally (643 A, Codex's one finding, which an in-house reviewer found too);
   - a successful roll is past calling off, as it was past turning: a click queued in the frame of the roll does
     nothing (644 A);
   - the one frame between the list's update and the ship, when the panel shows the derelict's techs, is left (645);
   - a module of under 1 max health is a mod's error, as a module below 1 health is destroyed, and the content load
     logs a warning for it; vanilla, Combined Arms and Star Trek have none (646);
   - a derelict whose ship cannot be made is gone at once with its model, out of the list, rather than sinking away,
     as its comment and test now say (647 A);
   - another empire's recovering platform shows no recovery bar or tooltip on its panel, its break-in unshown, as
     in the derelicts window (562 A), which leaves 637's guard nothing to guard (648 A);
   - a click on the success notification shows the system once the ship is not the player's (649 A);
   - left as they are: the break-up's seconds unsaved, so a save in them sinks the derelict at once on load (650 B),
     the tooltip's turns left counting the platform's turns alone (653 B), and no positive control for the recovery
     shuttles (655);
   - the rebels' luck seed comes from the clock as they rise, so a reload that runs a defeat again gives them new
     draws; logged for the ai sub-branch, as nothing recovers for them until then (651 B);
   - the Salvage Unit's description says it can recover a ship (7143, 656 B); Codex 100207 spells center (657 A);
     the summaries of 7201 and 7203 end at a word within 60 characters (658 A); a DerelictRecovery comment no
     longer names the salvage techs, which sub-branch 8 brings (652 A);
   - the derelicts window reads the goals as it opens and once each click has reached the sim thread, a read queued
     behind the click's own work, instead of every frame, its station button's design pick included (654 A): the
     game does not run under it, as a universe covered by another screen runs only the queued work of clicks, paused
     or not; the Exotic Systems screen under it pauses the game, except when opened from the planet list, which
     closes first and leaves the universe covered, and the window pauses the game itself only when opened over the
     running game, which play never does (659 A). The derelict panel, with the game running under it, still reads
     every frame. A click cannot change the goals on the UI thread even paused: the paused sim thread still runs the
     queued actions and walks the player's goals for the build icons;
   - review of the fixes (659-663): a derelict's hand-off that throws is logged and the others risen that frame
     still get their ships (661 A); left: no test of a row button's click or of 643, 646 and 648 (660 A), the window's
     tech tooltip without the gained line for an unbuilt order on a derelict a rival claimed, which reads 0 there
     (662 A), a second click within a frame of the first playing its sound for nothing, as before (663 A), and the
     claim tooltip looking up a rival's order while it is hovered (664 B).
6. **hauling** (was 5): freighters pick up a station's stored production for colonies (132 B); the hauling itself
   came last (Gilad, 2026-10-22; 665-673, 716-720).
   - first, the derelict caps (Gilad, 2026-10-21; 676-679, 686-691): each is the galaxy's star systems times its
     multiplier, used from 0.1 to 10 (see How many). Review: the clamp at 10 also keeps a huge value from overflowing
     the cap to 1 (686); `Cap` and its limits are private (688); the tests check the shipped default of 1 (690), and
     the debug key's test has 10 systems, so it tells the salvageable cap from the drifting one (691). Test universes
     start with no systems, so a test that makes more derelicts than its own systems adds bare ones in rows from the
     universe's corner (`StarDriveTest.AddRemoteSystems`, 689). A multiplier under 0.1 warns too (692 A).
   - then the drifting derelicts (Gilad, 2026-10-21; 680-685), so a player who comes to a system after a battle sees
     some of its wrecks, salvage ops on or off: the hull's second roll, unseen ones kept, the 10 s stop, the rule
     outside every system and the cap counting every one (see How many and How a derelict moves and ends). Tests fix
     the hull's roll with `Derelicts.TestDriftRoll`, null in the game; the roll itself is tested through a seeded
     random. Review: the player's own sensors only, outside every system (693 A); a listed system shows its drifting
     ones (694 A); a test that the oldest of two gives up its place (695 A); no test of the warning under 0.1 (696 A).
   - The station's own order hauls, as research and mining stations get their supplies from their own orders
     (665): each turn, while what it holds beyond the room in the holds of those already on their way is an
     average freighter load (20 at game start) or more, it calls another idle freighter, with at most 3 on their
     way at once, like a mining station's 3 supply runs (667 A). Once it has no room for another platform load (75
     of the Basic Salvage Station's 316), 1 or more is enough, as an average load can outgrow the station's hold
     (721 A). Orders run before the trade turn, so hauls take idle freighters first. A freighter holding other
     goods is not called. No order is kept per run: the station counts the freighters with an order to load at it.
     A station counting down to its scuttling calls none (727 A).
   - Where a haul goes (666 C, 717 A): the closest of the empire's colonies with an open production import slot
     that an idle freighter reaches, then the closest of its trade partners' such colonies, with Allow Player
     Inter Trade on and the freighter's own inter-empire trade on (a partner's colony counts the 35-turn limit and
     pays the 2-credit tariff), then the closest of its colonies with room for 3 or more in its production store, counting
     what is on its way there, loaded or still to load. Closest is the straight distance from the station; the
     quickest freighter there goes. Only trade partners count, not allies without a trade treaty (716 A).
   - No freighter is built for a haul (668 B). Areas of operation and route lists rule as for research and mining
     stations: a station inside one of the freighter's areas, or a freighter with neither, and a route list alone
     never reaches an untethered station (669 A). With Auto Trade off only freighters set to trade production go.
   - While an armed hostile is in the station's sensor range it calls none, and a haul outside the system waits,
     giving up after the 120 s of a blockaded colony (670 A, 722 C: not the whole system, so an enemy's platforms
     elsewhere in it do not block the station for good). Armed means it has weapons, so an unarmed station working
     the same wrecks does not count (725 A). Hauled production is taxed like any delivery (671 A).
   - The freighter lands on the station and, at touchdown, fills its hold whatever its colony's room, to move the
     production out of the station (Gilad, 2026-10-22); only a station holding nothing calls the run off. It then
     delivers like any freighter, keeping aboard what the colony has no room for. Every delivery, a colony's included,
     is now taxed only for what the colony takes (723 A). A freighter loading another good leaves what it still holds
     in that colony as far as its store has room, taxed as a delivery, and what has no room turns into credits at the
     tax rate (724 B; before, the store's cap destroyed it). A ship scrapped at a colony unloads its food, production
     and colonists there as far as there is room, the goods taxed; goods with no room turn into credits at the tax
     rate, and colonists with no room are lost (Gilad, 2026-10-22). Those credits are paid with the turn's income
     under Excess Goods, as a colony's own excess goods are (734 B): the empire gathers them, and its money turn
     collects them once, as the budget screen and techs update incomes between turns too. Saves keep those still to
     come, and an empire absorbed passes them on (review).
     A station lost or in other hands calls off a run still on its way; a load aboard is still delivered. A run whose
     colony is lost, quarantined or no longer a trade partner's is called off too, and a run called off flies to the
     nearest rally point, as it has no exporting colony to fly back to (728 A). A freighter reaching a station that
     other freighters emptied does not land. Two freighters touching down on one station in the same step can both
     load its whole hold, as two at one colony can (729 C, left). The ship list reads "Picking up
     Production from <station> to <colony>", the trade lines go blue to the station and gold to the colony, and
     the haul adds space-road heat as colony trade does.
   - Only the player's stations called freighters here; AI empires' and rebels' call theirs since sub-branch 7 (740 A).
   - Once nothing is left to do in its system, the station sends what it has left, below an average load too, and
     once it holds nothing it is scrapped, vanishing as a scrapped station does, with no refund (667, 718 A,
     719 B). Nothing is left to do when no salvage or recovery order of its empire's is left there, its platforms
     gone, and every salvageable derelict its empire knows of there is claimed by another empire or gone; a known
     derelict not yet ordered keeps the station. The player is told once, under Salvage Alerts, as the station is
     scrapped (720 A; with 719 B there is no dismantling to tell of first). Less than 1 aboard counts as nothing, as a
     haul takes no less. A station taken over gets its first turn with the goal pass, not at once, so the change of
     hands has shown its new owner the derelicts there first, and a station not yet placed in its system, which has
     yet to show them, does not end, nor one about to change hands, which calls no freighters either, as a boarding
     applies after the turn's goal pass (732 A). A derelict claimed by an empire with no station there
     still counts as work: that empire's claims end with its next goal pass, and the player's pass comes first, so a
     station just boarded would otherwise be scrapped with its old owner's claims still standing (review).
   - One test of what is left to salvage rules the order, the drop of an order not yet built and the station's
     end (726 B): a known salvageable derelict that no other empire works from its station there. Where other
     empires work every one, the derelict panel's button is grey with its tooltip saying so and the derelicts
     window says so in grey in the button's place (text 7221), placement shows no zone there, and an order not yet
     built is dropped as when the derelicts run out. Before, a station could be built only to be scrapped a turn later.
   - The station's status reads "Waiting for freighters" while it holds production and none is coming,
     "Freighters on their way: N" while some are, and "Salvaging" when it holds nothing (672 A). No notification
     for a full station with nowhere to send its production (673 A).
   - Parts: 1) the hauling; 2) the station's end, its status, the Codex text: Hauling the salvage in 100093, and the
     Freighters (100055), Automation Freighters (100164), Alerts (100173) and Deep Space Building (100086) entries.
     Texts 7218-7221.
   - Pre-merge review (2026-10-22; Codex approve, three in-house areas, no blockers): 727 A, 728 A, 732 A and 734 B
     above; 729 C, the same-step double load, left; 730, 731 and 733 not answered, nothing changed (a scrapped
     station kept in a save through its last haul's FromStation, the status line's walk of the empire's ships each
     frame, and a one-frame flip of the panel's grey button as another empire's goal list shifts); 735 the Codex
     wording; 736 this plan; 737 A tests of the 35-turn partner limit, allies without a trade treaty, a waiting
     haul going on, a station changing hands, colonists lost on scrap and a docked freighter called first.
7. **ai** (was 6): the station planner and design picker, where auto salvage ops and the AI place a station (249), the
   player's auto-build switch (252), the strip/recover choice from the tech list, the rival rule (138), constructors
   and platforms kept out of danger, pirates in recover mode (176) with salvage station designs of their own, since
   they have no small station hull (315), and later their raids to capture a salvage station; the rebels' luck seed
   made from their parent empire's, so a reload that runs a defeat again draws the same for them (651 B). Once AI
   empires order salvage: a claim with no order behind it (a save's, or a defeated faction's, whose goals are not
   cleared) never ends, and with the 726 B test it keeps another empire's station there at work for good.
   - Decisions (Gilad, 2026-10-22; 739-769):
     - Parts (739 A): 1) AI hauling and the station's end; 2) the AI planner, its stations and its derelict orders;
       3) a claim with no order behind it and the rebels' luck seed; 4) the player's auto switch; 5) pirates, talked
       over again before they are built (747 A).
     - AI empires haul and their stations end by the player's rules, no one told and no freighter built for it
       (740 A); so do the rebels', as they finish what they inherit, while a station pirates hold does nothing for
       them, as 358 B keeps it.
     - The planner runs as the mining planner does: not with salvage ops off, for an empire that cannot build a
       salvage station, or for the player with Auto Build Salvage Stations off (768), every `MiningOpsTurnsPerRun`
       turns, one new station a run. A system it has explored, with a salvageable derelict it knows of, within the
       distance its mining and research stations use, outside enemy influence, with no Remnant portal and no rival
       there (741 B). A rival has a colony in the system and is neither at war with it nor holds open borders or an
       alliance with it, as no trust is lost in those cases (138, 760 A); the player counts as any empire (765 A).
       Only where the production left in the derelicts it can work there is at least the station's cost, or one of
       them holds a tech it wants or is a ship it would recover (741 B, 759; the techs its design uses, 779 C). The
       site is the panel button's (249).
     - No station and no new order where it knows of hostile strength (a station's new orders wait only for an
       armed hostile in its sensor range, 778 A); it asks its military to clear the system as the mining planner
       does, and a constructor or platform already out carries on (742 A).
     - The station's goal fills its free units each turn, the derelict with the fewest turns left first, as a claim
       stops its clock (744 A), a station held without the tech to build one included (769 A).
     - Strip or recover, chosen once as the order is placed (758 A), from the chance and never the empire's own
       draws: it recovers a ship whose design is stronger (`GetStrength`) than the strongest design of its hull role
       it can build now, or than none when it can build none of that role (743, 750 A, 751 A), whatever its techs
       (752 A) and its chance (753 A), and strips the rest. A tech it wants is one it can still learn that its
       designs use (743, 749 A): `TechEntry.shipDesignsCanuseThis`, which AI research reads too (761 A), from every
       role of the race's list (762 B) but shipyards and the Salvage Platform, which the research progression skips
       as well (767 B). Its recompute turns the flag on as well as off (766 A), at start, on load, and on absorb with
       the race's designs read again (764 A), and when the player saves or deletes a design (763 A).
     - The player's auto switch (252, 745 A): Auto Build Salvage Stations in the Salvage Station dropdown's row
       (334 A), off in a new game (756). With it on, the planner orders the player's stations and salvage by the
       same rules (754 A), passing over systems with hostiles and sending no military; the Salvage and Recover
       buttons are grey with a tooltip saying salvage is on automatic, and their second click does nothing (381;
       as built, a standing order's stays lit and no click does anything, 781 A). A
       station can still be ordered by hand, and Abort Deployment still works, auto free to order one there again
       on a later run; turning auto off leaves its orders as the player's own (755 A). Codex 100167 and 100093 say
       what the switch does, not how it picks (746 A).
     - Rebels and Remnants never order salvage (757 A).
     - Each turn a derelict lets go of a claim whose empire is defeated or has no order on it (748 A).
     - A station pirates hold neither hauls nor ends (770 A), which 100086 leaves unsaid until the pirates part
       (771 A). One whose empire owns no colony, as the rebels', is scrapped once nothing is left to salvage, with
       whatever it holds, as it has nowhere to send it (772 B); so is one of an empire that has just lost its last
       colony, its goals running a pass before its defeat, as the rebels it would pass to would scrap it (773 A).
   - As built (part 1): every station but one pirates hold calls freighters and ends by the hauling rules, a player
     alone told of a station scrapped; Codex 100086 no longer says the station's end is only yours, and adds the end
     of one whose empire has no colony.
   - The flag as research reads it (774 A): a tech that leads on to one unlocking no module, a bonus or hull tech,
     is of use too, so about a third of the techs no design of the race uses still count, 14 of 36 for the Terrans,
     the laser beam turrets among them; research needs it so, or it could not reach those techs. A design the player
     saves or deletes reads only the player's designs again, not those of AI empires sharing a ship style, as a load
     does (775 B), and diplomacy's tech values still count shipyards and the salvage platform (776 A).
   - As built (part 2, the flag): `Empire.UpdateShipDesignTechUse` works the flag out both ways from the race's
     designs, shipyards and the salvage platform left out, at start and as a save loads (`InitEmpireUnlocks`), and
     `Empire.ShipDesignsChanged` reads the designs again on the sim thread as the player saves or deletes one and at
     the end of an absorb, once the absorbed race is the empire's. A flag a save held off turns back on at load.
   - A station's new orders wait only for an armed hostile in its sensor range, as its hauls do, so a rival's
     unarmed salvage station there, which has a little strength of its own, holds off none; the planner keeps the
     mining planner's rule of any hostile strength known (778 A). Worth a station is read from the derelict's design,
     the techs it uses, as anyone can tell from the ship, not the few it holds, which only a station there shows
     (779 C). Where the empire's own borders reach the system, an enemy's colony there does not keep it out (760 A).
   - As built (part 2, the orders and the planner): `SalvageOps.OrderDerelicts`, `SalvageDerelict.WouldRecover` and
     `DesignHasWantedTech`, and `SalvageOpsPlanner`, saved with the empire's AI and run after the mining planner,
     which asks for a fleet only once while one is on its way. The Remnant portal rule, the derelicts other empires
     work from their stations, and the call from the AI's turn have no test.
   - As built (part 3): each turn, as its timer runs, a derelict lets go of a claim whose empire is defeated or has
     no order on it still at work, as a save can catch one, or an empire's goals cleared with two orders on the
     derelict, each leaving the claim to the other, which has no test (780 A); a claim passed on holds, as the order
     passes with it at once. Today only a test reaches the defeat rule: a defeated empire's goals are cleared,
     pirates clear theirs before they fall, and rebels are never defeated. Rebels take the luck seed of the empire
     they rise from, their id keeping their draws their own (651 B).
   - With auto on (781 A), a standing order's button stays lit, showing its work, every other is grey, none lights
     under the cursor, no click does anything, and every tooltip says salvage is on automatic. The checkbox shows
     whenever salvage ops are on, as those of research and mining do, so a station captured without the tech orders
     by itself with auto on (782 A). The orders the switch places tell the player as orders by hand do, the Salvage
     alerts box turning them off (783 A).
   - As built (part 4): `Empire.AutoBuildSalvageStations`, saved, off in a new game; the Automation window's salvage
     row is its checkbox, text 7158 retitled, the row's dropdown hiding as those of research and mining do. With it
     on, `SalvageOpsPlanner` runs for the player, asking for no fleet, and every salvage station of the player's
     fills its units; `DerelictSalvageButton` keeps the buttons from ordering. Texts 7222 and 7223; tooltips 7157
     and 7160 name the list by the checkbox; Codex 100167 and 100093 say what the switch does. A hidden dropdown in
     any of the window's checkbox rows takes no clicks, as one under Auto Pick could pick a design unseen (784 A).
     The window's rows have no test.
   - Pirates (Gilad, 2026-10-22; 785-791): a station they capture works for them, and their orbital raids go for a
     salvage station first, whatever orbital they roll (785 B, 791); they get no designs or stations of their own,
     which a later pirate part may bring. Their station recovers every derelict ship it can in its system, the
     fewest turns left first, and never strips (786 A, 176); its bays launch the default constructor of the race
     whose station it is, as pirates build none (787 A), and it regains a tenth of its ordnance a turn, as a pirate
     base does out of combat, unless its design makes ordnance (788 A). A ship they recover heads for their nearest
     base, where the rules for the ships they capture apply: one below a cruiser is taken in, its design joining
     theirs with a level-up roll, and a cruiser or larger stays on as an escort, while one of a design they already
     spawn is taken in for nothing (789 A), so their station passes over a derelict of such a design, their own
     wrecks among them (792 A). With nothing left to recover the station stays, neither hauling nor ending (790 A,
     770 A). Their recovery chance is the default; no tech raises it (809 C), and they gain salvage speed, and with
     it recovery speed, by their level instead (sub-branch 8).
   - As built (part 5): `SalvageOps.OrderDerelicts` for pirates, `SalvageDerelict.ConstructorName` given the
     station, `SalvageOps.ReplenishOrdnance`, the recovered ship's `OrderPirateFleeHome`, which a constructor out from
     its own salvage station ignores, as a raid's recall would strand its order, `Pirates.TargetType.SalvageStation`
     and `PirateRaidOrbital.PickTarget`; a pirate base under attack drafts no constructor to defend it, as a mod's
     armored one has the strength it drafts by. Codex 100086 and 100093 said what a captured station does for
     pirates, until the pre-merge review took it out (795 A).
   - Pre-merge review (Gilad, 2026-10-23; 793-802): Codex approved with no findings; the three in-house area
     reviewers found the rest.
     - A station of an empire with nowhere to send what it holds locked up once its hold was full while a platform
       there held production: the platform's load waited for room for good, so its order never ended, its claim
       held and the derelict's timer stood. Now what such a station cannot take of a platform's load is lost, the
       platform strips on, and the station is scrapped with what it holds once the work is done (793 A). Rebels
       count as having nowhere to send it whatever colonies they hold, as they build no freighters, though they haul
       with those they inherit (794 A).
     - Codex 100086 and 100093 say nothing of what pirates do with a station they capture (795 A).
     - Logged, not built: a constructor pirates board in the turn after they board its station sits idle for good,
       as the early return in `OrderPirateFleeHome` keeps it from going home (796 A).
     - The planner counts only derelicts with at least a fifth of their turns left, and a salvageable derelict lasts
       200 turns times the production pace, up from 100 (797 B).
     - An empire with no colony left, defeated that turn, has its stations order nothing (798 A).
     - Texts: the 7157 summary, "color" in 100093, and 100086's no-colony end, which names the rebels and comes
       after its pointer to Combat Basics (799 A); this plan's stale lines (800 A); "color" and "labeled" in
       100207 too (804 B). A rebel platform with its hold full reads Station hold full for the turn before its load
       is lost, as the station is full (803 A).
     - Tests: the planner's setup check for the player, the design read again off the sim thread, and the switch
       and the planner's timer through a save; Abort Deployment with auto on (755 A), the player's station held
       without the tech (782 A) and the switch's orders telling the player (783 A) still have none (801 A). An
       empire that has lost its last colony scrapping its station with what it holds (773 A) has one now too.
     - As built: `SalvageOps.NowhereToSend`, used by the station's end and `SalvageDerelict.SendLoad`;
       `Derelict.LifetimeTurns`; `SalvageOpsPlanner.MinTurnsLeftPercent`. The full suite runs before the merge
       (802 A).
8. **salvage techs** (moved out of recovery; Gilad, 2026-10-20; 516): the techs that raise the salvage speed (270),
   and with it the recovery speed, always twice it (554); they leave the break-in alone. Techs that raise the
   recovery chance, which pirates get by their level (Gilad, 2026-10-22), are dropped (809 C).
   - Decisions (Gilad, 2026-10-23; 805-818):
     - One tech, Salvage Speed, a leaf off Small Stations, the tech that unlocks the Salvage Unit, as Mining Speed is
       off Deep Core Mining (805 A). Mining Speed's shape: 5 levels, each adding a fifth of the salvage speed,
       costing 1000 and half as much again each level (806 A).
     - A turn's strip still stops at the room the platform's hold has, the break-in is unchanged, the strip's flashes
       and junk come faster with the speed, the AI researches it as any bonus tech, and its texts are 7224-7226 in
       English, Russian, Ukrainian and Portuguese (807).
     - Hidden with salvage ops off, as Mining Speed is with Mining Ops off, by the tech's `RequiresSalvageOps`
       (808 A). Mods that load only their own techs never load it; a mod that loads vanilla techs and overrides Small
       Stations without the link leaves it with no parent, never discovered.
     - No tech for the recovery chance (809 C, 810).
     - Pirates, who research nothing, gain a tenth of the salvage speed a level from level 3, +180% at level 20
       (811, 814 A). Gilad then had it set where they gain their other bonuses as they rise a level,
       `Pirates.AdvanceInTech`, into their own `EmpireData.SalvageSpeedMultiplier`, so no other code tells pirates
       apart (815, in place of 812 A). A level down keeps it, as it does their fuel cells, until their next rise sets
       it by that level, and an older save's pirates gain it at their next rise, those already at level 20 never
       (816 A, 818 A).
     - The diplomacy screen's empire stats show a Salvage Speed Modifier, as they do Mining Speed's, text 7227
       (817 A).
     - Its icon: Mining Speed's bolt beside a small medallion of a ship broken in two (813 A; preview sheet in
       BlackBoxPlus-references\previews\s8icon).
     - An older save gets the tech on load, as the game adds every tech a save lacks (`Empire.CreateEmpireTechTree`);
       Gilad loads one in game to check.
     - As built: `EmpireData.SalvageSpeedMultiplier`, set by research and by `Pirates.AdvanceInTech`;
       `SalvageDerelict.SalvageSpeed`, `DerelictRecovery.Speed(Empire)`; Codex 100093 names the tech.
     - Pre-merge review (Gilad, 2026-10-23; 819-825): an empire absorbing another came a level up in each
       multi-level tech the other had researched, its bonus counted again, so it gained two levels however far the
       other had gone, in every multi-level tech since before this feature (Mining Speed, Refining Efficiency,
       Exotic Storage, Ordnance and Warhead Development, Coherency, Energization, Dispersion, Durability, Dyson
       Swarm Overclock Upgrade); now it comes up to the other's level, a bonus for each level gained, and keeps its
       own when higher (819 A, kept with all of them in view, 824 A; Codex 100130 says so). A load, which runs the
       absorb's tech transfer again, keeps the levels the save has, so an older save's absorber stays where the old
       rule left it (825 A). A tech hidden since the save, as a mod turned its feature off, leaves the research
       queue and, unresearched, the tree; research never picks a hidden tech and the queue takes none, so no
       empire's research stalls on it (820 A). A speed of none, from a mod, shows no strip effects (821 A).
       Codex 100182 says which techs each switch hides (822 A). The Recover tooltip shows the recovery speed
       rounded up (823).
9. **Codex review** (Gilad, 2026-10-17; 333): the last stage. Gilad reviews the Codex text of the whole feature.
   - Decisions (Gilad, 2026-10-23; 826-830), worked on the base branch: how a ship dies leaves Combat Basics for a
     Warfare topic of its own, How a Ship Dies, right after Combat Basics (828 A), with Destruction, Death Blasts and
     Wrecks under it (827 A); Death Blasts says in short what a blast does, without its percentages (827), and
     Wrecks how a derelict forms and which drift (826 A). Derelicts follows, with Salvaging, Recovering and Hauling
     under it (829 A). Codex ids 100232-100255; the derelict and salvage tooltips point to the entry each describes,
     the ship structure one to Destruction, and the see pointers follow (830). Combat Basics opens with a lead that
     points to both topics.
     Then (Gilad, 831): Wrecks, Derelicts, Salvaging, Recovering and Hauling each in gold-titled sections of a
     paragraph, in plain words without the exact numbers (a small chance, varies with the hull, clear of the radiation
     zone with some margin), as players skip long technical paragraphs.
     F1 opens the page each tip describes, the rival's claim and the derelict tech icons too, which had no link.
     With Auto Build Salvage Stations on, the player can still switch a standing order between salvage and recovery:
     its other button lights and switches it, as auto never revisits that choice, while its lit button calls nothing
     off and a derelict with no order takes no click (832 A); the lit button's tip says so (7228), and the Automation
     tip and the Codex say the buttons still switch (833 A).
     The self-review put back what the plain words had lost or bent: who knows of a derelict is settled as it
     forms; only a freighter from outside the system waits on a hostile; a recovered ship stays fragile until
     repaired; with salvage ops off it is the hull's roll that decides. On automatic the lit button offers the
     switch only where one can be made, a station's derelict never, and a click needs a standing order.
10. **dismantling** (Gilad, 2026-10-22; 719): every station and platform scrapped where it stands is taken apart
    over turns, as a salvage platform is, instead of vanishing.
   - Decisions (Gilad, 2026-10-23; 834-842), on feature/salvage-ops/dismantling. Every scrap of a platform,
     station or orbital shipyard takes it apart over turns: the Scrap button on its panel and on its ship list row,
     the governor's scrapping and a salvage station's end (834 A). Scuttle in the right-click menu stays a
     10-second self-destruct, and removals by rule stay instant: an orbital over the limit at a planet not yours,
     a research station whose target can no longer be researched, a second salvage station taken over, the old
     orbital a refit replaces. Subspace projectors keep vanishing at once, and the salvage platform keeps its own
     dismantling (835 A). It takes 1 turn per 50 slots (836).
     Coming apart, it stops at once: no weapons, research, refining, building, supply or repair, and no upkeep. It
     leaves the planet's orbitals, so the governor and the orbital limit count its slot free. It can still be shot,
     and its status reads "Dismantling" (837 A, 840 B). Every orbital shows the salvage platform's coming-apart
     effect. Clicking Scrap again calls it off, whole and at work again; the AI never calls one off (839 A). No
     notifications (841). Destroyed, it dies as any ship does; boarded, its new owner gets it whole and at work
     (842 A).
     Refund (838): one orbiting a planet of yours vanishes at the end, and the planet gets 50% of its production
     cost x its health, the excess paid as credits at the tax rate. A deep-space one is worth 50% x health too,
     and a salvage station all of its cost x health: the cost its goal stored when it was built, handed to the
     dismantling goal. That refund leaves in a builder ship, which the player cannot order, rising from the
     orbital as a recovered ship rises; an empire goal holds the amount. The builder flies to the nearest safe
     colony with room in its production store, stops within the planet's radius + 200 and sends what fits, its
     shuttles 1 per 50 production where seen, then goes on to the next colony, up to 10. Where all of it fits, or
     at the 10th, it lands on the planet or a shipyard and unloads all, the excess paid as credits at the tax
     rate, and the goal ends. It can be attacked.
     Then (Gilad, 2026-10-23; 843-856): the turns are 1 per 50 slots, rounded up, at least 3 and at most 20, not
     scaled by the pace (843). An orbital tethered to a planet not yours, a research or mining station's, or one
     whose planet changed hands, counts as deep space (844 A). A salvage station gives all of its cost on every
     dismantling, a Scrap while at work included (845 A), and the production in its hold joins the refund in full,
     not scaled by health (846 A). The salvage station's goal stores the station's own cost as built, which passes
     to a new owner with the station (847 A); the builder's goal stores its refund and uses it. Each leg goes where
     a scrapped ship goes: among the empire's five safe colonies with the most room in their production store, the
     nearest, skipping those it unloaded at; one turned unsafe on the way is swapped without counting a leg (848 A,
     written "S"). At most 20 shuttles a stop (849 A); a landing sends none. With no safe colony, what is left is
     paid as credits at the tax rate at once and a builder in flight is removed; with every colony full, it lands at
     the nearest and the excess is paid as credits (850 A). The builder's status reads "Delivering production to
     <colony>", its load above the panel as a freighter's (851 A; text 7230). Its health is taken as the orbital
     goes, with no repair while it comes apart (852); a builder destroyed or boarded loses its load, untold (853);
     the salvage station's end notice goes, with text 7220 (854); the refund lands untaxed and is not trade (855);
     every empire works by these rules (856).
     Its troops leave for a planet as it goes (Gilad, 2026-10-23; 857 A, written "S"), and production aboard any
     orbital, a research or mining station's supplies too, joins its refund in full (858 A).
   - As built (part 1, the dismantling): a DismantleOrbital goal starts it and ends it; ShipAI.OrderScrapShip sends
     every orbital Scrap takes apart there, from the panel, the ship list row, the governor and the salvage station's
     end, whose order waits for the station's end. Research and mining orders wait while their station comes apart,
     so a call-off brings it back to work, and let go of their planet as a death does once it is gone. A boarded one
     stops coming apart as it changes hands. The tooltip of both Scrap buttons on an orbital is text 7229; the
     salvage station's end notice and text 7220 are gone. Codex: Refitting and Scrapping gains a Dismantling an
     orbital section; Hauling and Alerts follow, and the Salvage Alerts tooltip drops the end notice.
     The self-review: a second Scrap of one coming apart, as a selection's, called the first off and left it to be
     removed whole; now it changes nothing. A call-off brings it back only while its place is still free: its planet
     its empire's or no one's, with room among its orbitals, a mining station's ops still its empire's (Gilad,
     2026-10-23; 859 A); a salvage station's cannot be called off, its salvage ended as it began (860 A).
     Its planet taken while it comes apart, it stays its empire's and never joins the captor's orbitals. A refit
     never takes one coming apart, and a refit constructor on its way scraps itself as at a lost orbital; one
     counting down to its scuttling is not taken apart, one dying gives nothing, and repair drones pass it over. The
     governor still removes another empire's orbital in its orbit at once.
   - Part 2 (Gilad, 2026-10-23; 861-868): the builder rises with the shipyard launch from where the orbital stood,
     facing its first colony (861 A). The ship list shows it, with its status and no Refit or Scrap buttons (862 A).
     Once no colony it has not stopped at has room, it lands at the nearest safe colony, one it stopped at included,
     the excess paid as credits (863 A). A colony whose store filled on the way still counts as a stop and is skipped
     from then on (864 A). The colony lost during the landing, the whole load is paid as credits at the tax rate
     (865 A). An empire absorbed loses the builder and its load, as one that falls does (866 B). Loads on their way
     count against a colony's room, for builders and scrapped ships alike (867 A). It makes at most 10 stops, or one
     for each of the empire's colonies when it has fewer (868, Gilad's clarification).
   - As built (part 2, the builder): a DeliverRefund goal holds the load and the builder, the empire's supply shuttle
     design. The design has no hold, so the panel draws the goal's load as a freighter's production. Its ship plan
     flies it within the planet's radius + 200 and stops it there, and the goal, at its next turn, sends down what
     the store has room for, untaxed: 1 shuttle per 50 production, rounded up, at most 20, where the player sees it.
     Each turn the goal decides whether it lands there instead (all of it fits, or its last stop), on a shipyard of
     the colony or the planet, and unloads it after touchdown as a scrapped ship's goal does. Its status is text
     7230. It takes no orders: it cannot be scrapped or refitted, so it pays no upkeep; its panel has no order
     buttons, Refit All skips it, and clearing its empire's goals removes it. The salvage station's order now stores
     the station's cost as built (StationCost) and passes it to a new owner on a take-over. Codex: Refitting and
     Scrapping gains The builder ship's delivery; the Dismantling section, the 7229 tooltip in four languages and
     Hauling's station's end follow.
     The self-review: the Excess Goods tooltip and the Economic Overview now name a refund no colony could take. The
     selection panel's order buttons pass the builder over, as its Resupply cleared the builder's order and let it be
     scrapped, the refund lost. A builder boarded just before its turn waits for its change of hands instead of being
     paid out and removed. The tooltip says a salvage station gives all of its cost rather than half; the Codex says
     the builder stops only where part of its load fits, and that with no safe colony no builder rises.
     Then (Gilad, 2026-10-23; 869 A): it may go back to a colony it stopped at once that colony's store has room
     again, so the skip of 848 and the "skipped from then on" of 864 go; the stop limit still ends the trip.
     The mutation run (46 of 51 caught) found the builder, flying in at full speed, braking only at the radius + 200
     and coming to rest about 100 inside the planet's disc. It now brakes in time, at sublight only, so it comes to
     rest close by and still warps on the way. Tests were added for the stop, for a colony lost on the way and for
     the unloading waiting for touchdown; the one escape left is a pair (the end of the delivery removes the builder
     as its last step does).
   - Part 3 (the tests and the Codex): 18 tests for what the part 1 mutation run let through and for 871, in
     DismantleOrbitalWorkTests, DismantleOrbitalTests and RefitLandingTests. One coming apart makes no ordnance and
     its planet does not rearm it; no ship turns to it for supplies or repair and repair drones pass it over; its
     beams go out; miners bring it nothing; a research station researches nothing and a mining station refines
     nothing; a Dyson swarm no longer counts its controller; a refit constructor on its way scraps itself; a new
     orbital is not placed where one comes apart; a refit never takes off from a shipyard coming apart; a call-off
     reaching the sim thread after its place is taken changes nothing. Once it is gone, a research station's planet
     can be researched again and a mining station lets go of its ops; its ops another empire's, a mining station's
     cannot be called off. Left untested: troop bays (no shipped station has one), the button's negative click, the
     resupply gate (its priority order already stops a resupply) and the goal's own check that it still comes apart
     (nothing else stops it while its goal is there).
     Then (Gilad, 2026-10-23; 870-878, after a Codex audit): a research or mining station keeps its place at the
     planet while it comes apart, as a call-off brings it back to work: no other research station of the empire's
     can be placed there, and a mining station still counts among the four allowed there. The 7229 tooltip in four
     languages and the Codex say so (870 A, in the words of 876 A, as the mining claim itself may lapse meanwhile).
     A call-off at a planet no one owns needs room among its orbitals too, as 859 said (871 A). The selection's
     Scrap tooltip, 7005, says an orbital is taken apart where it stands (872 A) and, as a lone projector's panel
     shows it too, that a subspace projector is removed at once (877 A). Exotic
     Resources says the mining claim may lapse while the last station comes apart (873 A). Hauling names the station's
     Dismantling status (874 A). The Russian tooltip names the builder ship as 4443 does (875 A). Logged (878 A): one
     boarded, or made whole by its empire's goals being cleared, at a planet no one owns rejoins its orbitals even when
     they are full, nothing removing an orbital over the limit once it is placed. Logged, from before the branch:
     2214, the ship list's Scrap tooltip on a subspace projector or a ship without engines, still says it flies to a
     planet where the button self-destructs it; and at a planet no one owns the orbital limit does not count the
     empire's constructors on their way, in placement and now in a call-off, so 100086's "counting those on the way"
     holds only at a colony.
     The recheck of the part 1 mutants caught 15 of these 16; the escape, a supply base supplying while it comes
     apart, is a double check, as one that cannot be refitted has no ordnance status to supply from. The 871 test
     fails with the old rule. The self-review added a refit constructor on its way to a deep-space orbital, which
     fails with that gate alone removed (as the one at a planet does: a refit always deploys as a structure, so the
     same check in the orbital deploy is never reached by one), and the research station's planet open to research
     again once its order ends.
     The pre-merge review (Gilad, 2026-10-23; 879-890): an orbital made whole as it changes hands rejoins its
     planet's orbitals when the planet is the empire's it passes to as well, after an absorb or when the empire that
     took its planet boards it (882 A). A station's supply order ends as it starts coming apart, and a freighter
     loading for it or on its way turns back with what it carries, as for a station lost (884 A). One counting down
     to its scuttling gives nothing, its turns waiting (885 A). The Codex gives the turns as 3 to 20 (888 A). Left as
     they are: the last turn and the builder's unloading not waiting for a change of hands about to land (879 B, 880
     B), its full combat strength while it comes apart (881 B), a station boarded where its new owner's own comes
     apart still removed (883 B), Star Trek's transporters (886 B) and Hauling's reasons a freighter gives up (887
     B); the gift test is named for a gift (889 B). Logged (890 A): a salvage station's stored cost is lost when its
     empire is absorbed or defeated, its goals cleared before the take-over, so its refund is its new owner's current
     cost.
     Then (891-894, after the self-review): logged, one at a planet another empire took rejoins that empire's
     orbitals when its own empire is defeated in the frame that empire boards it, the rebels then holding it there
     (891 A), and, as 878, one rejoining after an absorb or its captor's boarding even when the planet's orbitals are
     full (894 A). Left: no test for one passing to a third empire (892 B), and its progress running on while it
     counts down to a scuttle that is then cancelled (893 B).

Each sub-branch adds its Codex text and tests.

## Risks and catches

- The stop point, not the death point, decides whether a derelict is in a system or in a radiation zone.
- A recovered ship of a player design breaks on load unless the derelict's design is saved.
- No blast for derelicts shifts combat a little: stacked fleets take fewer death blasts.
- The roll, not visibility, removes the blast, so the camera never changes combat (170). A tumbling ship that did
  not roll a derelict still blasts when it ends (`Ship.cs:1760-1765`).
- Hauling gives a trade plan a station as its source in place of a colony, so every reader of a plan's source takes
  one: the ship list, the trade lines and the projector bridges (sub-branch 6).
- A platform keeps what it stripped in its hold until it sends a load, so losing one loses that load (275).
- Saves grow with derelicts: the throttle and the timer bound the salvageable ones, and the drifting cap the drifting
  ones. Each one is small (no modules).
