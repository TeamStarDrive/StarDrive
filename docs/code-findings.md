# Code findings backlog

Problems noticed while working on something else — mostly while writing Codex entries from the
code, and during PR review passes. Collected here so they survive and can be picked up
deliberately, rather than rediscovered.

**These are not filed as GitHub issues on purpose.** Most are not bugs in the sense an issue
implies. Several are unreachable dead code, several would change combat or economic balance, and
a few are decisions we made and want kept. Filing them publicly would also invite contributor PRs
against engine internals, each needing a full caller-impact review. Open an issue for one when
it is about to be worked on, or when we would genuinely welcome someone else taking it.

**Status of the evidence:** found by reading code, with a second fact-check pass on most.
**Almost none has been reproduced in game.** Line numbers are from 2026-09-22 to 2026-09-24 and
drift — treat them as a starting point, not a citation. Verify before acting.

Dates: power/damage/budget/misc logged 2026-09-22 during Codex buckets 4 and 6; the rest added
through 2026-09-24 from PR review passes.

**Tags:** `[latent]` unreachable or unused today · `[display]` the screen disagrees with what the
sim does · `[balance]` fixing it changes play, needs a test and Gilad's call · `[crash]` NaN or
null-deref class · `[thread]` cross-thread access · `[content]` data/xml cleanup ·
`[settled]` decided, do not "fix".

---

## Priority 1 — planet ambience — SHIPPED 2026-09-25, five of six dead cues now play

`[settled]` The `PlanetAmbient` category at `game/Content/Audio/AudioConfig.yaml:84` shipped six
sound effects when the NAudio engine was written, transcribed from the 2013 XACT bank and never
wired to anything: `git log -S"sd_planet_" -- "*.cs"` and `git log -S"PlanetAmbient" -- "*.cs"`
both returned nothing across the whole history. Five of the six now play.

**What it does.** While a colony screen is open, the viewed planet's ambience plays **once**,
layered over the music. `PlanetType.AmbientCues` is a `[StarData] string[]` read from
`PlanetTypes.yaml`; an empty or missing entry means silence. `GameAudio.SetPlanetAmbience` is
driven once per frame from `UniverseScreen.Update`, next to `ScreenManager.StartMusic`, and
cleared in `UnloadContent`.

**The rules, and why each is shaped that way:**

- **The selected cue is kept while it still appears in the new planet's list.** This is not a
  nicety, it is what makes the per-frame call safe: without that early-out the driver would stop
  and restart the sound every frame, and arrowing between two colonies that share a cue would cut
  it. The guarding test asserts stability across repeated calls with a **multi-entry** list — a
  single-entry list re-picks the same string and proves nothing.
- **The handle lives in `GameAudio`, not in `ColonyScreen`.** `UniverseScreen.workersPanel` is
  reassigned without disposing the previous screen (`UniverseScreen.Camera.cs:123`,
  `ColonyScreen_HandleInput.cs:200`), so a screen-owned handle would orphan a playing sound on
  every left/right colony cycle.
- **`PlayAmbience` is synchronous**, a near-copy of `PlayMusic`. With `PlaySfxAsync`, a `Stop()`
  that beats the queued play is a no-op — `Audio` is still null — and the worker then calls
  `OnInstanceLoaded` on a handle nobody holds, leaving a 25 s bed with no way to stop it. That is
  the hazard in the wedged-handle entry below.
- **It is an effect, not music**: gated on `CantPlaySfx`, and `AudioConfig.SetVolume` already gave
  the category the effects volume because its name has no "Music" in it. Both halves now agree, so
  the master FX slider is the single control. Birds and waves are not a score.
- **Not looped.** The beds run 22–28 s and are not authored to loop seamlessly. Restart-on-stopped
  is the idiom `ScreenManager.StartMusic` uses, but it is also the mechanism indicted below, so it
  was left out rather than added blind. A real loop flag on `NAudioSampleInstance` is the correct
  follow-up if the silence after one play is felt.

**Mapping shipped in `PlanetTypes.yaml`** — 36 of 43 vanilla types, and 37 of 44 in Combined Arms,
whose own `PlanetTypes.yaml` fully replaces vanilla's and so must ship with it:

| `PlanetCategory` | Cues |
| --- | --- |
| Barren | `sd_planet_barren_01`, `sd_planet_gasgiant_01` |
| Desert, Tundra, Ice | `sd_planet_barren_01` |
| Terran, Swamp, Steppe | `sd_planet_forest_01` |
| Oceanic | `sd_planet_water_01` |
| Volcanic | `sd_planet_volcanic_01` |
| GasGiant | none - silent by design |

**Barren is the one type with two cues**, picked between at random, which is what the array
shape is for. It is also the case that gives the keep-rule teeth: a Barren colony playing the
gas giant bed keeps it when you arrow to another Barren, but must swap when you arrow to a
Desert, because Desert lists only `sd_planet_barren_01` - even though the two share that cue.
`MovingColoniesChecksThePlayingCueAgainstTheNewList` is exactly that move.

