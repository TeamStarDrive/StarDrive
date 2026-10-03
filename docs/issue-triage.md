# Open issue triage

Snapshot taken 2026-09-24 of every open issue filed by `Roland-Johansen` (41 of the 47 open
issues on the repo), sorted into what is already handled and what is not. One fixed and closeable, six with an open
PR, 34 to work on. Hand-maintained: the
cross-references and groupings are judgement calls, not generated.

To refresh the status columns:

```
gh issue list --author Roland-Johansen --state open --limit 100 --json number,title
gh pr list --state open --limit 100 --json number,title,body,closingIssuesReferences
```

**Watch out when refreshing:** an issue fixed on a `fixes_NN` branch is never closed automatically
— GitHub's closing keywords only fire on a default-branch merge — and our commits do not reliably
write the `#`. Commit `54e182a1f` says "the ex-volcano stall in issue 312", so a search for `#312`
misses it. Match `issue\s+#?\d+` as well as `#\d+`.

**SAVE** means a save game is attached to the issue, which is most of them and is the reason these
are worth working from.

---

## 1. Already fixed on main, still open — verify and close (1)

| # | Title | Fixed by |
|---|---|---|
| 312 | Governor doesn't recognise terraformed tiles or tiles freed by an eruption | `54e182a1f` — names "the ex-volcano stall in issue 312", edits the tile picking in `Planet_EvaluateBuildings.cs` with tests |

Confirm against the attached save, then close with a reference to the patch that ships it.

**Do not close 293 with it.** `b870e1808` names issue 293 in its subject, but the issue reports
three separate problems and that commit fixes only the first. See the trade section below.

## 2. Has an open PR — leave alone (6)

| # | Title | PR |
|---|---|---|
| 307 | Demands in diplomacy can be ignored without a diplomatic effect | #385 |
| 317 | A platform under 10% hangar modules counts as a dedicated carrier | #367 |
| 318 | Ship order on the main screen is inconsistent | #349 |
| 320 | Ship requisitions in the fleet manager show 'Need spaceport' | #350 |
| 322 | Deleting a ship in the ship array doesn't update its status | #351 |
| 338 | The game forgets ship settings when saving and reloading | #388 |

All six are awaiting our re-review rather than awaiting Ludoal: every one carries a CHANGES_REQUESTED from gkapulis and every one pushed a "Review changes" commit on 2026-09-16 — see the pending
re-review notes before picking any of them up.

---

## 3. The worklist — no PR, or only a partial fix (34)

Grouped by suspected shared cause rather than by report order, because several of these are
probably one bug each.

### Attack warnings that never appear (3) — best first target

Three separate reports of the same missing signal, all with saves. Likely one root cause, so one
fix plausibly closes three issues.

| # | SAVE | Title |
|---|---|---|
| 281 | SAVE | No warning from a flashing star system that the Remnant are coming |
| 287 | SAVE | The Remnant attack without showing any attack signal |
| 334 | SAVE | Star system attack warning that never comes |

### Pathing and movement (4)

| # | SAVE | Title | Note |
|---|---|---|---|
| 344 | SAVE | Ships keep wanting to move to the planet of Opteris III | Almost certainly the parked lingering-`HasPriorityOrder` chase bug. That was parked **waiting for a save, and the save is attached here.** Start with this one. |
| 339 | | Errors in the pathing algorithm | screenshots only |
| 290 | | Pathfinding: complicated path through a dangerous star system | screenshots only |
| 319 | SAVE | Pathing algorithm doesn't recognise open borders treaty | **Blocked on the PR #411 decision.** That PR rewrites transit rules around open borders; it either fixes this, supersedes it, or changes what the fix should be. Do not start until #411 is resolved. |

### Remnant behaviour (7)

| # | SAVE | Title |
|---|---|---|
| 278 | SAVE | Remnant planetary defenders don't bomb planets |
| 286 | SAVE | The Remnant attack without bombers |
| 309 | SAVE | The Remnant balancers don't attack (anyone) |
| 331 | SAVE | Remnant Ancient Warmongers don't behave according to their protocols |
| 288 | SAVE | A Remnant defence fleet spawning from portal hangars persists if you save at that moment |
| 329 | SAVE | The AI has a very hard time dealing with kiting long-range Remnant |
| 346 | SAVE | Remnant fleets are a lot more powerful than the best AI fleets — balance |

