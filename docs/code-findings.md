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

## Power (5)

From the `design_power_budget` Codex entry. Items 1, 2, 4, 5 confirmed by two fact-check passes;
item 3 is one reading, untested.

1. `[balance]` `[display]` **Reactor tech bonus applied twice.** `ShipModule.ActualPowerFlowMax`
   already multiplies by `EmpireHullBonuses.PowerFlowMod`; `Ship.UpdatePower` (`Ship.cs` ~1114)
   adds `PowerFlowMax * data.PowerFlowMod` again. Live recharge is (1+mod)², the design screen
   shows (1+mod). Tech "Power Flow Bonus", `TechEntry.cs` ~1006.
2. `[latent]` **Pwr Dmg and Siphon only work on beams.** `BeamPowerDamage` / `CauseSiphonDamage`
   (`ShipModule.cs` ~986-1018) are reached only from beam hits, so projectile weapons carrying
   PowerDamage (IonCannon1x2, IonDefenseCannon, DarkMatterCannons) or SiphonDamage (EmpCannon,
   DualEmpCannon, EmpDischarger1x2, REAegis) show a stat that does nothing. The Codex says so —
   fixing the code means editing that sentence.
3. `[balance]` **Destroyed reactors and conduits keep powering the grid.** `PowerGrid.Recalculate`
   (`Ship_PowerCalc.cs`) distributes from every module with a PowerRadius and floods every conduit
   without checking `Active`. `OnModuleDeath` sets `ShouldRecalculatePower`, which then changes
   nothing. Verify in game first; the fix changes combat balance.
4. `[display]` **Design screen over-counts multi-projectile weapon power.**
   `WeaponTemplate.PowerFireUsagePerSecond` (~177) multiplies by `ProjectileCount`;
   `PrepareToFire` / `PrepareToFireSalvo` charge once per shot.
5. `[balance]` **Siphon gives full value regardless** — `CauseSiphonDamage` credits the whole
   amount even when the shield held less.

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

## Everything else (14, two resolved)

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