**`sd_planet_colonized_01` is the one cue still unused** (Gilad's call). It has no natural
trigger here: a `ColonyScreen` is only ever built for a planet you own
(`UniverseScreen.Camera.cs:123`), so an "owned planet overrides terrain" rule would fire on
every single planet and make the terrain mapping dead. If it is ever wanted, it can be appended
to a type's array the same way the gas giant bed was.

**Known behaviour, decided rather than overlooked:**

- A cue that fails to start still latches. `SetPlanetAmbience` records the cue before playing, so
  if `PlayAmbience` returns `DoNotPlay` — effects at zero, engine down, missing file — that colony
  stays silent until the player moves to one with a *different* cue. Latching only on success
  would instead retry a genuinely broken cue every frame, which is worse.
- Opening the ground-combat screen for the same planet stops the bed, because the trigger is
  `workersPanel is ColonyScreen`. Widening it to `PlanetScreen` with an owner check would cover
  both screens if that is preferred.
- `PlayAmbience` duplicates `PlayMusic` apart from the gate. Left as-is on purpose: collapsing them
  would edit a working music path to satisfy a style point, and in this repo the code written to
  satisfy a review is reliably where the next regression comes from.

**Still XACT-era and never heard in this engine:** the six effects carry `Volume: 1.76` and `1.04`.
Retune when the mix is judged.

---

## Priority 2 — `InFlightCue` — FIXED 2026-09-25, and the shape of the fix is load-bearing

`[settled]` Found and fixed 2026-09-25 while reviewing the Combined Arms sound rework. Kept here
because the four problems below are why it was not the one-line change it looks like, and because
every guard the fix added is easy to mistake for noise and delete.

**What was wrong.** `Projectile.Initialize` tested the destination instead of the source:

```csharp
if (cueName.NotEmpty())     DieCueName  = cueName;             // tests the source, correctly
if (InFlightCue.NotEmpty()) InFlightCue = Weapon.InFlightCue;  // tests the destination: always ""
```

`InFlightCue` is a field initialized to `""` and assigned nowhere else, so the guard was never
true and the assignment had **never run**. Every `<InFlightCue>` in content was inert: 23 vanilla
weapon xml and 66 in Combined Arms. This was proved in the running game rather than argued from
the code — a temporary probe, firing a vanilla RocketLauncher on screen, logged
`playSound=True inFrustum=True` with `guardPasses=False`, which ruled out the camera as the
explanation and showed `PlaySfxAsync` being handed `""` on every frame of the flight.
(The `DieCueName` line above it reads the source correctly but is still confined to the
`playSound && inFrustum` block — see item 4 and entry 15 under "Everything else".)

**Why the one-liner was written, reverted, and then rewritten.** Four problems, all verified, and
what the shipped fix does about each:

1. **It restarts, and the engine has no looping.** The poll re-triggers whenever the audio handle
   stops, and it sat *above* the `if (InFrustum)` guard, so it ran for every projectile on the map
   every frame. `sd_weapon_rocket_flight_01` is about a second against a 24 s missile.
   → **Fixed by** moving the poll inside the existing `if (InFrustum)` block — which also puts it
   after the `Duration` expiry and `Die()` return, so a projectile that dies this frame no longer
   starts a sound it will never own — and by passing `InFlightSfxReplayTimeout` (0.5 s) to the
   `replayTimeout` parameter `AudioHandle.PlaySfxAsync` already takes (`Ship_Warp.cs:189` is the
   house precedent, with 4 s). The restart is still a restart, not a loop; see below.
2. **The poll must test `IsDisposed`, not `IsStopped`.** This is the subtle one and it defeats
   the cap in item 3 if you get it wrong. `AudioCategory.TrackInstance` adds a tracked entry and
   increments `NumActiveInstances` per *play*, but the entry stores the **handle**, and
   `AudioCategory.Update` collects it only when the handle's *current* instance reports
   `IsDisposed || CanBeDisposed`. Replay on `IsStopped` and there is a window — instance 1 ends
   on the mixer thread, the sim worker polls, the enqueue thread loads instance 2 into the same
   handle — in which entry 1 is still in the list but now asks instance 2 and answers "still
   playing". Every replay that wins that race leaks a phantom entry against both the per-effect
   cap and the category's 64. `IsDisposed` is `Audio == null`, which is only true before the first
   play and after `RemoveTrackedInstance` has disposed the handle, so at most one entry per handle
   can exist. `AsyncPlayStarted` already covers the enqueue window. Cost: about one frame of extra
   silence between repeats.
3. **It starves the `Weapons` category.** That category is `MaxConcurrentSounds: 64`,
   `MaxConcurrentSoundsPerEffect: 32` and `MaxSoundsPerFrame: 2` over 59 live effects that include
   every fire *and impact* cue. Flight cues are its first long-lived members, so a few Hellstorm
   volleys would exhaust the budget and silence lasers, cannons and shield impacts.
   → **Fixed by** a new per-effect `SoundEffect.MaxConcurrent`, set to 8 on the three flight cues.
   **Not** by giving flight cues their own category, which is the obvious move and is unsafe:
   **the Star Trek mod redefines those same ids inside its own `Weapons` block**, so moving an id
   to a new category in vanilla leaves the same id in two categories after the merge, and the
   `AudioConfig` constructor throws — killing all audio for that mod. Adding a category is safe;
   moving an existing id into one is not. A per-effect field merges cleanly, and the mod inherits
   the cap because `MergeNodes` merges field-by-field on `Id`. Note `MaxSoundsPerFrame: 2` is
   checked first and is the tightest limit of the three: at 20 fps in a barrage the whole category
   gets 40 starts per second, and flight restarts take a real share of them.
4. **`playSound` means "play the FIRE cue once for this volley"**, not "this projectile is
   audible" — `Weapon.SpawnSalvo` sets `playSound = false` after the first and MIRV uses `i == 0`.
   Assigning the cue inside that block would have left one warhead in a cluster whooshing and the
   rest silent, and every missile in a loaded savegame permanently silent. → **Fixed by** moving
   the assignment out of the `playSound && inFrustum` block entirely, next to `ModelPath`, so it
   also runs for `OnDeserialized`, which passes `playSound: false`.

**Still open: there is no real looping.** The restart-on-stopped poll is what ships, so a short
cue under a long flight is audibly a restart. Planet ambience (Priority 1) needs the same missing
primitive; solve it once, for both.

**Two zoom thresholds, and they are not the same one.** `GameObject.IsInFrustum(screen)` — the
local `inFrustum` in `Initialize`, which gates the *fire* cue — is `IsSystemViewOrCloser`, true
out to `CamPos.Z` 250,000. The `InFrustum` **flag** that the flight poll reads is set only in
`UniverseObjectManager.UpdateVisibleObjects` under `if (UState.IsPlanetViewOrCloser)`, i.e.
`CamPos.Z` <= 35,000. That happens to be exactly `ProjectileSfxDistance`, so the flag and
audibility line up and the flight poll has no dead band; the `replayTimeout` throttles retries
*within* that range, for projectiles far to the side or near the Z ceiling. Fire and impact cues
do have the dead band: they are attempted out to 250,000 and are silent past 35,000. So when a
report says **"I hear no weapon sounds", check the zoom first** — the listener is `CamPos`
including Z and falloff is linear over `MaxDistance`, so at 17,500 everything is already at half
volume, and flight cues cut out earlier than anything else.

---

## Priority 3 — two reviews Gilad asked for, 2026-09-26

### 1. Explosion proximity, and whether adjacent ships take any of it — SHIPPED

`[done]` Gilad reported that ships touching each other all exploded at once, titans included.
Reproduced, measured and fixed. `UnitTests/Ships/AdjacentShipExplosionTests.cs` pins each part.

**The one cross-hull path is ship death**, for vanilla and Combined Arms.
`SpatialManager.ShipExplode` is it. A projectile can splash other ships only through the
`radius >= 256f` branch in `SpatialManager.cs`, and nothing in either of those two reaches it: the
highest damage radius any weapon can hit fully teched is 203 in vanilla and 203 in Combined Arms,
out of 35 and 125 explosive weapons. **The bundled Star Trek mod does reach it** -
`Fed_HeavyPhotonTorpedo_Mk1.xml` has `ExplosionRadius` 950 - so that branch is live content, not
dead code, and must not be deleted. RedFox's `62a4b333a` (2022-05-18) replaced "every exploding
projectile splashes everything nearby" with that gate and called it "very rare". Not a migration loss.

**Three things made one death wipe a formation, all now fixed:**

- The blast **entered on whichever module was geometrically nearest**, which is an *internal* one
  as soon as the hulls overlap — at dead centre it detonated inside a `LargeOrdStorage`, killing
  183 internals while the armour absorbed nothing. `Ship.FindBlastEntryModule` now traces a ray
  from the blast towards the victim's centre and enters through the plate facing it, falling back
  to the ship's own facing when the two positions coincide exactly.
- The falloff was `(1-d/R)²` against a radius that **grew with the damage**, so a bigger blast
  never actually fell off more — it still delivered two thirds at contact. Now
  `ShipModule.ExplosionFalloff`: `100/(100+d)` with `d` floored at 10, so 0.91 at the centre,
  half at 100 units, and a longer tail (at 1100u it delivers 5033 of a 60k blast where the old
  curve gave 483).
- The blast had a floor and **no ceiling**. `Ship.ExplosionDamageCap` adds one per hull role,
  mirroring `ExplosionEvadeBaseChance`: 15x radius for drone/scout/fighter up to 100x for
  capital/station. Small hulls carry reactors and ordnance out of all proportion to what they can
  survive, and it is those, not warships, that actually chain.

**The population that chains is not warships.** A ship shot to death dies when its *internal*
slots drop below `ShipDestroyThreshold`, and resist armour is external, so combat never strips it —
a Dreadnought dies with 144 of its 191 plates still alive and blasts at exactly the `Radius*10`
floor. The ships that produce a large blast are the **135 vanilla designs carrying no
`ExplosiveResist` module at all**: nothing subtracts, so they emit 15x to 34x their own floor at
full health. Freighters (24), corvettes (21), supply (18), troop (17), fighters (16), stations (9),
scouts, construction, colony, platforms. Exactly the ships that pile up at a planet or a shipyard.

Measured, 8 stacked at 8 units with shields down, averaged over 20 trials:

| stack | before | after |
| --- | --- | --- |
| Dreadnought, armour-stripped victim | 7 of 7 destroyed | **0 of 7** |
| Seeder Transport (freighter) | 7 of 7 | **0 of 7** |
| Supply Shuttle | 5.0 of 7 | **0 of 7** |
| Corsair | 7 of 7 | **0 of 7** |
| Shipyard among 6 freighters | 6 of 6 | 3.7 of 6 |

**Still open, deliberately:** a station taking its docked craft with it. A Shipyard's cap is 100x
radius = 28,644 against 17-module freighters, and that reads as correct rather than broken.

**Do not "fix" these back:**

- **Evade compounds against you in a chain.** Raising a hull's evade barely moves a stacked
  outcome, because every explosion in the cascade rolls fresh: a 75% shuttle survives one blast
  80% of the time and seven of them ~21%. Gilad's 2026-09-26 pass raised the tiers and took
  point-blank from `*0.1` to `*0.25`; that is aimed at the single-blast case, not at stacks.
- `ExplosionEvadeBaseChance` has its `default:` label grouped with `drone`/`scout`, so every hull
  role the switch does not name inherits the **scout** value — `destroyer`, `gunboat`, `carrier`,
  `bomber`, `troopShip`, `freighter`, `platform`, `construction` and the rest.
  `ExplosionDamageCap` keeps the same `default:` placement on purpose so the two tables read
  alike, though it is not a row-for-row copy: `fighter` has its own evade tier (75) but shares the
  drone/scout cap multiplier. Whether a destroyer should really dodge better than a cruiser is a
  separate question, as is a Level 10 freighter reaching 100 evade outside point-blank, which
  `RollDice` treats as total immunity.
- The **visual** is not the damage. `ExplosionRadiusVisual` defaults to 4.5 and **no weapon in
  vanilla or Combined Arms overrides it**, and the sprite is drawn with that as its diameter, so a
  projectile's fireball is 2.25x its damage radius. A ship's is `2 * Radius * roleMult`. Ships will
  always look engulfed by more than actually hurts them.

**Two genuinely wrong things left, both out of scope for this pass:**

- `ShipModule.GetExplosionDamageOnShipExplode` subtracts `Health / (1 - ExplosiveResist)` — a
  *health* figure — from a damage total. Health dominates, so `SteelArmorLarge` at **4%** resist
  cancels 26,042 while `Reinforced Bulkhead` at **75%** cancels 14,400. Over a Dreadnought it
  totals 3,834,300 against 320,603 of positive terms, which is why the blast is a 29x cliff rather
  than a slope. Nothing guards `resist >= 1`: Star Trek's `AncientArmor_3x3` ships **1.39**, so the
  term flips sign and *adds* 255,769 per plate, and on the receiving side
  `InternalDamageModifier` returns `-0.39`, which **repairs** the plate instead of damaging it.
- Reactors are supercritical against each other. `Extreme Fusion Reactor` is 1,550 health and
  detonates for 6,000 over 72 units, roughly 4x the health of the reactor beside it, fired
  synchronously from `ShipModule.Die`. One `AntiMatterReactor` dying takes 34 modules of a Heavy
  Carrier with it.

### 2. Show enemy strength per system in the planet list, from the threat matrix

`[open]` Not investigated. Logged with anchors so the next pass starts from code.

The player has no compact answer to "where is the enemy strong". The data already exists:
`ThreatMatrix.GetHostileStrengthAt(Vector2 pos, float radius)` (`ThreatMatrix.cs:139`), with
per-empire and research-station-excluding variants beside it at `:131` and `:145`.

The natural surface is the planet list screen - `PlanetListScreen.cs` and its row type
`PlanetListScreenItem.cs` - adding a strength column fed by `GetHostileStrengthAt` at the system
position. Points to settle: which radius counts as "in this system"; whether to show raw strength
or a banded icon, since raw numbers mean little without a fleet to compare against; and above all
that it must show only what the player **knows**, so it has to come from the threat matrix rather
than from live empire data, or it leaks scouting the player has not done.

---

## Do not "fix" these

Settled behaviour that reads like a bug to fresh eyes. Each was decided deliberately and the
reasoning is in the linked notes or the commit that set it.

- **Biosphere capacity build/scrap rules** — shipped in `933196b4f`, issue #321. The tax-rate EMA
  was never implemented and `NetRevenueGain` was deleted with it. Do not reintroduce either.
- **Colony trade states** — the young-colony STORE rule fixing #314. Three rules there must not be
  re-derived.
- **Governor scrap rules and the exclusive-blueprints override** — terraformers sit outside the
  plan on purpose.
- **Station dropdown healing**, **box-selection modifiers**, **crash-site ownership gating** (only
  when `IsCrashSiteActive`), **refit port quality** (0.5 prioritized, 1 fallback).
- **Budget #14 below** — the biosphere payback heuristic omits `ExoticCreditsBonus` deliberately.
- The AI gets half production tax on cybernetic colonies and the player does not
  (`ColonyResource.cs:222`); `ResearchTaxMultiplier` is difficulty-only and always 1 for the player
  (`UniverseGenerator.cs:232`). Both intended.

## Worth a GitHub issue if we ever file any

Player-visible, self-contained, and safe to hand to someone else: power #1, damage #2, misc #6,
misc #1. Everything else is better done by us or not at all.

---

## Power (7)

From the `design_power_budget` Codex entry. Items 1, 2, 4, 5 confirmed by two fact-check passes;
item 3 is one reading, untested. Items 2, 5, 6 and 7 shipped in `3037ebbf7`; 1 and 4 followed.
**Every item in this list is now resolved.**

**When judging items 1 and 4, ask which side was lying.** In both the design screen was
correct and the running ship was generous - recharging faster in 1, firing cheaper in 4 - so
the fix makes the ship honour the spec sheet the player already designed against. Nothing anyone
built to the readout becomes wrong. The only exposure is a design tuned by in-game feel rather
than by the numbers, which will now behave as its own screen always described. A display that
*understates* cost or *overstates* capability is the case that deserves alarm, and neither of
these was that.

1. ~~Reactor tech bonus applied twice.~~ Resolved. `ShipModule.ActualPowerFlowMax` already
   multiplies by `EmpireHullBonuses.PowerFlowMod` and `Power.Calculate` sums that into
   `Ship.PowerFlowMax`, so `UpdatePower` adding `PowerFlowMax * data.PowerFlowMod` a second time
   made live recharge (1+mod)² while the design screen showed (1+mod). The design screen was the
   correct one, so `UpdatePower` now just uses `PowerFlowMax`.
   The magnitude is large late game - vanilla ships 10 "Power Flow Bonus" techs totalling
   **0.81**, so a fully researched empire was recharging at 3.28x base where its own design
   screen said 1.81x, 81% more than displayed - but see the note above the list: the screen was
   the number everyone designed against, so ships now do what their spec sheet promised. Early
   on it hides well, since one 0.07 tech is only a 7% gap.
   Safe because module `Bonuses` is a shared `EmpireHullBonuses` instance that
   `RefreshBonuses` mutates **in place** on every tech unlock, so `ActualPowerFlowMax` is always
   current - the second application was not quietly keeping anything up to date.
2. ~~Pwr Dmg and Siphon only work on beams.~~ Half resolved `3037ebbf7`: **power damage now applies
   to projectiles too**, via `CausePowerDamage(Projectile)` in the projectile branch of
   `TryDamageModule`. It is applied *before* the deflection return and with no threshold test, so a
   shot too weak to hurt the module still drains the ship - deliberate, and deliberately unlike EMP,
   which must beat the module deflection. It also drains once per module the shot touches along the
   armour-piercing walk, which the Codex now states.
   **Siphon stays beam-only by design - do not "fix" it.** The Siphon row is no longer drawn for
   non-beam weapons (`ModuleSelection.cs`), so REAegis, EmpCannon, EmpDischarger1x2 and
   DualEmpCannon stop advertising a value that does nothing.
3. ~~Destroyed reactors and conduits keep powering the grid.~~ Resolved. `PowerGrid.Recalculate`
   distributed from every module with a PowerRadius and flooded every conduit without checking
   `Active`, while `Power.Calculate` *did* check it - so a dead reactor stopped adding flow while
   its neighbours stayed `Powered`, and the `ShouldRecalculatePower` that `OnModuleDeath` sets
   recomputed the same wrong answer. Two guards now: the distribution loop, and
   `GetNeighbouringConduits` so a dead conduit stops relaying down the chain.
   **Losing power is not only "unpowered"**, which is the weight behind the `[balance]` tag:
   `ShipModule.GetActualMass` returns the absolute value for negative-mass modules when not
   `Powered`, and `ShipStats.InitializeMass` sums every module without filtering on `Active`, so a
   ship that loses its reactors also gets **heavier and slower**; self regeneration drops to a
   tenth as well. Wants in-game time before release.
   The design screen runs the same `Recalculate` through `DesignShip`
   (`ShipInfoOverlayComponent.ShowShip`) and is unaffected, because design modules come from
   `CreateNoParent`, which sets `Active`. The `designModule: true` bypass in `Power.Calculate` is
   a different path - it is handed raw `ResourceManager` templates, which are not `Active`.
   Save loading is safe too: `ShipModule.Create` assigns `Active` from slot health in the
   `OnDeserialized` module loop, before `InitializeStatus` reaches any `RecalculatePower`.
4. ~~Multi-projectile weapons fired without paying for every projectile.~~ Resolved, **and the
   first attempt fixed the wrong side.** The intended rule is that a shot costs
   `cost x ProjectileCount x SalvoCount`: a 100 power gun firing 2 projectiles costs 200, and 600
   over 3 salvos. The design screen already did that. The **runtime** did not - `PrepareToFire` and
   `PrepareToFireSalvo` each charged the listed price once however many projectiles the shot
   spawned, so multi-projectile weapons fired far cheaper than their own spec sheet said.
   It was first "fixed" by stripping `ProjectileCount` out of the three display properties, on the
   assumption that the runtime was the truth. It is not: **the display was right and the game was
   wrong**, which is the direction that misleads. `Weapon` now has `PowerPerShot` and
   `OrdnancePerShot`, used by the two charge sites *and* by `CanFireWeapon` - without the last one
   a weapon would fire a volley it could only half afford and drive the store negative.
   `SalvoCount` was never the problem: `FireAtTarget` charges once and queues `SalvoCount - 1` more,
   each charging again, so a trigger pull costs exactly `SalvoCount` shots.
   The magnitude is real - roughly 29 weapons across vanilla and the two shipped mods - though
   again the screen already charged this, so budgeted designs are unaffected.
   Vanilla: REAegis 800 -> 2400 power per shot, DualFlak 2.25 -> 6.75 ordnance, PlanetFlak
   0.5 -> 2.0. Combined Arms is hit hardest because shotgun-style weapons are a signature of it -
   REEmpProjector 75 -> 750 power at 10 projectiles, AMProjector 175 -> 525, and the whole
   Shot/Fletchette/Gatling family 2x to 5x on ordnance.
   **MIRV warheads are not affected and must not be counted** when judging the impact: the eleven
   `*Mirv` weapons are spawned by `Projectile` through `SpawnMirvSalvo`, which never calls
   `PrepareToFire`, so their cost has never been charged and still is not. A first reading of the
   numbers made this look far worse than it is - ClusterMirv appears to jump 0.57 -> 25.65 ordnance
   and never pays either figure.
5. ~~Siphon gives full value regardless.~~ Resolved `3037ebbf7`. It now transfers only what it
   drained. Note `AddPower` still clamps at `PowerStoreMax`, so the attacker can gain less than it
   drained; the Codex says "adds what it drained", which slightly overstates that edge.
6. ~~`beamModifier` was accepted and then dropped.~~ Resolved `3037ebbf7`, and the shape matters.
   `Beam.Touch` computes it as `timeStep.FixedTime * 60` to hold a continuous beam's damage per
   second constant, and pre-multiplies the raw damage with it - but `ShipModule.Damage` forwarded
   the damage and not the modifier, so every beam special effect ran at a hardcoded 1. It is
   invisible at the default 60 sim FPS and 1x speed, where the modifier IS 1; it bites because the
   sim **throttles its own rate down to a floor of 10** when turns run long, which is exactly the
   big battle where beams matter, and the rate is user-settable 10-120.
   **Repulsion is the exception and must NOT be scaled.** `CauseRepulsionDamage` calls `ApplyForce`,
   and force is already integrated against the step (`a = F/m`, then `v += a*dt`), so scaling it
   again made repulsion beams 6x stronger at 10 sim FPS and half strength at 120. Siphon, power
   damage, troop and tractor are per-tick accumulators and do need it.
7. ~~`CalculateOffense` ignored projectile power damage.~~ Resolved: the AI rated every projectile
   weapon carrying the stat as if it did nothing. Weighted at **0.75**, derived from the beam
   branch, which prices `PowerDamage` at 45 where it prices `DamageAmount` at 60; the `EMPDamage`
   line beside it uses 0.5. `SalvoCount * ProjectileCount` is correct here - each projectile that
   lands drains separately - and is *not* the inverse of item 4, where the cost is charged once per
   shot.

**Open, and it is content, not code.** Mod weapons carry power damage values authored while the
stat was dead, so nobody balanced them. `game/Mods/Combined Arms/Weapons/Planet/IonDefenseCannon.xml`
is **100000**, enough to clamp any ship's store to zero on every hit and lock out energy weapons and
warp for anything in range of a defended planet; `Magnetrom.xml` is 4000 **on an explosive
projectile**, so it applies per module in the blast. The AI blast radius is larger than the gameplay
one: through `Building.Offense` that Ion Cannon rates ~38x higher, which swamps every fleet-strength
comparison the invasion planner makes, so the AI would simply stop invading Combined Arms planets.
Vanilla is tame by comparison - the four affected weapons move 1.4x to 2.7x. **The mod values must
be revisited before any release that carries `3037ebbf7`.**

Two sibling inconsistencies left alone on purpose: `Building.Offense` is `[StarData]` and only
recomputed when it is exactly 0 or the planet levels up, so a save made before this change keeps the
old rating for weapon buildings until then; and `WeaponTemplate.GetDamagePerSecond` (~224) treats
`PowerDamage` as a *fallback* for a zero `DamageAmount` rather than an addition, so the DPS views
still read pre-fix while the offense rating reads post-fix.

## Damage, shields and weapons (18)

From the `design_armor_and_shields` entry. Code reading plus one fact-check pass, untested.

1. `[latent]` **Weapon-tag armor/shield damage bonuses never applied.** `Weapon.AddModifiers`
   (`Weapon.cs` ~613) accumulates `ArmorDamageBonus` / `ShieldDamageBonus` and nothing reads them.
   No stock or mod tech uses those bonus types today.
2. `[crash]` **100% resistance divides by zero.** `ShipModule.Damage` (~812) does
   `absorbedDamage /= damageModifier` when the modifier is ≤ 1; a modifier of 0 yields NaN, then
   `(int)NaN` for the remainder.
3. `[balance]` **Module-death blasts run through the killer's weapon.** `ShipModule.Die` passes the
   killing projectile as the source, so every module in the blast takes that weapon's
   EffectVsArmor, resistances, deflection roll and EMP — `CauseEmpDamage` once per module.
4. `[balance]` **Radial blasts hit big modules repeatedly.** `Ship.DamageExplosive`
   (`Ship_ModuleGrid.cs` ~525) hits a multi-cell module once per covered cell with no dedupe; the
   directional version already dedupes via `SplashHitScratch`.
5. **Module-death blast root is wasted** — the blast centres on the dying, already-inactive module,
   so the full-damage root slot yields nothing and every direction starts at 25%. Decide whether
   that is intended.
6. `[balance]` **Shield bubble spends armor piercing.** `Projectile.TryPhaseThroughModule` subtracts
   the shield module's width + APResist when the shot hits the bubble.
7. **Remainder truncated to int** — `damageRemainder = (int)(...)` in `ShipModule.Damage` drops
   fractional carry-on damage.
8. **Planet repair without an owner check (unconfirmed)** — `Ship_Repair.cs` ~59 repairs from any
   orbited or tethered planet. Check whether orbiting an enemy or neutral planet repairs.
9. `[balance]` **`IsCoveredByShield` picks the last shield, not the strongest** —
   `Ship_ModuleGrid.cs` ~115, `maxPower` is never updated inside the loop.
10. `[latent]` **Hull bonuses are off everywhere.** `UseHullBonuses` is false in vanilla and every
    shipped mod, so hull `ArmoredBonus` and `ShieldModifier` are dead. The Codex omits them.
11. `[latent]` **Missile and `MaxWeaponError` aim use the raw crew level.** `MissileAI.cs` ~258
    passes the missile's Level with no square, FCS or trait; `Ship.cs` ~1019 `MaxWeaponError` does
    the same and is never read.
12. `[balance]` **Jammed missiles re-target every frame.** `MissileAI.MoveTowardsTargetJammed` sets
    `Target = null` each call, so the missile re-picks roughly every 0.15 s and carries its
    Jammed/FixedError state across.
13. `[display]` **Shield penetration, screen versus combat.** The screen sums empire bonus + base +
    every tag (`ModuleSelection.cs` ~589); combat takes the max of (tag + base) with the empire
    bonus as a floor (`Weapon.cs` ~593, ~616). The screen overstates, and a weapon with no tags
    gets no base chance in combat.
14. `[latent]` **Hull fire-rate bonus never applies in combat.** `Weapon.FireDelay` is a `new`
    property carrying the hull bonus, but cooldowns use `NetFireDelay` from the template
    (`WeaponTemplateWrapper.cs` ~147). Moot while item 10 holds.
15. `[display]` **Screen-only weapon tag bonuses.** `tag.Rate` lengthens the screen's Delay (a rate
    bonus reading as slower) and combat never reads it; tag `ArmourPenetration` applies in combat
    but not on the screen.