278 / 286 / 309 / 331 are all "a Remnant force does not do the thing its role says it does", so
check whether they share a dispatch or role-assignment cause before treating them as four bugs.
329 and 346 are balance judgements and want Gilad's call, not a patch. 288 is really a save/load
bug wearing Remnant clothes — a fleet caught mid-spawn is serialized in a state it cannot recover
from — so it belongs with serialization work and its deeper review bar, not with this cluster.

### Fleets (5)

| # | SAVE | Title |
|---|---|---|
| 280 | SAVE | A single new ship added to a fleet slows the whole fleet to a crawl |
| 296 | | Fleet doesn't form correctly when first ordered or after a refit |
| 297 | | Multiple fleets overlap in the top left of the main screen |
| 302 | | Assigning ships to a fleet that is building that type places them at the spot under construction |
| 333 | | In fleet battles, assault shuttles are largely ignored |

280 is the one with a save and the clearest symptom.

### Colony and governor (3)

| # | SAVE | Title |
|---|---|---|
| 335 | SAVE | Governor doesn't complete colony blueprint |
| 282 | SAVE | Production left over from a manual rush auto-rushes the next building |
| 284 | | Platforms cannot be placed in orbit of planets close to a star from the deep space building menu |

335 is adjacent to the blueprint and tile-picking work already done in fixes_33 — check it against
current `main` first, it may already be fixed like 312 was.

### Combat (4)

| # | SAVE | Title |
|---|---|---|
| 315 | SAVE | My ships don't fire on competitor colony ships, the AI fires on mine |
| 279 | SAVE | Planetary repair doesn't seem to work |
| 283 | SAVE | Inconsistent values of base strength and offense value |
| 308 | | Glitchy tractor beams (reported by Judas, 12 May) |

### Trade and economy (3)

| # | SAVE | Title |
|---|---|---|
| 293 | | Trading population back and forth between planets — **partially fixed, 1 of 3** |
| 305 | SAVE | Trade display delay: importing/exporting freighter counts update once per turn |
| 304 | | Suggestion: distribute production requests among planets more evenly |

293 is the trap in this list. `b870e1808` closed the rule-1 vs rule-3 overlap — the
`]0.9, 0.99[` window where `BiosphereInTheWorks` alone decided the trade direction — and its
subject line reads as though the issue is done. Two reports in the same issue are untouched:

- **rule 1 vs rule 2**: a planet exports above 90% but imports below 80%, and a freighter is not
  capped at the gap, so a large hauler (worse with Manifest Destiny) overshoots past the import
  threshold and the ping-pong restarts at a slower period.
- **the import throttle**: a 20B-capacity world liberated from the Remnants accepts about five
  transports at a time, so filling it takes hundreds of trips while dozens of other planets sit on
  export. Spare freighters should be allowed to pile in.

### Diplomacy and AI (3)

| # | SAVE | Title |
|---|---|---|
| 310 | SAVE | Empire surrenders after losing their last planet |
| 291 | | The AI underestimates the difficulty of invading a capital |
| 289 | | Research penalty from espionage turns on and off, which is confusing |

### Content and enhancements (2)

| # | Title | Note |
|---|---|---|
| 340 | Inconsistent progression in the missiles and top ballistics lines | Combined Arms content — lives in the mod repo, not here |
| 292 | Enhancement: quest log for Remnant history | Design work, fits the parked Remnant-story pass |

---

## Suggested order

1. **Close 312** — verify against its save, then close. Cheap, and stops it being re-triaged.
   Leave 293 open; it needs the two remaining reports fixed first.
2. **344** — the save we were waiting for is attached, and the diagnosis is already parked.
3. **281 / 287 / 334** — three issues, probably one cause, all reproducible.
4. **335** — check against current `main` before writing anything.
5. **The Remnant role cluster** (278 / 286 / 309 / 331) — look for the shared cause first.
6. Everything else by whether a save makes it reproducible.

Leave **319** until PR #411 is decided, and treat **329 / 346** as balance questions for Gilad
rather than defects.
