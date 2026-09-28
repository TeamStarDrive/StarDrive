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

**Also observed in the PR review of 2026-09-28, not yet decided:**

- The latch above also fires in normal play. `PlanetAmbient` has `MaxConcurrentSoundsPerEffect: 1`
  and `FadeOutTime: 0.1`, and a stopped instance counts until its fade ends, so stepping
  Barren -> Terran -> Barren with the arrow keys inside that tenth of a second finds
  `CanPlayEffect` false and leaves the colony silent. Raising the effects volume from zero after
  opening the colony does the same.
- The bed plays on over full screens opened from the colony (research, ship design) for the rest
  of one 22-28 s bed, because a hidden `UniverseScreen` is not updated, and over the racial
  music popups; coming back does not replay it, since the cue is latched.

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
  **The reach test still measures to the nearest module, on purpose** (Copilot flagged it on PR
  #416). It answers "does the blast reach this hull at all", and a blast whose centre lies inside an
  overlapping hull does. Gating on the entry plate instead would make any blast inside a larger
  hull harmless whenever its facing plate is farther than the blast radius - a small ship dying
  over a capital's centre, with plates up to ~520 units out. When the plate is beyond the radius
  the spread radius is 0, so only that plate is hit, at the falloff for its own distance.
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
  than a slope. The `resist >= 1` half of this (Star Trek's `AncientArmor_3x3` ships **1.39**, so
  the term flipped sign and *added* 255,769 per plate, and `InternalDamageModifier` returned
  `-0.39`, which **repaired** the plate) has since been fixed by damage #2; the health term stays.
- Reactors are supercritical against each other. `Extreme Fusion Reactor` is 1,550 health and
  detonates for 6,000 over 72 units, roughly 4x the health of the reactor beside it, fired
  synchronously from `ShipModule.Die`. One `AntiMatterReactor` dying takes 34 modules of a Heavy
  Carrier with it.

### 2. Show enemy strength per system in the planet list, from the threat matrix — SHIPPED

`[done]` The player had no compact answer to "where is the enemy strong". The threat matrix
already held both halves of the answer and nothing read them out.

**What the player sees now.** The System column of the planet list **and** the exotic systems list
was rebuilt to match the Planet column beside it — the star's own icon on the left, text
left-aligned after it — and a system holding known hostiles prints a second line under its name:

```
(*)  Vega
     Hostiles: 4 Ships, 12.4k str ([flag][flag])
```

one flag per empire among them, tinted in that empire's colour. The star is `Sun.Icon`, the same
texture the galaxy map draws, at the Planet column's icon size (`rect.Height - 10`). All of this is
Gilad's call, arrived at over several passes; the column header above it stays centred, as every
other header on those screens is.

Both screens share `HelperFunctions.AddSystemNameAndHostiles`, which draws the whole System cell,
so they cannot drift apart. The usable width after the star is `rect.Width - 45`, which is 180px at
1920 and 102px at 1280, so the full line fits at 1920 and the `Hostiles:` label is dropped at 1280.
Below that the line can still overflow; the star icon size is the dial if it ever needs tightening.

**The `EnemyHere` flash icon keeps its original tooltip on all three screens, and the galaxy map
is untouched.** Gilad's call, twice: the numbers belong on the row rather than behind a hover, and
the map keeps the plain warning it has always had. Do not "enrich" those tooltips back.

**The data.** `ThreatMatrix.GetHostilePresenceAt(pos, radius)` returns a
`HostilePresence(int NumShips, float Strength, Empire[] Empires)` from **one** qtree query, summing
`ThreatCluster.Ships.Length` and collecting the loyalties beside the `Strength` that
`GetHostileStrengthAt` already summed. It resolves the empires eagerly rather than keeping the
cluster array: a row holds its presence for as long as the screen is open, and a `ThreatCluster`
holds `Ship` references the matrix itself has already let go of.
`Empire.KnownEnemyPresenceIn(SolarSystem)` is the per-system wrapper, beside
`KnownEnemyStrengthIn`. No existing caller changed - the AI's twenty-odd `KnownEnemyStrengthIn`
sites are untouched.

Both screens pause the universe while they are open (`GameScreen`'s `toPause`), so the numbers
cannot go stale under the player, and the count and strength cannot come from different ticks.

Two new text tokens: `Hostiles` (4577) and `Str` (4578), ENG only, in the free 4000-8000 run.

The three points the original entry said needed settling:

- **Which radius counts as "in this system"** — the system's own, via the same
  `FindHostileClusters(s.Position, s.Radius)` call the icon has always used. Reusing the query
  rather than writing a new one means the icon and the numbers beside it can never disagree, which
  `KnownEnemyPresenceIn_AgreesWithKnownEnemyStrengthIn` pins.
- **Raw strength or a banded icon** — raw, through `GetNumberString` (`12.4k`, `1.25M`). Gilad's
  call: the same figure the AI reasons about, so what the player reads is what the game uses.
- **Only what the player knows** — satisfied by construction. Rival clusters are built solely from
  the `Seen` set in `ThreatMatrix.CreateAndUpdateRivalClusters`, so nothing here can report a ship
  the player never scanned.

**One quirk worth knowing.** An unobserved cluster keeps its remembered `Strength` but filters
dead ships out of `Ships` (`ClusterUpdate.Update`, the `ObservedShips.IsEmpty` branch), so a
remembered sighting can end up with a ship count of zero while its strength stands. The line drops
the ship count in that case and shows strength alone, rather than printing `0 Ships`. The same
filter means the count shrinks when ships the player saw die out of sight — a small leak, inherent
to the existing data structure, not introduced here, but now player-visible for the first time.

Covered by four tests in `UnitTests/Universe/ThreatMatrixTests.cs`, each revert-checked against a
mutant. `CodexExpansionScoutingText` describes this icon and now describes the line too.

**Left open, by choice:** the threat is not a sortable column, so the player cannot rank systems by
danger; the flags are in cluster order rather than strongest empire first; and the galaxy map shows
only the icon, with no numbers anywhere on it.

---

## Priority 4 - biosphere placement, from Roland's follow-up on #321

### 1. A biosphere built to make room must land on an EMPTY tile

`[open]` Not investigated beyond reading the code. Logged 2026-09-27 from Roland-Johansen's
comment of 2026-09-25 on issue #321 (`issuecomment-5837186261`), which followed the biosphere
capacity work shipped in `933196b4f`.

**What he saw.** On Xammar I, whose blueprint was unfinished, the governor built biospheres on
the tiles of an **outpost** and a **terraformer** - two buildings that do not need a biosphere to
exist. The planet had no free tile a biosphere could occupy; the remaining ground was volcanoes,
marsh and tornadoes, which must be terraformed first. He agreed the population maths we gave him
was right, so a biosphere there was not worth building for population; the only remaining
justification was making room for the blueprint, and **a biosphere under an existing building
makes no room**.

**Why the code does it.** `Planet_EvaluateBuildings.TryBuildBiospheres` (~817) builds for either
of two reasons and then hands the tile choice to one common helper:

- `needGroundToBuildOn` - something is wanted, `FreeHabitableTiles == 0`, and the budget covers
  the biosphere plus the cheapest wanted building.
- `BiosphereCarriesItsPopulation(bio)` - the added population pays the upkeep back.

`PreferredBiosphereTile` (~858) then tries, in order: an empty tile that is not terraformable, any
empty tile, and finally **any tile that can take it at all** - which is where a tile already
holding a building gets picked. That third fallback is correct for the population reason and wrong
for the room reason, and the helper cannot tell them apart because the caller does not tell it.

**The refinement Roland asks for**, in his own terms: a biosphere placed to create a building spot
must go on an empty tile, and if there is none the colony should terraform first rather than build
the biosphere; a biosphere placed for population may go on an occupied tile, though an empty one
is still better because it gives both.

**Points to settle.** Whether `TryBuildBiospheres` passes its reason down to
`PreferredBiosphereTile` or the caller simply refuses to enqueue when the room reason finds no
empty tile; whether "empty" should mean `NoBuildingOnTile` or also exclude tiles reserved for a
terraformer, which the first preference already avoids; and whether refusing to build leaves the
colony stuck when terraforming is unavailable, since `needGroundToBuildOn` is what currently
breaks that deadlock.

**Do not re-derive the shipped rules while doing this.** The build and scrap rules from
`933196b4f` - `BiosphereCarriesItsPopulation`, `ShouldScrapFreeBiosphere`, and the deliberate
omission of `ExoticCreditsBonus` from the payback - are settled and listed below. So is the tile
rule itself (`54e182a1f`, `441802ecb`): steps 1 and 2 stay, and **every candidate must still pass
`CanEnqueueBuildingHere`**, because `Building.AssignBuildingToTile` validates the tile it is handed
and returns false rather than picking another - naming an unusable tile fails the enqueue on every
governor pass, forever, which was issue #312. The refinement is only that step 3 must not fire
under the facilitation reason. This is a placement question only.

---

## Priority 5 - empires shared their tech bonuses with their race template - FIXED 2026-09-27

`[balance]` Found while reviewing the damage #18 test, which leaked a weapon bonus into every later
test. Reproduced in unit tests and in Gilad's own saves; fixed on `fixes_35`.

**What was wrong.** `EmpireData.CreateInstance` is a `MemberwiseClone`. It gave the new empire
fresh research, agent and artifact lists, but **not** its `WeaponTags` map, its `RoleLevels`
array or its `ShipModulesInResearchQueues` set, so every empire made from a race held the race
template's own objects. Tech unlocks write straight into them:
`TechEntry.ApplyWeaponTagBonusToEmpire` raises the shared `WeaponTagModifier`, and the
`Bonus Fighter Levels` and `ShipRoleLevels` bonuses add to the shared `RoleLevels`. The templates
(`ResourceManager.MajorRaces`) load at boot and reload only on a mod switch or when leaving a game
for the main menu under memory pressure (`GameLoadingScreen(resetResources: true)`), so normally
they live for the whole session. The only reset, `EmpireData.ResetAllBonusModifiers`, is
reachable only from the debug Reset All Techs button, and even that clears `WeaponTags` but not
`RoleLevels`.

**What it did in play.**
- **A second new game in one session started with the first game's research.** Every weapon
  tag bonus (damage, HP, blast radius, shield penetration) and every ship level added to
  `RoleLevels` (the corvette and drone levels of `Bonus Fighter Levels`, and `ShipRoleLevels`)
  that any empire of a race had researched was already in place for that race in the next new
  game - the player's race and every AI race alike - and kept adding up over a third and fourth
  game. Restarting the game or a resource reload cleared it; loading a save did not reintroduce it,
  because a save carries its own copies. A save made during an affected game keeps the inflated
  values it started with; they cannot be told apart from real research and are not repaired.
- **Rebels stayed wired to their parent.** Rebels are made by `CreateRebelsFromEmpireData(data,
  this)` with the parent's **live** data - on a bankruptcy rebellion (`Empire.cs` ~2013), from
  espionage (`Empire_Espionage.cs` ~105), and for a defeated empire whose remaining ships defect
  to its rebels (`SetAsDefeated`, ~477) - so the rebels shared the parent's objects directly and
  their weapon bonuses and ship levels kept rising with the parent's research while it lived. Rebels
  never research (`Empire.cs` ~1929), so normally the parent was not affected in return, and nothing was
  applied twice: the rebel constructor clones the parent's tech entries and `InitEmpireUnlocks`
  marks techs researched without re-applying bonuses. **The link survived saving**, because the
  binary serializer tracks objects by reference: 12 of Gilad's 33 Combined Arms saves have rebels,
  and loading them without the fix leaves 68 pairs of empires sharing these objects.
- The unit tests leaked into each other the same way, since `CreateUniverseAndPlayerEmpire` makes
  each test's player from a shared template.

**The fix.** `CreateInstance` now gives each new empire its own copy of `WeaponTags` (a clone of
every `WeaponTagModifier`) and `RoleLevels`, and a fresh `ShipModulesInResearchQueues` beside the
research queue it already reset. Rebels therefore start with their parent's bonuses as they stand
at the rebellion and keep them. A `[StarDataDeserialized]` hook on `EmpireData` makes the same
copies on every load, which splits the shared objects in existing saves while keeping each
empire's values. The unused `EmpireData.GetClone()`, the same shallow copy, is deleted.
`RacialTrait` needed nothing: `CreateInstance` already clones the traits, and the race design
screen builds its own. **Not repaired:** the hook copies the research-queue module set rather
than rebuilding it, so an affected save keeps stale entries, whose only reader picks the outline
colour of a locked module (`Ship_Rendering.cs` ~440); and the list screens' sort buttons
(`PLSort`, `ESSort`, `SLSort`, not saved) are still shared, so a sort choice carries into the next
game in the session. Both are cosmetic.

**Evidence.** `UnitTests/Technologies/TechBonusIsolationTests.cs`: a second universe made after
the player unlocks Plasma Ordnance and Ace Training starts at the template's values; rebels made
from the player's data do not gain the player's later unlocks; and a parent and rebels that
share the objects come back from a binary round trip with their own copies and the same values.
The first two fail on the old `CreateInstance` and the third fails with the hook removed. All 41
of Gilad's current saves (8 vanilla, 33 Combined Arms) load and run 60 ticks with the fix, with
no two empires left sharing; the full suite passes.

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
- **Damage #3, #4, #5, #6, #7 and #11 below** — Gilad's call, 2026-09-27. A module-death blast carrying the
  killer's weapon, a radial blast hitting a big module once per covered cell while the directional
  one dedupes, that blast wasting its root slot, and carry-on damage truncated to int, are all as
  designed. #4 in particular: **do not add a dedupe to match the directional path.** And #11:
  missiles keep their raw-level aim error; **do not route them through the gun formula**.
  And #6: a shield bubble hit spends armor piercing; **do not stop charging it**.
- The AI gets half production tax on cybernetic colonies and the player does not
  (`ColonyResource.cs:222`); `ResearchTaxMultiplier` is difficulty-only and always 1 for the player
  (`UniverseGenerator.cs:232`). Both intended.

## Worth a GitHub issue if we ever file any

Player-visible, self-contained, and safe to hand to someone else: misc #6, misc #1 (power #1 and
damage #2 were on this list and are resolved). Everything else is better done by us or not at all.

---

## Power (7)

From the `design_power_budget` Codex entry. Items 1, 2, 4, 5 confirmed by two fact-check passes.
Items 2, 5 and 6 shipped in `3037ebbf7` and 7 in `6c9f0fdda`; 1, 4 and 3 (`68fa66b1a`, with tests)
followed.
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
   armour-piercing walk, which the Codex now states. **An explosive projectile's blast drains once
   per ship, however many modules it catches** (Gilad 2026-09-28, fixes_36), and applies its EMP the
   same way: the projectile carries `EmpDamage` and `PowerDamage` copied from its weapon, and an
   exploding one spends each on the first module that takes it; the space nuke branch
   (`DamageRadius >= 256`) recharges them before every ship. Before, a `RemnantNuker` blast on a
   Dreadnought caught 21 modules and applied 2100 EMP instead of 100. `CalculateOffense` no longer
   scales the EMP and power part of an explosive weapon's rating by its blast radius. Covered by
   `BlastEmpAndPowerDamageTests` and `TestWeaponModifiers.TheBlastRadiusDoesNotScaleTheEmpRating`.
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
projectile**, which applied per module in the blast until a blast became one drain per ship
(item 2). The AI blast radius is larger than the gameplay
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
All 18 are closed. Items 1, 2, 8, 9, 12, 13 and 18 are resolved, items 10 and 14 went with the
hull bonus feature itself, item 15 went with the two weapon tag bonuses it describes, item 16
went with ship-mounted repair beams, and item 17 was not a bug; items 3, 4, 5, 6, 7 and 11 are
as designed and must not be "fixed".

1. ~~Weapon-tag armor/shield damage bonuses never applied.~~ Resolved - **deleted, not wired up**.
   A tech's `Weapon_ArmorDamage` / `Weapon_ShieldDamage` reached `WeaponTagModifier`, was copied
   onto every projectile by `Weapon.AddModifiers`, and died there. It was structural rather than a
   missed call site: `Projectile.DamageMod` hands the damage path the *weapon template*, so a
   per-projectile bonus is unreachable from `ShipModule.Damage`. Meanwhile `ModuleSelection`
   already scaled the screen's "VS Armor" and "VS Shield" by it, so the screen promised what
   combat never delivered - and the two sides disagreed on units, `EmpireData` commenting
   `ArmorDamage` as FLAT and `ShieldDamage` as a percentage while the screen treated both as
   percentages and `AddModifiers` treated both as flat. **No tech in vanilla or any of the three
   mods grants either bonus type**, so nothing in play changed and there was no balance risk in
   either direction; Gilad chose deletion over wiring it up. Removed: two `Projectile` fields, two
   `AddModifiers` lines, two `WeaponTagModifier` fields, two `TechEntry` switch cases, two
   `GetStatBonusForWeaponTag` cases, and four test assertions plus their two setup assignments,
   which pinned the *assignment* rather than any effect. `DrawResistancePercent` now reads the weapon's own value. The commented-out
   tech-typing block in `Technology.cs` still names both bonus types; it is inert.
2. ~~100% resistance makes a NaN, and resistance above 100% repairs the module it hits.~~
   Resolved. A resistance is a *fraction* of incoming damage, so anything at or above 1 took
   `ShipModule.Damage` somewhere it was never meant to go. Two changes:
   - **Every resistance now reads back clamped to 1**, on the ten `ShipModule` properties rather
     than in the flyweight constructor - the flyweight's fields are `[StarData] readonly`, so a
     savegame can populate them directly and a constructor clamp would leak. Nothing outside
     `ShipModule` reads `Flyweight.*Resist`, so the properties are the whole choke point.
     **Four consumers move with it**, all in the right direction and all only for content that was
     out of range: the design screen now prints the figure the sim uses;
     `InternalDamageModifier` (`1 - ExplosiveResist`) goes from -0.39 to 0, so an internal blast is
     absorbed at that plate instead of repairing it; `CauseRadiationDamage`'s
     `1 - EnergyResist` likewise; and `CalculateModuleDefense`'s `def *= 1 + resist*0.2` drops a
     `PlasmaResist 5` module from a 2.0 multiplier to 1.2. `Ship.BaseStrength` is `[StarData]`,
     so an old save keeps its pre-clamp strength until something recalculates it.
   - **A damage modifier of zero absorbs the shot** instead of being divided by. `absorbedDamage`
     is set to the full incoming damage, so `damageRemainder` comes out 0 and nothing carries on.
   - **The death blast formula now skips a plate it cannot describe.**
     `GetExplosionDamageOnShipExplode` divides by `1 - ExplosiveResist`, which is only defined
     below full resistance, so the term is taken only when the resist is **above 0 and below 1**.
     Flooring the denominator instead - the first attempt, at 0.01 - was caught in review and is
     **much worse**: Star Trek's `AncientArmor_3x3` has `Health 99750` and `ExplosiveResist 1.39`,
     the only module in any shipped content with an explosive resist at or above 1, and it would have gone from
     contributing **+255,769** to the ship's blast to cancelling **9,975,000** of it, pinning any
     ship carrying that plate at the `Radius*10` floor. The domain guard makes its contribution 0
     instead. That is still a change for those ships - they lose a quarter million of blast that
     the broken formula was handing them - but it is bounded and explainable, and the formula
     itself stays on the list below as wrong.

   **What it actually did, corrected twice.** It was logged as `[crash]`; it is not one, but it is
   worse than the first correction said. Mutation testing showed the NaN does **not** quietly
   become 0: on this runtime `(int)NaN` yields **int.MinValue**, and `damageRemainder` is written
   straight back into the caller's own damage figure - `Projectile.TryDamageModule` does
   `victim.Damage(this, DamageAmount, out DamageAmount)` and `ShipModule.DamageExplosive` does the
   same with `ref damageInOut`. Both then test `<= 0f` and stop, so the *outcome* was accidentally
   right, but a projectile's `DamageAmount` was passing through -2,147,483,648 to get there.
   A previous commit message repeated the `(int)NaN` is zero reading; this is the correct one.

   **Reachable in shipped content.** Combined Arms' `Dark Energy Reactor` has `BeamResist 1` and
   `EnergyResist 1`, and the deflection early-out does not save it: beams skip that block
   entirely, and a projectile compares `0 < 0` against a default Deflection of 0. Above 1, the
   modifier went negative and `SetHealth(Health - modifiedDamage)` **healed** the module. Star
   Trek's hull-path resists above 1 are `PlasmaResist` 5 and 1.03 on three more modules, plus the
   `ExplosiveResist` 1.39 noted under the ship death blast work; its two `ShieldKineticResist 1.6`
   modules could never heal, because `DamageShield` returns at `damageAmount < 0.01f`.
   The heal was **not** beam-only: the guard is
   `modifiedDamage < damageThreshold && proj?.WeaponType != "Plasma"`, and `PlasmaThrower` is a
   plasma *projectile* - `Tag_Beam false`, `Tag_Cannon true`, `ProjectileSpeed 1700`,
   `FireDelay 0.025` - shipped in vanilla and in four variants each in Combined Arms and Star
   Trek. At 40 shots a second it repaired a plate to full almost at once.

   Covered by `UnitTests/Ships/ModuleResistTests.cs` against new `TEST_ModuleResist`
   (`KineticResist 2`, `BeamResist 3`, `ExplosiveResist 2`) and `TEST_ShipResist`. Each half was mutation-checked
   separately. One of the three tests as first written passed with and without the clamp, because
   a ballistic projectile *is* caught by the deflection guard - it was rewritten to assert the
   damage modifier never goes negative, which is the root cause rather than one weapon's path.
3. `[settled]` **Module-death blasts run through the killer's weapon.** As designed, Gilad
   2026-09-27. `ShipModule.Die` passes the killing projectile as the source, so every module in
   the blast takes that weapon's EffectVsArmor, resistances, deflection roll and EMP, and
   `CauseEmpDamage` and `CausePowerDamage` fire once per module. The exception, since fixes_36:
   when the killer is an exploding projectile, the module blast happens inside that projectile's
   own blast, so its EMP and power damage still reach the ship only once (Power item 2).
4. `[settled]` **Radial blasts hit big modules once per covered cell.** As designed, Gilad
   2026-09-27. `Ship.DamageExplosive` (`Ship_ModuleGrid.cs` ~525) has no dedupe, while the
   directional version dedupes via `SplashHitScratch`. The asymmetry stays: **do not add a dedupe
   here to match.**
5. `[settled]` **A module-death blast wastes its root slot.** As designed, Gilad 2026-09-27. The
   blast centres on the dying, already-inactive module, so the full-damage root yields nothing and
   every direction starts at 25%.
6. `[settled]` **Shield bubble spends armor piercing.** As designed, Gilad 2026-09-27. A shot that
   hits a bubble counts as touching the shield module, so `Projectile.TryPhaseThroughModule` takes
   the module's width in slots plus its APResist off the shot's armor piercing, although the shot
   never crosses that module. It matters only for a non-explosive armor-piercing shot that breaks
   the shield and carries on into the hull, and since piercing values (weapons' own 1 to 5, mostly
   1-2 and mostly in Combined Arms, plus kinetic technology bonuses of +1 to +3) are about the size
   of shield widths (1-4 slots, up to 6 in both mods), breaking a bubble usually uses it all up. Shields
   blunting armor piercing is the intended counter, and the Armor and Shields entry and the Armor
   Pen tooltip already say every module a shot touches uses up its width plus its AP resistance.
   **Do not stop charging the bubble hit.**
7. `[settled]` **Carry-on damage is truncated to int.** As designed, Gilad 2026-09-27.
   `damageRemainder = (int)(...)` in `ShipModule.Damage` drops the fraction.
8. ~~Planets repair any ship in orbit, whoever owns them.~~ Resolved - `Ship.Repair` now takes
   planet repair only from a planet owned by the ship's empire or an ally, the pair supply already
   used, Gilad's pick 2026-09-27. Covered by `UnitTests/Ships/PlanetRepairTests.cs`: own and allied
   worlds add their rate, and enemy, at-peace and ownerless worlds add nothing; with the gate
   removed the last three fail, and with an owner-only gate the ally test fails. The three Codex
   repair sentences now say "your own or an ally's colony". What it was: a player Vulcan Scout
   orbiting an enemy homeworld repaired at **440/s against 40/s** in deep space, and the same at the
   homeworld of an empire it was at peace with; its own homeworld, the control, adds exactly the
   planet's rate. The test turns
   `UseCombatRepair` on because the harness never advances `GameBase.TotalElapsed`, so every ship
   there reads as recently damaged; that the states below are out of combat comes from reading the
   code. `Ship.Repair` (`Ship_Repair.cs` ~59) takes the tether or the orbit target with no owner
   check, and the planet also clears EMP and lifts the repair level to `p.Level + p.NumShipyards`.
   Supply does check: `GeodeticManager.AffectNearbyShips` serves only the owner and its allies, and
   `IsSuitableForPlanetaryRearm` reads the same pair.
   **Reachable out of combat**, the only time repair runs (`UseCombatRepair` is false in vanilla and
   every shipped mod): the default right-click on any planet is `OrderToOrbit` with no owner check,
   so any foreign world at peace and any enemy colony with nothing firing; a bomber whose target
   stops being attackable is parked in orbit on purpose (`DoBombard`, "Stay in Orbit"); a troop ship
   that has launched all its troops orbits the planet it is invading; and a boarded orbital keeps
   its tether to the old owner's planet, because boarding never untethers. A planet that loses its
   owner also keeps its last `RepairRatePerSecond`, since only `AffectNearbyShips` sets it and that
   returns early without an owner, so it repaired at the dead colony's rate until the next load;
   the owner gate closes that case as well, and the stale rate itself is harmless now. The EMP
   sentence in `CodexWarfareCombatBasicsText` said "orbiting a friendly colony", so the EMP
   clearing that rides on the same block matches it; `15e9fc81f` reworded it to "orbiting your own
   or an ally's colony".
9. ~~`IsCoveredByShield` picks the last shield, not the strongest.~~ Resolved - `maxPower` is now
   raised when a shield wins, so the method does what its own comment says. **Narrower than it
   first reads**: the method has exactly one caller, `Ship.CauseRadiationDamage`, reached only
   from `SolarSystem.ApplySolarRadiationDamage`, so this is solar radiation and nothing else. Only
   three sun types carry `RadiationDamage` at all - `star_neutron` and `star_pulsar` at 10,
   `gargantua_black_hole` at 20. Within that, the bug had two effects: a nearly drained bubble
   could soak the radiation while a full one beside it stood untouched, and because the caller
   dedupes by shield, scattering the picks across more bubbles damaged more of them per tick than
   concentrating on the strongest. Covered by `ShieldCoverPicksTheStrongestShield` in
   `UnitTests/Ships/ExplosionShieldTests.cs`, which fails on the old behaviour by picking the
   shield at grid 3,2 over the stronger one at 0,2. The method is now `internal` so the test can
   call it; `InternalsVisibleTo("UnitTests")` was already in `Properties/AssemblyInfo.cs`. No Codex
   entry describes how radiation picks a shield. **Left as it stands:** "strongest" means most
   current charge, while the damage the winner takes scales by `ShieldHitRadius`, so a wide bubble
   with slightly less charge is passed over for a small full one. The comment asks for charge and
   that is now what it does; whether radius should weigh in is a separate balance question.
10. ~~Hull bonuses are off everywhere.~~ **Removed**, Gilad 2026-09-27. `UseHullBonuses` was false
    in vanilla and every bundled mod, and no content anywhere shipped a `HullBonuses` folder, so the
    loader would have switched the flag back off even if a mod set it. Gone: `HullBonus`,
    `ResourceManager.HullBonuses` and its loader, the `UseHullBonuses` global, `Bonuses` on
    `ShipHull` / `IShipDesign` / `ShipDesign`, and every place a bonus applied - weapon damage,
    armour, sensor range, speed, cargo, starting cost and cost, and the repair and shield factors in
    `EmpireHullBonuses`. `EmpireHullBonuses` itself stays: it is the empire's tech bonuses to
    modules, and it now keeps one entry per empire instead of one per `HullBonus`, which it already
    was in practice, since every hull shared the empty default. The design screen's hull bonus
    panel (`DrawHullBonuses`) was already dead code with an inverted condition. A third-party mod
    that ships `HullBonuses` loses them silently, and its `UseHullBonuses` line now logs a yaml
    warning instead of loading. The Codex never mentioned them.
11. `[settled]` **Missile aim uses the raw crew level.** As designed, Gilad 2026-09-27: missiles keep
    it, and the dead `Ship.MaxWeaponError` was deleted. `MissileAI` (~258)
    passes the launching ship's `Level`, or the planet's, into `Weapon.GetTargetError`, which then
    skips the branch guns take: a gun adds the Militaristic trait to its level, squares it, and adds
    FCS `TargetingAccuracy`; a missile gets the level as is. The split dates from `af2c19d26`
    (October 2020), which squared the level only inside the `level < 0` branch and left the missile
    call site on the old formula. For a 1x1 launcher (base error ~494), before the turret and trait
    multipliers: at level 0 both errors are ~83, at level 5 a gun's is ~0 and a missile's ~33. A
    missile re-rolls that error every 0.1-0.8 s and keeps homing, so it shows as weave more than as
    misses, except against small targets. **The Codex already describes the current rule**: FCS
    "shrinks the aim error of unguided weapons", and guided weapons "home in instead and show no
    Accuracy", so aligning missiles with guns would need that text changed as well.
    `Ship.MaxWeaponError` was computed in `UpdateWeaponRanges` and read nowhere.
12. ~~Jammed missiles re-target every 0.15 s.~~ Resolved 2026-09-27 - the stray `Target = null` is
    gone, so a jammed missile keeps its lock, steers for its decoy point every frame and
    self-destructs there, as the Codex already said. The line began as the `catch` of the 2014 ECM
    code (`431cee43a`) and the 2017 refactor `16968eb2f` left it running on every call. Covered by
    `MissileJamTests`, which fails on the old line. The same commit makes
    `Projectile.IsAttackable` read `MissileAI?.Target?.GetLoyalty()`, which would throw for a
    missile with no target; no caller passes a projectile today, so it never did. One more change
    rides on it: the jammed branch returns before the retarget timer, so a jammed missile no longer
    calls `ChooseTarget`. Before, an empire without smart missiles lost a jammed missile within
    0.15 s and a jammed torpedo flew straight; now both fly to the decoy, as the Codex describes.
    What it was, and still
    **unreachable in shipped content**: a missile is jammed only when the target ship's `ECMValue` beats the
    missile's `ECMResist` plus a roll of 0 to 1, and `ECMValue` comes only from module `ECM`, which
    no module in vanilla or any bundled mod sets. The ECM techs raise `MissileDodgeChance` instead
    (`TechEntry.cs` ~942), a different mechanic. If a mod adds ECM, the code does what this item
    says: `MoveTowardsTargetJammed` clears `Target` on every call, so the missile steers toward its
    decoy point for one frame, flies straight until `TargetUpdateTimer` picks a target again 0.15 s
    later, and keeps `Jammed` and `FixedError` from the first target, since neither is ever reset.
    It only checks for arrival within 300 units on the frames it steers.
13. ~~Shield penetration, screen versus combat.~~ Resolved 2026-09-27 - **combat changed to match
    the screen**, Gilad's pick. The screen added the empire bonus, the weapon's own chance and
    every tag's bonus; combat took the best of (tag bonus + own chance) and used the empire bonus
    only as a minimum. Now both call `WeaponTemplate.ActualShieldPenChance`, which adds all of them,
    so they cannot drift apart again. **In play only the empire bonus changed anything**: its one
    source in content is the Polaron Codex artifact (+10%, more with the Spiritual trait; the unused
    tech bonus type `Kinetic Shield Penetration Chance Bonus` feeds it too, for every weapon despite
    its name), whose text promises a
    chance for "your weapons (all types!)", and it used to do nothing for a weapon whose own chance
    was already 10% or more - a Dark Matter Cannon stayed at 35% - where it now adds its 10%. The
    other two differences were unreachable: Phased Ordnance on Kinetic is the only tag bonus in
    content, so no weapon has two, and every weapon with its own chance has tags. The chance is still
    rolled once per shot; above 100% it always passes. Covered by
    `TestWeaponModifiers.ShieldPenetrationBonusesAddUp`, which fails on the old rule and checks a Dark
    Matter Cannon's 35% becoming 45% with a 10% empire bonus. No Codex text
    described how the chance is built. Left alone at Gilad's call: Phased Ordnance's text says
    "Ballistic Cannons" while the bonus covers every Kinetic weapon.
14. ~~Hull fire-rate bonus never applies in combat.~~ **Removed** with item 10. It was the one hull
    bonus that never reached combat: `Weapon.FireDelay` was a `new` property carrying it, but
    cooldowns used `NetFireDelay` from the template (`WeaponTemplateWrapper.cs` ~147). A fix was
    written and tested first, then dropped when the feature went; the `new FireDelay` went too, and
    the `hull` parameter of the `Weapon` constructor and `ResourceManager.CreateWeapon` with it.
15. ~~Screen-only weapon tag bonuses.~~ **Removed**, Gilad 2026-09-27 - both bonuses were deleted
    rather than fixed. The fire rate bonus (`Weapon_Rate`, `WeaponTagModifier.Rate`) made the
    screen's Delay longer, so a rate bonus read as a slower weapon with lower DPS, and combat never
    read it. The armour penetration bonus (`Weapon_ArmourPenetration`) reached combat as
    `(int)ArmourPenetration`, dropping any fraction, and never reached the screen. **No tech, race
    or trait in vanilla or any bundled mod granted either**: the weapon tag bonuses content uses
    are Damage, HP, ExplosionRadius and ShieldPenetration. Gone: both `WeaponTagModifier` fields,
    their two `TechEntry` cases, the combat line in `Weapon.AddModifiers`, and `WeaponStat.FireDelay`,
    so the screen's Delay is the weapon's own `NetFireDelay`; `ApplyModsToProjectile` lost the two
    setup lines and the assertion that pinned the armour penetration bonus. The fields were saved
    with each empire and the reader skips them in old saves. The commented-out tech-typing block
    in `Technology.cs` still names both bonus types; it is inert. **Left alone, on purpose:** the
    screen compounds `Range` and `Speed` tag bonuses across tags while combat adds each against
    the base, which differs only for a weapon with two bonused tags; no content grants either.
16. ~~Ship-mounted repair beams repair nothing.~~ **Removed**, Gilad 2026-09-27. No weapon in vanilla
    or any bundled mod set `IsRepairBeam`, and no module mounted a repair beam, so everything hanging
    off the tag was dead: `Ship.RepairBeams` and `HasRepairBeam`, the AI's `DoRepairBeamLogic` and
    `Weapon.FireTargetedBeam`, the aim-error and retarget exemptions, the support-role count, the
    fleet and resupply "has repair" checks, the DPS and design-screen branches, and the screen's
    Repair tooltip `IndicatesTheMaximumAmountOf4` (id 7016). It could not have worked either: only
    `DroneBeam.Update` repairs (`Beam.cs` ~443), and a plain `Beam` with negative damage switches off
    its own collisions (~136), so a mod that set the tag would have fired beams that did nothing.
    The weapon **named** `RepairBeam` is unrelated and unchanged - it is the repair drone's own beam,
    which `DroneAI` creates by UID and which heals through its negative damage, not the tag. A mod
    weapon that still sets `<IsRepairBeam>` loads, since `XmlSerializer` skips unknown elements, and
    then acts as an ordinary beam: it picks enemy targets and fires to no effect, where before it
    fired at friendlies to no effect.
    Star Trek ships an orphan `orbital_RepairBeam` with damage **+1000** that nothing references.
17. ~~DPS uses the unclamped `ProjectileCount`.~~ **Not a bug**, closed 2026-09-27 with no code change.
    The clamp is the screen's own `projectiles = ProjectileCount > 0 ? ProjectileCount : 1`
    (`ModuleSelection.cs` ~543), a floor of 1 that only feeds the "Projectiles" line, which shows
    above 1. The DPS line uses the raw count, and so does combat: `Weapon.EnumFireSources` fires
    exactly `ProjectileCount` projectiles with no clamp, so a projectile weapon with a count of 0
    would fire nothing and show no DPS line (`DrawStat` skips a zero), and the two agree. No weapon in vanilla or any bundled mod has a count below 1
    (unset means 1; the highest is vanilla's 45).
18. ~~Blast radius on screen is the base value.~~ Resolved 2026-09-27 - the screen now shows the
    radius the projectile explodes with, Gilad's pick (the screen was the side that was wrong: the
    techs promise a bigger blast and combat delivers it). `ModuleSelection.BlastRadius` follows
    combat exactly: every tag's `ExplosionRadius` bonus is added against the **base** radius, as
    `Weapon.AddModifiers` does at launch, so a weapon carrying two bonused tags gets the bonus
    twice; then, for a weapon that uses ordnance, `OrdnanceEffectivenessBonus` multiplies the
    total, as `Projectile.ExplodeProjectile` does on detonation. For a MIRV it is the warhead's
    tags and ordnance cost, as in combat. The bonuses in shipped content are all tag bonuses, on
    Kinetic, Missile and Torpedo: Plasma Ordnance +25% and Remnant Assembly +20% in vanilla and
    Star Trek; Remnant Assembly +10% and GuidedMulti +3% (Missile and Torpedo only) in Combined
    Arms, which has no Plasma Ordnance. No content grants `OrdnanceEffectivenessBonus`. Covered by
    `BlastRadiusDisplayTests`, which fires a Rocket with both kinds of bonus and compares the
    screen's figure with the missile's `DamageRadius`; it fails against the base radius and
    against compounding the tag bonuses. The weapon stats Codex entry said Blast Rad was a radius
    "which technology can enlarge in combat"; it now says Blast Rad already counts it. **Left as
    it is:** for a MIRV the screen shows the warhead's blast, so a cluster missile launcher that
    hits a hull before it splits deals its own, larger blast (radius 80 on `ClusterMissiles`),
    which the screen has never shown.

## Budget, money and espionage (15, fourteen resolved)

From the budget screen entry (bucket 6). The Codex text describes what the code actually does, so
fixing any of these needs a Codex impact pass. Re-checked against the code on 2026-09-28: all 14
were still present; items 1 to 13 and 15 have since been resolved.

1. ~~Leeched money was paid twice.~~ Resolved 2026-09-28. `Espionage.AddLeechedMoney` put the
   money into the leecher's treasury the moment the victim's `DoMoney` ran, and the same amount
   was collected at the end of the turn into `TotalMoneyLeechedLastTurn`, a term of `GrossIncome`,
   which the leecher's next `DoMoney` added again. Every empire with a level 5 network collected
   4% of its victim's positive gain rather than 2%. The immediate `AddMoney` is gone, so the
   money arrives once, through the Money Leeched income line - which is what the budget screen,
   its tooltip, the Leech Income tooltip ("2% of their income per turn"), the espionage Codex
   entry (100196, level 5) and the Budget Screen entry (100225) already said, so no text changed;
   100225's Net Gain paragraph, false for a leecher until now, is now true. `TotalMoneyLeeched`
   (the level 5 panel's running total) and the victim's 2% loss are unchanged.
   **The collection moved too, and that part is load-bearing.** It used to run in
   `EndOfTurnUpdate`, which loading a save also runs, for every empire, before any `DoMoney`. That
   overwrote the saved, still-unpaid `TotalMoneyLeechedLastTurn` with the empty pending counter;
   harmless while the immediate credit had already paid it, but with the credit gone every load
   would have dropped a turn of leech. `UpdateMoneyLeechedLastTurn` is now private and runs at the
   start of the leecher's own `DoMoney`, so nothing touches it at load, the income row shows
   what was just paid, and it left the `Parallel.For`. It also walks `MajorEmpires` rather than
   `ActiveMajorEmpires`, so the leech from a victim defeated after paying it is still collected.
   Pinned by three tests in `InfiltrationOperationsTests`: `LeechedMoneyIsPaidOnce` (the old code
   puts 40 credits in the treasury where the income line shows 20),
   `LeechedMoneySurvivesSaveAndLoad` (collecting at end of turn loses the 20 at load) and
   `LeechFromAnEmpireDefeatedThatTurnIsStillPaid` (the active-only walk loses it).
2. ~~Planet troop upkeep cancelled out and was never charged.~~ Resolved 2026-09-28, together
   with 3. Garrisons were meant to cost: `af9f1cea3` (2021-04) added `TroopMaint` into each
   planet's `Maintenance`, so `TotalBuildingMaintenance`, gross minus net minus troops, came out
   as buildings only and `AllSpending` charged the troops on top. `66702a5dc` (2021-06) took
   the troops back out of planet `Maintenance` so colony budgets would not pay for them, but left
   the `- TroopCostOnPlanets` in `TotalBuildingMaintenance`: the two terms cancelled, and every
   garrison has been free since. `TotalBuildingMaintenance` is now gross minus net, so troops are
   charged at the empire level and colony budgets still do not pay them. The colony build list has
   shown each troop's 0.1 upkeep since `af9f1cea3`. Sized on 33 real Combined Arms saves (157 empires): the player
   pays a median 1.6% of gross income, at most 4.3% (about 25 credits a turn for a 69-planet late
   empire); AIs up to 16.8% where taxes sit near zero, which `AutoSetTaxes` covers since it
   chases `AllSpending`, and `TreasuryGoal` already subtracted the troops. Vanilla saves were all
   turn 1000-1001 with no troops, so they say nothing.
3. ~~Troop upkeep computed two ways.~~ Resolved 2026-09-28. The Troop Maint. row counted our
   troops on our planets, `ColonyMoney.TroopMaint` every troop on the planet, invaders included
   (commented "We count enemy troops as well", 2021). Now `TroopMaint` counts the owner's troops
   only, `GetTroopMaintThisTurn` is gone and the row reads `TroopCostOnPlanets`, so the rows add up
   to their total. Paying for the invader's soldiers, which the invader does not pay for either,
   read as a slip; foreign troops stood on a planet in only 4 of the 157 sampled empires, at
   most 10. Our troops standing on someone else's planet still cost nothing (the removed
   method's own TODO asked about it): during an invasion, or when they land to help an ally defend.
   Texts changed:
   Building Maint., Troop Maint. and Expenditure total tooltips (4561, 4563, 4566) and the Budget
   Screen Codex entry (100225); Economic Basics (100052) already said troops cost upkeep and is
   now true, and the level 3 dossier's Maintenance Costs line on the diplomacy screen adds the
   garrisons to `BuildingAndShipMaint`. Allied troops landed to help defend are free as well.
   `BudgetTests.GarrisonUpkeepIsChargedForYourOwnTroopsOnly` fails on each half of the old code.
4. ~~Dead treasury label.~~ Resolved 2026-09-28. `TreasurySliderOnChange` wrote half the goal into
   the slider text, which `Update()` replaced with the full `ProjectedMoney` before it was ever
   drawn; the halved write is gone, leaving the full goal the Codex and the slider tooltip
   describe. Both said the credits figure sits "beside" the slider, where the percentage is; they
   now say it is in the slider's title. The
   duplicate `treasuryGoal` assignment went too: the slider runs 0 to 1, so its relative and
   absolute values were the same number.
5. ~~`TreasuryGoal(float normalizedMoney)` never used its parameter.~~ Resolved 2026-09-28: the
   parameter is gone, and the method is private now that the planner is its only caller.
6. ~~Credits multiplier applied twice.~~ Resolved 2026-09-28. `ChargeCreditsHomeDefense`
   pre-multiplied by `CreditsMultiplier` and `ChargeCredits` multiplied again inside
   `ProductionCreditCost`, so a home defense launch paid the multiplier squared: 4% of the ship's
   cost on Normal instead of 20%, and 25% instead of 50% for a player on Insane. A defender that
   lands again is refunded its cost times its health times the multiplier (`LandDefenseShip`), so
   every undamaged sortie used to pay the player 16% of the ship's cost on Normal; now it nets
   zero, and a damaged one costs the fee on the health it lost. Scrapping a
   military building refunded through `EstimateCreditCost`, already multiplied, and
   `RefundCredits` multiplied again: 2% of its cost on Normal where a ship refunds 10%. Both now
   apply the multiplier once, like every other credit charge and refund, and the unused
   `HomeDefenseShipCostMultiplier` is gone. No text named either amount; the Production Fees
   tooltip's "a fifth of the production spent on Normal" now also holds for home defense
   launches, which land in that row. `BudgetTests.AHomeDefenseLaunchChargesTheCreditFeeOnce` and
   `ScrappingAMilitaryBuildingRefundsHalfItsCreditFee`.
7. ~~The AI's `SpyBudget` is not money under the new espionage system.~~ Resolved 2026-09-28
   (Gilad: fix as designed). It stored the raw weight fraction (1/420 above Normal, 0 on Normal)
   rather than credits, so `EspionageManager.DetermineBudget` → `SetAiEspionageBudgetMultiplier`
   landed at about 1.0001 and no AI ever paid for extra espionage points since Mars 1.50. The spy
   area is now credits like every other area - the treasury goal (or cash, when larger) times the
   weight - spent as before only while the AI's credit rating is above 0.6. The AI multiplier is
   capped at `Empire.MaxEspionageBudgetMultiplier` (5), the player slider's range, which it had no
   cap against: uncapped, a rich test AI reached 45. Normal stays free points only. An AI with a
   20,000 treasury goal can spend about 48 credits a turn, about 2x the points at 50 billion
   colonists. The multiplier also scales an AI's spy defence ratio, so infiltrating AIs above Normal
   gets slower too. The Infiltration Levels Codex entry now says what AIs do.
   `BudgetTests.AnAiAboveNormalBuysEspionagePointsFromItsTreasury`.
8. ~~Budget weights depend on yaml key order.~~ Resolved 2026-09-28, and the claim was wrong: a
   post-loop override set the AI's Spy weight to 0 or 1 under the new system whatever the order.
   What was real: that override made the yaml `Espionage:` value dead; under legacy the Espionage
   key counted as a useless area of its own; and race blocks could never work, since the loader
   sorted the All block last (overwriting them, the opposite of its comment) and matched a block's
   `PortraitName` against `empire.Name`, the full empire name. No shipped file has a race block.
   Now only the All block is read (anything else logs a warning), Espionage is never a weight of
   its own, and above Normal an AI's spy weight is the yaml Espionage value (1 if absent). The
   Budgets.yaml comment says what each key does. `BudgetTests.TheEspionageWeightIsTheAiSpyWeightAboveNormal`
   and `OnlyTheAllBudgetBlockIsRead`.
9. ~~Legacy espionage silently doubles the governor budgets.~~ Resolved 2026-09-28 the other way
   round (Gilad's call, option B): it was the new espionage system that halved them. The player's
   planner hands half of Build + Spy to each of colony, defense and space roads; Gilad tuned the
   weights (`d58fdc20e`) and raised that hand-out from `/3` to `/2` (`cfb897b3b`) in November
   2022 with the Spy weight at 25, for a colony share of about 1/19 of the treasury goal. The new
   espionage system (Mars 1.50) forces the Spy weight to 0 on Normal and 1 above, and the player
   slices fell to 1/38, 1/84 and 1/105 on Normal (1/37, 1/76 and 1/93 above it).
   `BudgetPriorities` now gives the player the yaml weights
   whatever the espionage system - Spy at its yaml value, the Espionage key ignored - so the player
   is back on the 2022 tuning: colony 1/19, defense 1/25, space roads 1/27, about 2x, 3.3x and
   3.9x today's default. The player's Build and Terraform slices shrink 6% with the larger total.
   AI weights are unchanged. The planner's comment claiming the spy budget "is not distributed"
   is gone; it always was. Budget Screen entry (100225) updated.
   `BudgetTests.PlayerBudgetWeightsDoNotDependOnTheEspionageSystem`.
10. ~~Trade panel rows and total read different lists.~~ Resolved 2026-09-28. The per-partner
    rows came from `TradeRelations`, the freighters' treaty cache, refreshed once a turn and never
    saved, while the Trade Treaties row and the total read the relations live. A treaty signed or
    broken this turn was in one and not the other, and after a load the rows were empty until the
    next turn. The rows now read the same live relations; the screen pauses the game, so nothing
    changes under them while it is open. The unused `TradeRelations` property is gone; the cache
    itself stays for the freighters. `BudgetTests.TheTradePanelListsATreatySignedThisTurn`.
11. ~~Lifetime trade average truncates twice, and is not lifetime.~~ Resolved 2026-09-28.
    `AllTimeTradeIncome` was an `int` that dropped the fraction of every delivery, the average was
    integer division, and neither it nor `TurnCount` was saved, so Mercantilism (Avg) was the
    average since the last load and every load reset it to 0 - which also dipped the treasury goal,
    since `MaximumStableIncome` reads it. Both are now saved and the sum is a float, which makes the
    tooltip and the Budget Screen entry (100225), "everything they have ever earned divided by the
    turns played", true. `BudgetTests.TradeUnderACreditCountsTowardTheTradeAverage` and
    `TheTradeAverageSurvivesSaveAndLoad`.
12. ~~The scrap guards never ran on a colony without blueprints.~~ Resolved 2026-09-28.
    `SuitableForScrap` returned early with `if (!RequiredInBlueprints(b)) return true;`, meant for a
    building outside a blueprint plan, but `RequiredInBlueprints` is false on a colony with no
    blueprints too. Every AI colony, and every player colony governed without blueprints, skipped the
    guards below it: a building that pays for itself, a food building the colony needs, a
    build-anywhere building on uninhabitable ground when replacing, a building with no upkeep when
    over budget, and storage whose goods would not fit without it. The early-out arrived with the
    blueprints UI in `de8f49ab4` (2024-05-31); before it the guards covered every building. Measured
    on 44 saves, over budget is rare (19 of 4128 governed colonies) and replacing is where it bit: in
    395 of 1052 full colonies the building the governor would give up next was one a guard
    protects, mostly storage. The early-out now tests `Blueprints?.IsNotRequired(b)`, so blueprint
    routing is unchanged (a planned building over budget meets the corrected money and food guards)
    and every other colony is guarded again, the auto-terraformer's room-making included. The food
    guard came back to life with it and had a precedence slip, `x - y/x` for `(x - y)/x`; fixed, it
    would still have priced a building with no fertility effect at zero on a barren world through
    the 0.01 floor, so it scales by the share of fertility or richness left only when the building
    changes it.
    `GovernorScrapGuardsTests`.
13. ~~The money guard's arithmetic was wrong.~~ Resolved 2026-09-28. `MoneyBuildingAndProfitable`
    set `PlusTaxPercentage * pop + CreditsPerColonist * pop` against upkeep: it ignored `Income`,
    applied the tax percentage to the population instead of the colony's tax base, and left out the
    tax rate and `ExoticCreditsBonus`. The errors partly cancel - on 44 saves the old figure was 1.08
    times the real revenue at the median, not the 2-4 times first estimated - but it gave the wrong
    answer for one money building in six, both ways: Combined Arms' Luxury Resort (flat income only)
    was never protected, and its Space Port (+50% tax) was protected above a billion colonists though
    it rarely pays. It is deleted. The guard calls `ColonyMoney.NetCostOf(b, standing: true)`, the
    model the build list colours with, at the current tax rate; `standing` takes a building's share
    out of the colony's figures instead of adding it. At 0% tax nothing is protected, which 29 of 256
    empires in those saves were at.
    `GovernorScrapGuardsTests.TheNetCostOfABuildingIsTheRevenueItAddsOrTakesAway`.
14. `[settled]` **The biosphere payback heuristic omits `ExoticCreditsBonus`**
    (`Planet_EvaluateBuildings.cs`, `BiosphereCarriesItsPopulation`). Left deliberately: the formula
    already uses `TaxRateMultiplier` rather than `TaxRate` so it is a "full rate" heuristic by
    design, `BiospherePaybackShare = 0.6` was tuned against it, the bonus is 1 for most empires, and
    the error is conservative. Retune the share and the term together or not at all.
15. ~~The budget screen runs the economic planner on the UI thread.~~ Resolved 2026-09-28.
    Opening the screen, ticking Auto Taxes and every step of a treasury slider drag called
    `EmpireAI.RunEconomicPlanner` from the UI thread, and a stepped slider fired `OnChange` twice
    per step. Each call moved the governor budgets' moving averages one step, so a drag
    fast-forwarded budgets the Codex says ease towards their new value, and every opening of the
    screen nudged them. The planner's goal-and-taxes part is now its own
    `UpdateTreasuryGoalAndTaxes`, and the screen queues it - and the tax slider's rate and
    `UpdateNetPlanetIncomes` - with `RunOnSimThread`, so the slider title and the auto tax rate
    still follow the slider and the budgets only move on the turn's planner run. The sim thread
    drained that queue only while paused or active; the budget screen opened from the Research,
    Diplomacy or Empire top bar, or from the Shipyard, leaves the universe neither, so the queue now
    drains in that state too. `FloatSlider` no longer fires `OnChange` a second time after setting
    `AbsoluteValue`, which already fires it. `BudgetTests.TheBudgetScreenLeavesThePlannerToTheSimThread`.

## Everything else (17, eight resolved)

1. ~~EMP recovery is a per-frame constant, unscaled by the time step.~~ Resolved 2026-09-28.
   `Ship.EmpRecovery` was drained once per simulation step, and the step is
   `1 / SimulationFramesPerSecond * min(GameSpeed, 1)`: EMP wore off twice as fast per game second
   at 0.5x speed, and slower whenever the simulation rate dropped - including the automatic drop
   the game makes when it falls behind, which is to say in big battles. It is now
   `EmpRecoveryPerSecond`, 60 times the old per-step figures, times the step, so the default 60
   steps a second at 1x or faster play exactly as before. `ShipEmpRecoveryTests`.
2. ~~`UniqueInEmpire` is a dead building tag.~~ Resolved 2026-09-28 - stripped. No C# has ever
   read it; it arrived with the 2021 building content (`d5d2435cd`). The per-empire flag is
   `BuildOnlyOnce`, which five of the six vanilla files already set alongside it. The sixth, the
   Imperial Bank, set only the dead tag and so has always been one per planet; kept that way, since
   `BuildOnlyOnce` would also stop governors building it. Combined Arms' Capital City had it too
   (moot there: a command building is only offered to a planet without one).
3. ~~`Weapon.BaseTargetError` carries two dead parameters.~~ Resolved 2026-09-28 - `range` and
   `loyalty` are gone, and the `ShipModule` doc comment that pointed at an `int` overload is fixed.
4. ~~The design screen's Accuracy row ignores the Militaristic trait.~~ Resolved 2026-09-28.
   `ShipDesignStats` and `ModuleSelection` passed `TargetingAccuracy` alone as the level, so a
   Militaristic empire's new ships aimed better than the row showed. Combat and both screens now
   take the level from `Weapon.AimLevel` - crew level plus the trait, squared, plus fire control -
   with crew level 0 on the design screen, the untrained crew the Codex says the row shows.
   `TestWeaponArcs.TheDesignScreenShowsTheAimOfANewCrew`.
5. `[content]` **Ship category tooltips bake in a threshold a mod can change.**
   `ShipCategoryUnclassifiedTip` onward state 85 / 97.5 / 92.5 / 90 / 87.5 / 75 percent, which is
   `threshold * 0.5 + 0.5` for the shipped `ShipDestroyThreshold: 0.5`. Star Trek's `Globals.yaml`
   sets 0.4, so all seven are wrong there. The Codex names the setting instead of the numbers.
6. ~~`RemoveMoles` dereferences a `Find` result without a null check.~~ Resolved 2026-09-28 -
   `agent?.`. Reachable under legacy espionage: an infiltration that succeeds sets the agent
   undercover and then awards its experience, and a level 10 agent retires there, leaving its mole
   behind with no agent. The next time that colony changed hands, the sim thread threw.
   `LegacyAgentTests.AMoleWhoseAgentRetiredGoesQuietlyWhenItsColonyChangesHands`.
7. `[thread]` **The sim thread repopulates UI dropdowns.** `UniverseScreen.Events.cs:14`
   `OnPlayerBuildableShipsUpdated` reaches `AutomationWindow.UpdateDropDowns` → `InitDropOptions`,
   which clears and refills `DropOptions` and writes `EmpireData` strings while the UI thread may
   be drawing them. Pre-existing.
8. ~~The colony screen tints biospheres by the old tax rule.~~ Resolved `a5c45f34b`.
9. ~~`Biospheres.xml` carries a dead `MaxPopIncrease` of 100.~~ Resolved 2026-09-28 - the line is
   deleted from the vanilla template (Combined Arms never had it). `UpdateMaxPopulation` excludes
   biospheres from `PopulationBonus`, so it raised no cap, yet the colony screen's building panel
   showed it as "+0.10 Max Pop" and it had misled a contributor into double-counting. The
   Biospheres tech stays a Colonization tech through `IsBiospheres`.
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