16. `[latent]` **Ship-mounted repair beams repair nothing.** Only DroneBeam repairs (`Beam.cs` ~443)
    and ship beams with negative damage skip collisions (~136). No content sets `IsRepairBeam`.
17. `[display]` **DPS uses the unclamped `ProjectileCount`** — `ModuleSelection.cs` ~533.
18. `[display]` **Blast radius on screen is the base value** — the `ExplosionRadius` tag bonus
    (Plasma Ordnance) and the ordnance bonus enlarge it in combat.

## Budget, money and espionage (14)

From the budget screen entry (bucket 6). The Codex text describes what the code actually does, so
fixing any of these needs a Codex impact pass.

1. `[balance]` **Leeched money looks double-credited.** `Espionage.AddLeechedMoney` calls
   `Owner.AddMoney` immediately (`Espionage.cs:262`) and the same amount lands in
   `TotalMoneyLeechedLastTurn` (`Empire_Espionage.cs:36`), a term of `GrossIncome`
   (`Empire.cs:195`), added again by `AddMoney(MoneyAfterLeech(NetIncome))` (`Empire.cs:1365`).
2. `[balance]` **Planet troop upkeep cancels out and is never charged.**
   `TotalBuildingMaintenance` subtracts `TroopCostOnPlanets` (`Empire.cs:197`) and `AllSpending`
   adds it back (`:199`). Garrisoned troops cost nothing.
3. `[display]` **Troop upkeep computed two ways.** `GetTroopMaintThisTurn()` counts only our troops
   (`Empire.cs:1283`); `TroopCostOnPlanets` counts every troop on the planet including enemies
   (`ColonyResource.cs:324`), so Expenditure rows stop summing to their own total during an
   invasion.
4. **Dead treasury label.** `TreasurySliderOnChange` writes `TreasuryGoal(Money)/2` into the slider
   text (`BudgetScreen.cs:197`); `Update()` overwrites it with the undivided `ProjectedMoney` next
   frame (`:244`). The same method assigns `data.treasuryGoal` twice.
5. **`TreasuryGoal(float normalizedMoney)` never uses its parameter** —
   `EmpireAI.RunEconomicPlanner.cs:205`.
6. `[balance]` **Credits multiplier applied twice.** `ChargeCreditsHomeDefense` pre-multiplies by
   `CreditsMultiplier` and `ChargeCredits` multiplies again inside `ProductionCreditCost`
   (`Empire.cs:2593`, `2620`, `2643`). `RefundCreditsPostRemoval(Building)` has the same shape
   while the ship overload passes the raw cost, so buildings and ships refund on different scales.
7. **Player `SpyBudget` is not money under the new espionage system** — it stores the raw weight
   fraction (~0.0024) rather than credits (`RunEconomicPlanner.cs:123`).
8. **Budget weights depend on yaml key order.** `Spy` writes `budgets[Spy] = 25` and a later
   `Espionage` key overwrites it with 0 or 1 (`BudgetPriorities.cs:57`). If `Espionage` were listed
   first, every budget fraction in the game would change.
9. `[balance]` **Legacy espionage silently doubles the governor budgets** — with that rule option
   on, the Spy weight of 25 is real and half of Build+Spy is redistributed, moving the player's
   colony share from about 1/38 to 1/19 of the treasury goal (`RunEconomicPlanner.cs:109`).
10. `[display]` **Trade panel rows and total read different lists** — rows iterate cached
    `TradeRelations`, the footer iterates `ActiveRelations` live (`Empire_Trade.cs:41`, `460`).
11. `[display]` **Lifetime trade average truncates twice** — `AllTimeTradeIncome += (int)taxedGoods`
    per delivery and `AverageTradeIncome` is integer division (`Empire_Trade.cs:77`, `33`), so
    sub-credit deliveries never reach the Mercantilism (Avg) figure.
12. `[latent]` **`Building.MoneyBuildingAndProfitable` never runs on any AI colony.** Its only
    caller is `SuitableForScrap` (`Planet_EvaluateBuildings.cs:512`), four lines below
    `if (!RequiredInBlueprints(b)) return true; else if (!overBudget) return false;`.
    `RequiredInBlueprints` is `Blueprints?.IsRequired(b) == true` and the only production caller of
    `AddBlueprints` is a player-only button (`GovernorDetailsComponent.cs:409`), so no AI colony
    has blueprints and every building returns at that first line. **Four guards are stranded**
    there: `MoneyBuildingAndProfitable`, `WillMaintainPositiveFoodOutput` (`:524`),
    `IsBuildingOnHabitableTile`, and the `scrapZeroMaintenance` / `IsStorageWasted` pair. The
    early-out arrived in `de8f49ab4` (2024-05-31); the same commit stranded
    `b.IsPlayerAdded && OwnerIsPlayer`, which is exactly issue #303 — already paid for once.
    Mitigation: `CalcBuildingScore` (`:586`) weights the money terms, so `ChooseWorstBuilding`
    rarely picks a good money building anyway. **The starvation guard is the one with no substitute
    in the scoring.**
13. `[latent]` **And its arithmetic is wrong where it does run** (`Building.cs:460`):
    `grossProfit = PlusTaxPercentage * pop + CreditsPerColonist * pop`. `Income` is ignored though
    `IsMoneyBuilding` counts it; `PlusTaxPercentage * pop` is not the marginal revenue (tax
    percentage multiplies the colony's whole rate); it prices at a 100% tax rate, overstating
    profit by roughly 1/TaxRate, typically 2–4×; `ExoticCreditsBonus` is missing.
    **`ColonyMoney.NetCostOf(Building)` already has the correct arithmetic** — a before/after gross
    revenue delta, which is the only form that catches the cross term when a building has both
    `CreditsPerColonist` and `PlusTaxPercentage` (Capital City does). Call it rather than write the
    model a third time.
    **Order matters:** fixing 13 alone changes nothing while 12 keeps it unreachable; fixing 12
    alone hands the governor a rule computed at a fictitious 100% tax rate. Math first. A test must
    build a planet that is over budget **and** carries blueprints requiring the building, or the
    guard stays invisible.
    Also dormant in the same dead block: `WillMaintainPositiveFoodOutput` has a precedence bug —
    `x - y/x` where both branches read as though the intent was `(Fertility - delta) / Fertility`.
14. `[settled]` **The biosphere payback heuristic omits `ExoticCreditsBonus`**
    (`Planet_EvaluateBuildings.cs`, `BiosphereCarriesItsPopulation`). Left deliberately: the formula
    already uses `TaxRateMultiplier` rather than `TaxRate` so it is a "full rate" heuristic by
    design, `BiospherePaybackShare = 0.6` was tuned against it, the bonus is 1 for most empires, and
    the error is conservative. Retune the share and the term together or not at all.

## Everything else (17, two resolved)

1. `[balance]` **EMP recovery is a per-frame constant, unscaled by the time step.**
   `Ship.EmpRecovery` (`Ship.cs:385-392`) is applied once per update as
   `CauseEmpDamage(-EmpRecovery)` (`:1085`), guarded by `timeStep.FixedTime > 0` but never
   multiplied by it, so recovery follows update rate rather than game time. The Codex dodges this
   by saying only that EMP wears off "quick", with no number.
2. `[content]` **`UniqueInEmpire` is a dead building tag.** No C# reads it, yet six vanilla xml
   files set it (Imperial Bank, The Underhive and its three event buildings) and mods copy the
   pattern including Combined Arms' Capital City. Harmless because `Building.Unique` defaults true,
   but it reads as a working rule. Honour it in the loader or strip it.
3. **`Weapon.BaseTargetError` carries two dead parameters** — no caller passes `loyalty`, and
   `range` is passed but never read.
4. `[display]` **The design screen's Accuracy row ignores the Militaristic trait** —
   `ShipDesignStats.cs:70` and `ModuleSelection.cs:505` pass `TargetingAccuracy` as `level`,
   skipping the level-squared branch. A Militaristic empire's level-0 ships aim better than shown.
5. `[content]` **Ship category tooltips bake in a threshold a mod can change.**
   `ShipCategoryUnclassifiedTip` onward state 85 / 97.5 / 92.5 / 90 / 87.5 / 75 percent, which is
   `threshold * 0.5 + 0.5` for the shipped `ShipDestroyThreshold: 0.5`. Star Trek's `Globals.yaml`
   sets 0.4, so all seven are wrong there. The Codex names the setting instead of the numbers.
6. `[crash]` **`Empire.cs:2725` dereferences a `Find` result without a null check** —
   `data.AgentList.Find(a => a.TargetPlanetId == planetId)`. One `?.` next time the file is touched.
7. `[thread]` **The sim thread repopulates UI dropdowns.** `UniverseScreen.Events.cs:14`
   `OnPlayerBuildableShipsUpdated` reaches `AutomationWindow.UpdateDropDowns` → `InitDropOptions`,
   which clears and refills `DropOptions` and writes `EmpireData` strings while the UI thread may
   be drawing them. Pre-existing.
8. ~~The colony screen tints biospheres by the old tax rule.~~ Resolved `a5c45f34b`.
9. `[content]` **`Biospheres.xml` carries a dead `MaxPopIncrease` of 100.** `UpdateMaxPopulation`
   excludes biospheres from `PopulationBonus`, so it raises no cap. It survives only because the
   template overrides `ShortDescriptionIndex`, suppressing the auto-generated "+0.1 Max Pop" line —
   any mod dropping that override shows a false claim, and it already misled a contributor into
   double-counting. Delete the field.
10. **`CanRepairOrHeal()` is a dice roll, not a predicate** — `Planet.cs:909`,
    `BombingIntensity == 0 || Random.RollDice(100 - BombingIntensity)`. It reads like a query, so
    calling it twice in a turn squares the probability. Caught while reviewing a proposed
    `GrowPopulation` extraction that would have done exactly that. Rename it, or split the roll
    from the test.
11. ~~An exclusive big plan can lock a colony out of terraforming.~~ Resolved `300dc9ad3`.
12. `[latent]` **`ResourceManager.BlueprintsValid` only checks that each planned building exists.**
    It does not check `IsSuitableForBlueprints`, so a hand-edited AppData yaml can plan a
    non-unique building. `UpdateCompletion` counts instances against a HashSet of names, so several
    of one pushes `PercentCompleted` past 100 and `Completed => PercentCompleted == 100` never
    fires, silently breaking the linked-blueprint chain. Not reachable through the UI.
13. `[display]` **The colony-tile terraform icon uses the wrong unlock test.**
    `EmpireManagementScreen.cs:277` guards on `IsBuildingUnlocked(TerraformerId)` while the governor
    asks `Empire.CanTerraformPlanetTiles` (unlocked **and** terraforming level ≥ 2). Between the two
    the screen marks tiles the empire cannot turn and the governor roofs with a biosphere instead.
    One-line fix whenever that screen is next touched.
14. **A third copy of the biosphere tile rule.** `AssignBuildingToTileOnColonize` and
    `AssignBuildingToTilePlanetCreation` both reach `AssignBuildingToRandomTile`
    (`Building.cs:397`), which special-cases biospheres but ignores `Terraformable`. Since
    `441802ecb` the governor and the colony build list share `Planet.PreferredBiosphereTile`; this
    one does not. It runs only at colonization and galaxy generation so it cannot contradict the
    others at runtime, but it is the third place the same idea is written.
15. `[latent]` **`DieCueName` is set only for the first projectile of a volley.** Same block as the
    `InFlightCue` bug above, one line up: it reads the source correctly but sits inside
    `if (playSound && inFrustum)` in `Projectile.Initialize`. `Weapon.SpawnSalvo` clears
    `playSound` after the first shot and `OnDeserialized` passes false, so warheads 2..N of every
    salvo and every projectile in a loaded savegame have no death cue. Deliberately left out of
    the `InFlightCue` fix: unlike the in-flight cue this one does play today for some projectiles,
    so turning it on for the rest is an audible balance change that wants its own listen.
16. `[latent]` **A loaded projectile's speed is squared.** `Projectile.OnDeserialized` calls
    `Initialize(Position, Velocity, ...)`, passing the restored `Velocity` where the parameter is
    `direction`, and `Initialize` does `SetInitialVelocity(Speed * direction)`. `Velocity` is
    `[StarData]` on `GameObject` and its magnitude is already about `Speed`, so the result is
    roughly `Speed` squared. `Duration` is saved and restored around the call, but velocity is
    not. Found while checking what else `Initialize` clobbers on the deserialization path.
17. `[latent]` `[thread]` **A flight cue can start after its projectile is gone.** `Projectile.Die`
    does `if (InFlightSfx.IsPlaying) InFlightSfx.Stop()`, but in the 1-15 ms between
    `PlaySfxAsync` queueing and `SfxEnqueueThread` draining, `IsPlaying` is true only through
    `AsyncPlayStarted` while `Audio` is still null - so `Stop()` nulls nothing and does not clear
    `AsyncPlayStarted`. The enqueue thread then calls `OnInstanceLoaded` anyway and the whoosh
    plays to completion from the dead projectile's frozen emitter, holding one of the cue's 8
    `MaxConcurrent` slots. Dead code until in-flight cues were switched on, live now. Audible
    impact is small - an explosion usually covers it - so it is logged rather than fixed; the fix
    shape is a cancelled flag on `AudioHandle` that `TrackInstance` honours, which touches shared
    audio plumbing and wants its own listen.

    Same root cause, second trigger, found on the branch review: `AsyncPlayStarted` is set before
    the enqueue and cleared only by `OnAsyncPlayComplete`, so any path that drops a queued item
    without draining it wedges the flag **true forever** - `Destroy()`'s queue clear, the
    null-safe `AsyncSfxQueue?.Add`, and the enqueue thread's `config == null` batch drop. Unplug a
    headset mid-battle and every projectile with a play in flight at that instant loses its cue
    for the rest of its life, and `IsPlaying` reports true forever so `Die()`'s `Stop()` is a
    no-op. Projectiles live seconds, so it is cosmetic and self-limiting - but a fix for the entry
    above should clear the flag on every drop path, not just on `Stop()`.

## Larger items with their own notes

**`GetBestPorts` lets a crippled Colony-type port through** — `Empire_RallyPlanets.cs:264-267`.
The filter parses as `(A && B && C) || D` because `&&` binds tighter, so a port whose CType is
Colony is admitted on D alone and escapes the `!IsCrippled` guard the line opens with. Narrow in
practice: prioritized ports are pre-filtered for `!IsCrippled`, so only the ordinary
`SafeSpacePorts` path can pick a crippled Colony as a build or refit target. Wants parentheses
around the whole CType branch. Not fixed because it changes which planets every build goal
considers — its own commit and its own test.

**The governor builds orbitals into a star's radiation zone** — `Planet.AddOrbital`
(`Planet_BuildDefenses.cs:176`) creates the `BuildOrbital` goal with no radiation or sun-distance
check; the only gate is `IsOutOfOrbitalsLimit` in its callers. `SolarSystem.ApplySolarRadiationDamage`
(`SolarSystem.cs:326`) then damages every ship in the system inside the radius, exempting only
`IsGuardian` — and orbitals are ships in that list. Reachable on ordinary maps: the danger radius is
`RadiationRadius + 2000` (neutron 17000, pulsar 22000, gargantua 27000) while the first planet ring
sits at `starRadius * 30` = 7500–15000. Two things make it worse than the planet's own orbit
suggests: `FindNewOrbitalLocation` picks a **random** angle up to ~6000 units out, so even a planet
outside the zone can have an orbital dropped inside it on the star side; and `TetherOffset` is fixed
in world space, so the structure's distance to the star swings by up to twice the offset as the
planet orbits. Suggested shape: have `FindNewOrbitalLocation` reject candidates failing
`SolarSystem.InSafeDistanceFromRadiation` — it already loops rings and angles, so it becomes a
condition on the existing search and places orbitals on the far side rather than denying them. A
planet deep inside the zone needs a check in `AddOrbital` itself.

**The save-overwrite check reads the visible list, not the filesystem — and that blocks an
otherwise obvious fix.** `GenericLoadSaveScreen.IsSaveOk()` walks `SavesSL.AllEntries` and returns
"safe to save" when no *listed* item matches the typed name. Today the damage is narrow, because
`SaveGameScreen.InitSaveList` lists every save whose header parses, so almost nothing is hidden —
only a save whose header will not parse at all can be silently clobbered.

It becomes serious the moment anyone filters that list. The obvious tidy-up is to make the save
screen filter like `LoadSaveScreen` does, on `Version == SaveGameVersion && ModName ==
GlobalStats.ModName`. Do that alone and a hidden save is no longer a listed item, so `IsSaveOk`
returns true, `DoSave()` runs with **no overwrite prompt**, and the file is destroyed. The likely
victim is not an old-version save but a same-name save from another mod: play vanilla, hide every
Combined Arms save, type a name you used there, and it is gone.

**So `IsSaveOk` must become filesystem-based (`File.Exists` on the target path) before the save
screen filters anything.** They are one change, not two. The better fix for the underlying
confusion is probably not to filter at all but to show each row's version and mod — the header is
already parsed and sitting in `FileData.Data` — the way `LoadRaceScreen` labels entries "Mod: X" /
"Vanilla" instead of hiding them. That keeps every possible collision visible, which is exactly
what an overwrite check needs.

Sibling defect, now fixed: the export archive was named from the running build and mod rather than
the save's own header, so exporting an old save produced a zip that misreported its contents.
Export now refuses a save whose header version or mod does not match the running build.

**"Billion Credits" may be as wrong as the "/Y" was.** `51a12578d` fixed `BC/Y` → `BC/T` because
money is charged per turn. But `MoneyString()` (`SDUtils/NumberExt.cs:117`) is a plain two-decimal
format with no scaling anywhere, and the budget Codex entry says "credits per turn" — so "Billion"
looks like a legacy label with nothing behind it. Redenominating is a design call, not a bug fix,
and would touch all 12 display sites plus six tooltips again. Recorded so it is not rediscovered.

## Test debt

No test covers `Relationship_trust.GetTrustGain` (a live balance change), `Building.OnDeserialized`
text ids (a serialization change — a round-trip test is the one most worth writing),
`InfiltrationOpsUprise` fertility, or `InputState.Undo`.
