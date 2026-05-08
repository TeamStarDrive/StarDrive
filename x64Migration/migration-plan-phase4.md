# Phase 4 Migration Plan — Polish, Mod Compatibility, Steam

## Context

[Phase 3](migration-plan-phase3.md) closed with the renderer at feature parity for the original 2013 art: 122 SunBurn-baked + 8 static-raw + 6 Ralyeh skinned XNBs all render through the offline FBX pipeline; particle/beam/projectile FX restored; bloom + screen-space distortion + fog-of-war post-process passes back; basic shadow maps; skinned-mesh animation playing on the Ralyeh ship17 family. The runtime is functionally complete — what's left is polish, mod compatibility verification, and the items that were always tagged as Phase 4 from the start (Steam SDK x64).

**Phase 3 carryovers** (from the Phase 3 plan's "Confirmed Phase 4 carryovers" section, in scheduling order):

| Carryover | Phase 3 status | Phase 4 sub-phase |
|---|---|---|
| **Combined Arms mod regression sweep** | §3.10.B.8 fixes apply uniformly across the corpus, but only Ralyeh ship17 a-f was visually verified | §4.2 |
| **Build hygiene: zero warnings on x64** | **DONE 2026-05-07** (re-baselined 16 unique C# warnings + 29 MSTest analyzer + 42 C++ warnings on Release\|x64; cleaned across StarDrive/SDGraphics/SDUtils/UnitTests + SDNative/SDNativeTests + NanoMesh submodule bump; WaE gate active on Release\|x64 only across all 6 projects; full matrix logs in `phase4-logs/wae/`) | §4.3 |
| **Performance vs Phase 2 baseline** | Soft cap from Phase 3 plan: <10% MainMenu, <20% peak combat. **DEFERRED to post-release** (rescoped 2026-05-08; §4.1 baseline at vsync-cap, no regression observed through §4.6 polish; not a Phase 4 close gate) | §4.4 |
| **YouLose desaturate visual** | **DONE 2026-05-07** (commit `f4449df2d`; root cause was the SpriteBatch position-form Draw trap, not the shader math — see `project_phase45_spritebatch_position_form_trap.md`; user-confirmed visual: zoomed-in close-up at fade-in renders fully grayscale, slowly zooms out + colorizes over 30s) | §4.5.A |
| **Light rig data rebake** | **DONE 2026-05-07** (commit `0e8b92900`; resolution path was option (2) — `LightRig` stub has zero data and `LightManager.Submit` was a no-op, so the load+catch was always functionally equivalent to "do nothing"; dropped the dead path) | §4.5.B |
| **Visual polish pass** (was §3.11) | 7 items: MainMenu polish residue, projectile dynamic glow light, glow-map emissive, muzzle FX check, sun Z, specular intensity, fog-of-war map circle dimness — plus a dedicated user-driven UI pass | §4.6 |
| **Mesh-export toolchain decision** | Two export-side fixes live on `legacy/mesh_exporter_xna31` only (`f964b6df7`, `5c3a218be`); decide whether to cherry-pick, resurrect on migration, or keep legacy as the dedicated re-export branch | §4.7 |
| **NanoMesh upstream PR** | Local `blackbox-migration` branch carries 7 commits not yet pushed upstream — fresh clone breakage today | §4.8 |
| **Steam SDK x64** | **DONE 2026-05-08 (rescoped)** — Steamworks.NET wiring deferred for public alpha; vendored x86 baggage scrubbed (commit `67514fc69`: dropped `GARSteamManager.dll` + `steam_api.dll` + `steam_appid.txt`, reduced `SteamManager.cs` 153→26 lines keeping the 6-method external public surface, scrubbed manifest entries). Recipe preserved in `migration-plan-phase2.md` "Deferred Final Step" appendix for the future revive | §4.9 |

**Related memory** (read these before starting any sub-phase):
- [project_phase4_legacy_mesh_export_sync.md](c:/Users/gkapu/.claude/projects/c--Development-stardrive-BlackBoxPlus/memory/project_phase4_legacy_mesh_export_sync.md) — three-option matrix for the toolchain decision
- [project_nanomesh_local_branch.md](c:/Users/gkapu/.claude/projects/c--Development-stardrive-BlackBoxPlus/memory/project_nanomesh_local_branch.md) — what's on `blackbox-migration` and PR options
- [project_phase3_3_youlose_desaturate_unresolved.md](c:/Users/gkapu/.claude/projects/c--Development-stardrive-BlackBoxPlus/memory/project_phase3_3_youlose_desaturate_unresolved.md) — four hypotheses + diagnostic checklist
- [project_phase2_backlog_runtime.md](c:/Users/gkapu/.claude/projects/c--Development-stardrive-BlackBoxPlus/memory/project_phase2_backlog_runtime.md) — Steam SDK execution recipe lives in `migration-plan-phase2.md` "Deferred Final Step" appendix

---

## Phase 4 Goals (Success Gate)

1. **All Phase 3 success-gate criteria still hold** (boot, MainMenu, navigation, 3D hulls render, beam weapons fire, animations play, build matrix green).
2. **Combined Arms mod runs end-to-end** with no visible regressions vs pre-migration. All hulls render; ships designable; combat reachable.
3. **Zero warnings** on `Release|x64` for both `StarDrive.csproj` and `SDNative.vcxproj`. Project warnings fixed at the source; vendored third-party warnings suppressed via `<DisableSpecificWarnings>` in the vcxproj. Warnings-as-errors gate enabled so future regressions can't sneak in.
4. ~~**Performance within budget**: <10% frame-time regression vs the Phase 2 baseline at MainMenu, <20% at peak combat. Both measured under identical scene loads on the same hardware.~~ **Deferred to post-release** (rescoped 2026-05-08). §4.1's baseline already showed vsync-locked 60 Hz across MainMenu/Universe/Combat; no perf regression has surfaced through §4.6 polish. A formal Phase-2-vs-post-§4.6 measurement pass is parked as post-release work — see §4.4. It is **not** a Phase 4 close gate.
5. **Visual polish pass** lands the seven items from the (former) §3.11 list plus a dedicated user-driven UI pass. Each item shipped with a pre/post screenshot pair.
6. **NanoMesh upstream** has a merged PR (or, if upstream rejects, a documented decision to keep the fork on a fixed tag).
7. **Steam x86 baggage scrubbed**. Vendored `GARSteamManager.dll` + `steam_api.dll` + `steam_appid.txt` removed from `game/`; `SteamManager.cs` reduced to a clean stub with the 6-method external public surface preserved. Full Steamworks.NET wiring deferred — public-alpha distribution doesn't justify it (see §4.9 reopen condition).

(**Release + sign-off** — cutting `mars-release-1.6.0`, signed installer, GitHub release, PHASE4_RESULTS.md, ARCHITECTURE.md update — moved to [Phase 5](migration-plan-phase5.md). §5.1 ships the artefact; §5.2 is the optional post-release migration close.)

**Anti-goals for Phase 4** (out of scope; revisit only if explicitly raised):
- Pixel-exact match to 2013 SunBurn deferred-renderer output. Forward-renderer-equivalent remains the bar.
- Save-game compatibility with pre-migration XNA 3.1 saves (separate workstream if ever needed).
- Network / multiplayer (none planned per ARCHITECTURE.md §5.6).
- HDR tone mapping.
- God-class refactor of `Fleet.cs` / `Empire.cs` / `ResourceManager.cs` (ARCHITECTURE.md §8 — gameplay debt, not migration debt).
- `Xna31ModelReader` runtime decoder (ARCHITECTURE.md §9 alternative path C). The offline FBX pipeline supersedes it; reach for this only if a mod ships an XNB Model that has neither an `.fbx` nor `.obj` sibling.
- Sound / music engine changes (already working).
- Wiring Steamworks.NET in Phase 4 at all. The full integration (achievements, stats, cloud saves, web overlay) is parked for public alpha — see §4.9 rescope and reopen conditions. Pushing a build to the Steam store via SteamPipe (or modifying the StarDrive Steam app's achievement/stat/cloud-save schema) is doubly out of scope: maintainer has no partner-backend admin access on AppID 220680.

---

## Confirmed Strategic Decisions

| Decision | Choice | Rationale |
|---|---|---|
| **Mod compat priority** | Combined Arms first, then a broader mod-dir sweep on best-effort. | Combined Arms is the largest StarDrive mod and ships its own hulls; it's the canary for export-pipeline correctness. Other mods are smaller and reuse vanilla content. |
| **Warnings cleanup scope** | Project warnings fixed at the source. Vendored `SDNative/3rdparty/` warnings suppressed via per-file `<DisableSpecificWarnings>`. `lodepng.cpp` (single file, ~20 warnings) and FBX SDK headers are vendored upstream code we don't own. | Fixing third-party at the source means carrying patches forward through every upstream bump; suppression is the conventional answer. Project code stays warning-clean as the gate. |
| **Warnings-as-errors gate** | Enable on `Release|x64` only after §4.3 lands. Debug + DebugAutoFast remain warning-tolerant during active development. | Release|x64 is the ship config; the others are dev configs where in-progress code shouldn't refuse to build. |
| **Performance baseline source** | Re-capture the Phase 2 frame-time baseline on the current hardware before measuring Phase 3 deltas. | Phase 2's "~16ms/frame at 1080p in MainMenu" was on a dev machine snapshot; comparing to today's Phase 3 timings without re-baselining mixes hardware with software. |
| **Mesh-export toolchain** | **Decide in §4.7 between three options** preserved in `project_phase4_legacy_mesh_export_sync.md`: (1) keep legacy-only, (2) cherry-pick into migration as dead source, (3) resurrect on migration toolchain. Default to (1) if no concrete need surfaces during §4.2. | The split exists because legacy carries the XNA 3.1 + XNAnimation stack required to read original XNBs. Resurrecting on migration is large work; cherry-pick adds dead source; legacy-only keeps things working but risks rot. |
| **NanoMesh PR path** | Push `blackbox-migration` head to NanoMesh upstream as a single PR (commits curated for review). If upstream rejects or stalls >30 days, pin the submodule to a tag in our fork. | A PR is the right thing to do and the fixes (skin/anim read+write, bind-matrix recovery, TransparencyFactor write fix) are general-purpose — likely accepted. Having a tag fallback means our build doesn't depend on PR merge timing. |
| **Steam SDK approach** | **Park + scrub** (rescoped 2026-05-08). Phase 4 deletes the x86 baggage and reduces `SteamManager.cs` to a clean stub; full Steamworks.NET wiring is deferred until BlackBoxPlus moves out of public alpha or the maintainer obtains partner-backend access. | Public-alpha distribution (itch.io, public repo, no Steam-store presence) gets nothing from Steam achievements/stats/cloud — every user reaches the binary outside the Steam launch path. Wiring it now is dead weight; scrubbing the x86 DLLs is still worthwhile for build hygiene. Steamworks.NET recipe preserved in `migration-plan-phase2.md` appendix for the future revive. |
| **Sign-off shape** | PHASE4_RESULTS.md mirrors PHASE1/2/3_RESULTS.md. Final ARCHITECTURE.md update marks the migration roadmap §9 items DONE. | Pattern consistency. ARCHITECTURE.md is the artifact future maintainers read first. |

---

## Sub-phase Index

| # | Title | Risk |
|---|---|---|
| 4.1 | Baseline checkpoint, Phase 4 branch, runtime + perf baseline — **DONE 2026-05-07** (tag `phase4-start` at `781a00f18`; build matrix 100/100/99/99/91 warnings, 0 errors; perf baseline vsync-locked at 60 Hz across MainMenu/Universe/Combat) | Low |
| 4.2 | Combined Arms regression sweep (mod compat — first) — **DONE 2026-05-07** (re-exported on legacy/mesh_exporter_xna31: 196/197 model FBX, 1 orphan `Vulfar/Alpha.xnb` benign-fail; copied corpus alongside CA xnbs; deleted 336 duplicate xnbs (56.4 MB) now superseded by FBX/.dds siblings; smoke clean — CA v8.7i loads, MainMenu+Universe render, 0 errors, 0 missing-texture warnings) | Medium |
| 4.3 | Build hygiene: zero warnings on `Release\|x64` — **DONE 2026-05-07** (re-baselined 16 unique C# warnings + 29 MSTest analyzer + 42 C++ warnings on Release\|x64; cleaned across StarDrive/SDGraphics/SDUtils/UnitTests + SDNative/SDNativeTests + NanoMesh submodule bump; WaE gate active on Release\|x64 only across all 6 projects; full matrix logs in `phase4-logs/wae/`) | Low–Medium |
| 4.4 | Performance baseline + targeted optimization — **DEFERRED to post-release** (rescoped 2026-05-08; §4.1 captured vsync-locked 60 Hz baseline; no regression surfaced through §4.6 polish; formal Phase-2-vs-post-§4.6 measurement pass is post-1.6.0 work, not a Phase 4 close gate) | Deferred |
| 4.5 | Backlog finishes: YouLose desaturate, light rig data rebake — **DONE 2026-05-07** (§4.5.A YouLose desaturate at commit `f4449df2d`, user-confirmed visual; §4.5.B Light rig at commit `0e8b92900`, dropped dead load+stub path) | Medium |
| 4.6 | Visual polish pass (was §3.11) — 7 prepared items + user UI pass — **DONE 2026-05-08** (10/11 items closed, 1 N/A: MainMenu polish residue, projectile dynamic glow lights, ~~glow-map emissive (N/A)~~, muzzle FX check, sun Z, specular intensity, FOW sensor dark ring, user UI pass, universe-screen ship lighting, universe-screen nebula brightness, atlas cache-version invalidation; pre/post screenshots committed; user visual sign-off) | Low–Medium |
| 4.7 | Mesh-export toolchain decision (legacy-only vs port vs resurrect) — **DONE 2026-05-08** (hybrid: `legacy/mesh_exporter_xna31` is the clean public re-export branch, `legacy/mesh_exporter_ca_patch` is local-only override stack, migration mirrors general fixes via commit `ab8184c48`; ADR + runbook in `x64Migration/adr-mesh-export-toolchain.md` + `x64Migration/re-export.md`) | Low |
| 4.8 | NanoMesh upstream PR — **PR open, awaiting review** ([RedFox20/NanoMesh#1](https://github.com/RedFox20/NanoMesh/pull/1) opened 2026-05-08; squash of 9 commits into single +687/-55 commit `8165536` on `gkapulis/NanoMesh:upstream-pr/fbx-skin-anim`; submodule pointer holds at `5acc08b` until merge) | Low |
| 4.9 | Steam SDK x64: park + scrub x86 baggage (Steamworks.NET wiring deferred — public-alpha rescope 2026-05-08) — **DONE 2026-05-08** (commit `67514fc69`: dropped `GARSteamManager.dll` + `steam_api.dll` + `steam_appid.txt`, `SteamManager.cs` 153→26 lines, 2 manifest entries scrubbed; Release\|x64 clean) | Low |

**Release + sign-off moved to [Phase 5](migration-plan-phase5.md)** (2026-05-08): §5.1 cuts the 1.6.0 release (was §4.11), §5.2 is the optional post-release migration close (was §4.10). Phase 5 has its own success gate, sub-phase index, and risk summary; reference it directly rather than tracking those items here.

Each sub-phase ends with a commit and is rollback-able via `git revert <sha>` or `git reset --hard <tag>`.

---

## 4.1 — Baseline Checkpoint, Phase 4 Branch, Runtime + Perf Baseline

**Goal**: Tagged starting point for Phase 4 with Phase 3 fully merged. Capture the runtime + performance baselines that §4.3 (warnings) and §4.4 (perf) measure deltas against.

**Steps**:
1. Confirm `migration/phase3-x64-monogame` has been merged to `migration/monogame_migration` (the Phase 3 sign-off PR must be merged before §4.2 starts).
2. Branch `migration/phase4-x64-monogame` from `migration/monogame_migration` head.
3. `git tag phase4-start`.
4. Build matrix: 5 configs × x64. Capture all 5 logs under `phase4-logs/baseline/`. Record warning counts per config (these are the §4.3 baseline numbers).
5. Runtime smoke: launch `game/StarDrive.exe`, walk MainMenu → New Game → Universe → combat → MainMenu → exit. Capture `phase4-baseline.log`.
6. **Performance baseline**: capture frame time at three identical scene loads using a deterministic save:
   - MainMenu idle (steady-state, post-fade-in).
   - Universe map at empire-start zoom level (no fleets in motion).
   - Combat: a saved fight with N ships on each side (pick a number that's stable across runs).
   - Sample for 60s per scene. Record p50, p95, p99 frame times to `phase4-logs/perf-baseline.md`.
7. Cross-check with Phase 2's stored baseline (per `cross-cutting concerns` in `migration-plan-phase3.md`: ~16ms/frame at 1080p MainMenu, 60fps achievable). If today's Phase 3 perf is wildly off, decide in §4.4 whether to invest in optimization or re-set the budget.

**Verification**:
- Build matrix green; all 5 logs captured.
- `phase4-baseline.log` and `phase4-logs/perf-baseline.md` committed.
- Tag `phase4-start` exists.

**Rollback**: `git checkout migration/monogame_migration && git branch -D migration/phase4-x64-monogame`.

**Risk**: Low. Pure setup + read-only measurement.

---

## 4.2 — Combined Arms Regression Sweep

**Goal**: Re-export every Combined Arms hull through the legacy/mesh_exporter_xna31 pipeline (the §3.10.B.8 fixes apply across the entire mesh corpus, but only Ralyeh ship17 a-f was visually verified). Run Combined Arms in-game and confirm no regressions vs pre-migration.

**Why this is first**: it's the highest-stakes correctness check left. The export-pipeline fixes during §3.10.B.8 (Scale/Rotation swap in `SDMeshAddBone`, Quat→Euler convention via `SDMeshAddBoneTRS`) touch every bone of every skinned export, not just ship17 a-f. Until Combined Arms is verified, we don't actually know whether the fixes generalized cleanly.

**Steps**:
1. Locate the Combined Arms mod under `game/Mods/`. Inventory the hull XNBs it ships and which directories they live in.
2. On `legacy/mesh_exporter_xna31`: confirm SDNative x86 build picks up §3.10.B.8 changes (cherry-picked at commit `c0d5b70e8`, `7b5fef051`). Rebuild SDNative x86 if not.
3. Run `StarDrive.exe --export-meshes=fbx` on the legacy branch with Combined Arms active so the exporter walks both vanilla and Combined Arms hulls. Capture the export log (volume only — full output is hundreds of meshes).
4. Diff `game/MeshExport/Mods/<CombinedArms>/...` against any pre-existing exports for the same paths. Anything that changes geometry vs metadata-only is a regression candidate.
5. Copy the new `.fbx` corpus into `game/Content/Mods/<CombinedArms>/Model/...` (and vanilla overrides as needed).
6. On migration: rebuild SDNative x64; load Combined Arms via the mod selector; smoke-test:
   - Ship Designer: every Combined Arms hull renders with materials, bones articulate visibly for any skinned hulls, no NaN/Inf clip space (silent invisible ships).
   - Universe combat: build one of each Combined Arms ship class, confirm in-game animation looks right (no tentacle-style "broken limbs" wobble like the §3.10.B.8 pre-fix symptom).
   - Save → reload → re-render: state survives.
7. If any hull regresses, capture the offending bone diagnostic via the `Ship17EndToEndTest` pattern (BindPose T/R/S vs frame-0 dump), root-cause, and apply the fix uniformly (likely a §3.10.B.8 follow-up, not a Combined-Arms-specific bug).

**Tests added**: none. Skipped per user call at §4.2 close (2026-05-07): Combined Arms hull XNBs are mod-owned and re-exported on demand, not pinned content; pinning a unit test against a representative hull would couple the test suite to mod-side asset stability. Smoke is the gate.

**Verification**:
- Combined Arms loads and renders end-to-end.
- All hulls visible; no invisible-ship NaN regressions; animated hulls articulate correctly.
- Build matrix green.
- 5+ minute interactive smoke session captured to `phase4-logs/combined-arms-smoke.log`.

**Rollback**: revert the FBX corpus drop. The `.xnb` originals stay in place under `game/Mods/`, so reverting puts the mod back in its pre-Phase-4 state.

**Risk**: Medium. The export-fix correctness was proven on Ralyeh ship17 a-f only; Combined Arms hulls may use different bone counts, hierarchies, or animation conventions that surface a §3.10.B.8 follow-up. Mitigation: §3.10.B.8 added thorough error-path diagnostics, so any regression should fail loudly with a useful message rather than rendering silently wrong.

---

## 4.3 — Build Hygiene: Zero Warnings on Release|x64

**Goal**: Drive both the C# (`StarDrive.csproj`) and the C++ (`SDNative.vcxproj`) builds to zero warnings on `Release|x64`. Enable warnings-as-errors as a permanent gate on the Release config.

**Baseline at Phase 3 close** (commit `c5c5159ea` / `bd413fbda`):

| Build | Count | Top categories |
|---|---|---|
| `StarDrive.csproj` (Release\|x64) | 30 | 8× CS0618 obsolete API; 4× each CS0108 hides-inherited / CS0649 unassigned-readonly / CS8509 non-exhaustive switch / CS8981 lowercase type names; 2× each CS8600 nullable / SYSLIB0014 WebClient / CA2014 stackalloc-in-loop |
| `SDNative.vcxproj` (Release\|x64) | 39 | 20 in `lodepng.cpp` (third-party); 5 in `SlabAllocator.h`; smaller clusters in `ObjectCollection.cpp`, `Mesh_Obj.cpp`, `ShipDataSerializer.cpp`, `SlabAllocator.cpp`, `Mesh_Fbx.cpp` |

**Steps**:
1. **C# pass** — fix project-internal warnings at the source:
   - `CS0618` (obsolete API): replace `DrawIndexedPrimitives(PrimitiveType, int, int, int, int, int)` with the 4-arg form per the deprecation message; ditto `WebClient` → `HttpClient` per `SYSLIB0014`.
   - `CS0108`: add the `new` keyword where the hide is intentional (`SkinnedLightingEffect.TryLoadShared`, `GameContentManager.LoadedAssets`).
   - `CS0649`: assign defaults or remove the field if genuinely unused (`ChoosePatrolPlan.Screen`, `Log.TraceContext.Trace`).
   - `CS8509`: add the missing arms (likely `Goods.None`).
   - `CS8981`: rename `pixelformatstruct` / `ddscapsstruct` to PascalCase (`PixelFormatStruct` / `DdsCapsStruct`) — risk: cross-references; grep first.
   - `CS8600` / `CA2014`: per-site review (one-line fixes typically).
2. **C++ pass — vendored third-party** — suppress in `SDNative.vcxproj` per file:
   ```xml
   <ClCompile Include="3rdparty\lodepng\lodepng.cpp">
     <DisableSpecificWarnings>4267;4334;%(DisableSpecificWarnings)</DisableSpecificWarnings>
   </ClCompile>
   ```
   Apply to all `SDNative/3rdparty/` sources hitting warnings. Don't blanket-disable on the project — keep first-party code under the warning gate.
3. **C++ pass — first-party** — fix at the source:
   - `Mesh_Fbx.cpp` C4267 (size_t → int narrowing in vertex/index counts): explicit cast or move to size_t-typed locals.
   - `SlabAllocator.h/.cpp`, `ObjectCollection.cpp`, `Mesh_Obj.cpp`, `ShipDataSerializer.cpp` C4267 / C4334: same shape — explicit casts at the boundary.
   - For first-party files in NanoMesh (submodule), commit there first then bump the submodule pointer.
4. **Enable warnings-as-errors** on `Release|x64` only:
   - `StarDrive.csproj`: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` inside the `Release|x64` PropertyGroup.
   - `SDNative.vcxproj`: `<TreatWarningAsError>true</TreatWarningAsError>` inside the `Release|x64` ItemDefinitionGroup.
5. Rebuild matrix; confirm Release|x64 builds clean. Other configs remain warning-tolerant for active dev work.

**Tests added**: none — the build itself is the gate.

**Verification**:
- `dotnet build StarDrive.csproj -c Release -p:Platform=x64` → 0 warnings, 0 errors.
- SDNative Release|x64 → 0 warnings, 0 errors.
- Warnings-as-errors gate active: introducing a fresh warning in a dummy commit fails the build.

**Rollback**: per-pass `git revert` of each commit. Vendored suppressions are project-file-only and trivially undone.

**Risk**: Low–Medium. The CS0618 swap is the largest blast surface (cross-file SpriteBatch / DrawIndexedPrimitives call sites); plan it as a single dedicated commit. The lowercase-rename CS8981 fixes risk cross-references — do those last after a wider grep audit.

**Status: DONE 2026-05-07.** Five commits on `migration/phase4-x64-monogame`:
- `293c86551` C# pass (16 unique warnings cleared: CS0108×2, CS0649×2, CS8600, CS8509×2, CA2014, CS8981×2 rename, CS0618×4 DrawIndexedPrimitives, CS0618 ModelMeshPart in tests, SYSLIB0014 WebClient → HttpClient with stream-chunk progress + CancellationToken bridge for the existing TaskResult cancel path).
- `25b94f4a3` C++ pass + NanoMesh submodule bump to `5acc08b` (lodepng vendored suppression; 13 unique first-party C4267/C4334/C4477 sites cleared in SlabAllocator h+cpp / ObjectCollection / ShipDataSerializer / SDNativeTests; NanoMesh Mesh_Obj lambda widened to size_t, Mesh_Fbx LoadMaterial loop uses int counter).
- `8bb2ad0c1` Test + SDK cleanup (29 MSTEST0039 mass-replaced `Assert.ThrowsException` → `Assert.ThrowsExactly`; MSTEST0006 ExpectedException → ThrowsExactly lambda; MSTEST0044 DataTestMethod → TestMethod; MSTEST0036 intentional-shadow #pragma; NETSDK1206 Libuv-RID NoWarn added to repo-wide `Directory.Build.props`).
- `860a22db0` WaE gate enabled on `Release|x64` only across StarDrive + SDGraphics + SDUtils + SDNative + SDNativeTests + SDUnitTests; SDGraphics gets `EnableMGCBItems=false` to silence the empty-mgcb info warning. Probe: `readonly string Probe;` field promoted to `error CS0169` under the gate (reverted).

**Final build matrix** (`x64Migration/phase4-logs/wae/`):
- `Release|x64` → 0 warnings, 0 errors  *(gated)*
- `Release - Auto Fast|x64` → 0 warnings, 0 errors *(no gate; coincidentally clean)*
- `Deploy|x64` → 0 warnings, 0 errors *(no gate; coincidentally clean)*
- `Debug|x64` → 2× LNK4098 (libpng/zlib MT vs SDNative MTd CRT mismatch — Phase 1 deferred)
- `Debug - Auto Fast|x64` → 2× LNK4098 (same)

**Tests added**: none, per the plan ("the build itself is the gate"). The probe trip on a deliberately-malformed field served as one-shot verification that the gate fires.

---

## 4.4 — Performance Baseline + Targeted Optimization

**Status: DEFERRED to post-release** (rescoped 2026-05-08). Not a Phase 4 close gate.

**Why deferred**: §4.1's baseline measurement (commit `9e32afd10`, captured 2026-05-07) showed the game holding vsync-locked 60 Hz across MainMenu, Universe, and Combat scenes — i.e. running at the framerate cap rather than below it, leaving headroom rather than burning it. No user-visible perf regression has surfaced through the §4.6 visual polish work that landed afterwards (FL10.0 shader bump, 8 dynamic light slots, projectile glow lights, lighting-effect-binder closest-light pick). The formal Phase-2-vs-post-§4.6 delta-table measurement is worth doing eventually but doesn't gate the 1.6.0 release.

**When to revisit**: post-1.6.0 release, paired with the first round of post-release polish work, or earlier if a user reports a real regression. The plan + recipe below remain valid for that future pass — only the timing changed.

**Goal** (preserved for the future pass): Validate that Phase 3's renderer additions (skinning, shadow map, post-process chain, material maps) fit within the Phase 2 perf budget. If a scene exceeds the soft cap (<10% MainMenu, <20% peak combat regression), apply targeted optimization. Maps to ARCHITECTURE.md §9 step 4c ("Performance profiling and optimization").

**Steps**:
1. Re-baseline Phase 2 timings on current hardware: check out `phase2-end` tag, run the §4.1 deterministic save through MainMenu / Universe / Combat scenes, capture p50/p95/p99 frame times. (Cheap to do because we want apples-to-apples.)
2. Capture the same scenes on `migration/phase4-x64-monogame` post-§4.2/§4.3.
3. Build a delta table: per scene, p50/p95/p99 absolute + percent change. Anything >10% MainMenu or >20% combat is flagged.
4. For flagged scenes, profile with PIX-on-Windows or RenderDoc to identify the hot pass:
   - Shadow depth pass cost? (Could trim cascade size or skip when no directional light cast-shadowers visible.)
   - Forward-lit fragment cost? (Material-map sampling adds 3 tex2D calls — was already measured against a no-maps reference in §3.7 step 4 contrast pass.)
   - Skinning palette upload? (One `SetValue(Matrix[])` per skinned draw — not per vertex.)
   - Post-process chain? (Bloom + distortion + fog compositing — reuse RTs cleanly.)
5. Apply the smallest change that brings the regression inside budget. Document the trade-off in commit messages and `phase4-logs/perf-fixes.md`.
6. Re-measure; commit perf logs.

**Tests added**: optional — a perf-regression unit test is hard to make robust on shared CI. Track perf as logs under `phase4-logs/`.

**Verification**:
- All three scenes inside budget.
- Delta table committed at `phase4-logs/perf-summary.md`.
- Build matrix green.

**Rollback**: per-fix `git revert`.

**Risk**: Medium. Hard to predict which pass will surface as the bottleneck without measurement. Mitigation: §4.1 captures the baseline first so §4.4 has data to work from, not vibes.

---

## 4.5 — Backlog Finishes: YouLose Desaturate, Light Rig Data Rebake

**Goal**: Resolve two small Phase 3 carryovers that don't fit into the §4.6 polish pass because they need their own diagnosis cycles.

### §4.5.A — YouLose / YouWin desaturate

**Status going in**: deferred 2026-05-03 per `project_phase3_3_youlose_desaturate_unresolved.md`. Best-effort attempts committed. Held-state visual still doesn't match pre-migration; user feedback on the final committed state was "still no luck".

**Diagnostic checklist** (from the memory entry — work through in order):
1. Run a release build (no debugger) for both pre and post migration on the same machine. Capture identical animation frames for direct comparison.
2. Add `Log.Info($"YouLose TP={TransitionPosition} Sat={Saturation}")` in `Update`. Verify values during fade-in vs held.
3. Test hypothesis #1: SpriteBatch + custom-PS-only effect texture sampler binding (mgfxc 3.8.1.303 may have the `BasicFogOfWar`-style parameter→sampler-state link issue). Output `tex2D(TextureSampler, uv)` directly to verify the SpriteBatch-bound texture is reaching the shader.
4. Test hypothesis #2: `Saturation` value reaching vertex color via `(byte)` cast.
5. Test hypothesis #3: held-state TP exact value (could be 0.01 instead of 0).
6. Test hypothesis #4: `scale = 1f + 2f * TransitionPosition` direction may be wrong.

**Resolution path**: fix if a hypothesis lands, otherwise document a final WONTFIX with the diagnostic data captured.

**Verification**: side-by-side video matches pre-migration, OR a documented WONTFIX with screenshots and traces in `phase4-logs/youlose-desaturate/`.

**§4.5.A status: DONE 2026-05-07.** User-confirmed visual: zoomed-in close-up at fade-in start renders fully grayscale, slowly zooms out + colorizes over 30s, held state shows full-fit colored battle scene.

Root cause was hypothesis #1 (SpriteBatch + custom-PS-only effect unreliable) PLUS a second, narrower trap that took live diagnostic to surface: the `position+origin+scale` form of `SpriteBatch.Draw(tex, pos, src, color, rot, origin, scale, ...)` produces **no rasterized fragments** under `SpriteBatch+custom-effect+Immediate` mode on MGFX 3.8.1.303 / DX11. A PS forced to return pure red turned the screen black with the position form, then red the moment we switched to the `Rectangle` form (`batch.Draw(tex, rect, color)`). That's why all four prior shader/formula iterations produced "navy / gray / black" symptoms whose actual cause was geometry never reaching the rasterizer — no shader rewrite could have helped.

Fix shape:
1. **`desaturate.fx`** — converted from PS-only to VS+PS. Adds `MatrixTransform` uniform + passthrough VS mirroring SpriteBatch's vertex format. PS body bytecode-faithful to the original ps_2_0 disassembly (`saturate(color.a * 4.0)` lerp from texture rgb to BT.601 luma). Recompiled to `desaturate.mgfxo` via `mgfxc /Profile:DirectX_11`.
2. **`YouLoseScreen.Draw` / `YouWinScreen.Draw`** — dropped `Pass.Apply()`; switched to `batch.Begin(Immediate, Opaque, LinearClamp, None, CullNone, desaturateEffect)` so our pass IS the SpriteBatch pass. `MatrixTransform` set manually before each Begin (SpriteBatch only auto-populates on `SpriteEffect`-typed effects). Replaced the `position+origin+scale` Draw with a manually-computed `Rectangle` that preserves the original zoom-out animation (`scale = 1 + 2*TP`, recomputed as `int(W*scale) × int(H*scale)` centered on screen).

Build matrix: `Release|x64` clean, 0/0. New memory entry `project_phase45_spritebatch_position_form_trap.md` captures the position-form trap so future contributors don't burn the same diagnostic cycle.

### §4.5.B — Light rig data rebake

**Status going in**: `GameScreen.AssignLightRig` catches the load failure and assigns an empty `LightRig`. Light rig XNBs are baked against SunBurn type-readers that are gone post-1.9; `LightRig` itself is a stub with no data, so even on success there's nothing to extract.

**Steps**:
1. Audit which scenes/screens currently call `AssignLightRig` and what they expect from a "real" rig.
2. If the answer is "nothing visible" (the catch-and-empty path is already correct), drop the catch and the empty-rig fallback; replace with explicit no-op + comment.
3. If real rig data is needed, rebake the rig content as plain YAML / JSON (per the original TODO) and load it via a regular `Content.Load<T>` path that doesn't depend on the SunBurn pipeline.

**Verification**: if (2), the catch goes away and the runtime behavior is unchanged. If (3), affected scenes show the intended lighting.

**Rollback**: per-step `git revert`.

**Risk**: Medium. §4.5.A is genuinely diagnostic-heavy — reserve a budget and accept WONTFIX if the four hypotheses don't land. §4.5.B is small but depends on §4.5.A's outcome (both touch lighting/visual passes).

**§4.5.B status: DONE 2026-05-07.** Resolution path was option (2) — confirmed `LightRig` stub has zero data (`public class LightRig { }`) and `LightManager.Submit(LightRig)` was a no-op, so the load+catch was always functionally equivalent to "do nothing". Single commit removes:
- `GameScreen.AssignLightRig(identity, rigContentPath)` → `GameScreen.AssignLightRig(identity)` (drops the path arg + try/catch + `TransientContent.Load<LightRig>`).
- `ScreenManager.AssignLightRig(identity, LightRig rig)` → `ScreenManager.AssignLightRig(identity)` (drops the now-permanently-null rig param + dead `Submit` branch).
- `ILightManager.Submit(LightRig)` interface method + `LightManager.Submit(LightRig)` impl + the empty `LightRig` stub class itself (zero remaining callers in Ship_Game; SDSunBurn isn't referenced from `StarDrive.csproj`, no namespace collision risk).
- 3 callsites updated: `ShipDesignScreen` / `FleetDesignScreen` / `UniverseScreen.ResetLighting`.

Build matrix: `Release|x64` + `Debug|x64` both 0 warnings 0 errors. Test suite identical pre/post-refactor (63 passed, 1 skipped, host-finalizer NRE in `UniverseState.Dispose` is pre-existing baseline noise unrelated to this change).

---

## 4.6 — Visual Polish Pass (was §3.11) — DONE 2026-05-08

**Goal**: Land a curated set of small finishes that surfaced during §3.3–§3.10 implementation but didn't fit any single earlier sub-phase, plus whatever the user surfaces during a dedicated UI pass. Each item is independently scoped, individually revertible, and gated only by user visual sign-off — no automated test gate beyond "build matrix green between commits".

**Items** (commit one per item; land in any order):

1. **MainMenu polish residue** — DONE 2026-05-08. User sign-off after three rounds of fixes. Sub-fixes landed:
   - **1.a Mars scene over-bright** (commit `b8b4ee29d`) — `MMenu.Mars.scene.yaml` `SunIntensity 4.0 → 3.0` and `AmbientIntensity 0.75 → 0.5625` (both ×0.75). Venus scene untouched. User-driven adjustment after the §4.6.B(b) specular work made the highlights read as too hot on the menu.
   - **1.b FTL warp effects invisible on MainMenu** (commit `b8b4ee29d`) — freighters cycle through `WarpingOut → IdlingInDeepSpace → WarpingIn`, but the warp flash sprites never appeared. Diagnostic instrumentation in `FTLManager.Enter/ExitFTL` showed both calls firing on schedule but with `radius=0.0`, which collapsed the entire effect to a 0-scale sprite via `flashSize = Math.Min(radius * 0.75f, 200f)` ([FTLManager.cs:102](Ship_Game/FTLManager.cs#L102)). Root cause: `StaticMesh.CreateSceneObject` ([StaticMesh.cs:116](Ship_Game/Data/Mesh/StaticMesh.cs#L116)) never copied the loaded mesh's `Bounds` onto the resulting `SceneObject.WorldBoundingBox`, leaving it at the C# default `(0,0,0)–(0,0,0)`. `SceneObj.HalfLength` (`SceneObj.cs:163`) reads that as zero and passes it through to FTLManager. Fix: in `CreateSceneObject`, set `so.WorldBoundingBox = Bounds` and `so.ObjectBoundingSphere = new BoundingSphere(center, Radius)` so every consumer of the SceneObject's bounds (FTL radius, hull-size estimate, debug overlays, future picking) sees real values. Affects every SceneObject built via `StaticMesh.CreateSceneObject` — i.e. all ships, stations, asteroids, weapon meshes — so this also fixes any latent zero-bounds bug in the rest of the codebase, not just the MainMenu warp flash.
   - **1.c BackAdditive layer over-attenuated** (commit `15c9156b9`) — user reported missing colored cloud puffs (Dust, Aurora) around the Mars planet. Pipeline diag confirmed all 4 BackAdditive panels reach the gather pass with reasonable positions/sizes; isolation test (every other panel hidden, Dust/Aurora forced to neon colors) confirmed the BackAdditive render path is sound. Root cause was Phase 3.7's custom `SoftAdditive` blend (`src*(1-dst) + dst`, `Blend.InverseDestinationColor`) attenuating the additive contribution heavily where the planet panel was bright underneath. Switched to canonical `BlendState.Additive` (`src*srcAlpha + dst`) — the pre-migration intent. The Aurora/Dust textures remain inherently subtle (mostly black with small colored clusters); per-panel `MaxColor` knobs in `MMenu.Mars.yaml` dial intensity if more lift is wanted later.
2. **Projectile dynamic glow light** — DONE 2026-05-08. Projectiles cast a per-projectile point/dynamic light onto nearby ships and stations via the forward renderer's existing `LightingEffect` point-light path. After §4.6.B's FL10.0 bump lifted the 2-slot const-pool cap to 8 slots and `LightingEffectBinder.Apply` was wired to pick the closest lights by XY, beam/projectile travel now illuminates flanking hulls visibly per pre-migration footage. User visual sign-off confirmed.
3. **Glow-map light points** *(optional / investigate)* — N/A 2026-05-09. User-driven decision: not applicable for now. Originally scoped as "promote glow-map texture channels from a flat additive overlay to an actual emissive light contribution (per-pixel emissive in the lit pass, optionally seeding small dynamic point lights at high-intensity glow-map pixels for nearby-surface bounce). Only land if it improves perceived quality vs the simpler additive-overlay baseline." Current additive-overlay baseline reads acceptably; the cost/benefit of the lit-pass extension doesn't clear the bar at this stage of the migration. Reopen if a future visual pass surfaces a clear win.
4. **Muzzle effects check** — DONE 2026-05-09. User-confirmed muzzle-flash particle emitters fire on weapon discharge end-to-end (no regression vs §3.5 particle FX restoration). User flagged that further visual tweaks are wanted but explicitly out of scope for this migration project ("needs tweaks but not in this project's frame"). Closing the migration-side regression check; any future tweak lands as separate gameplay-polish work outside Phase 4.
5. **Sun Z / depth ordering** — DONE 2026-05-09. User-driven Sun Z adjustment landed in earlier work; user signed off ("we changed the sun Z and it's ok for now"). Closing the item against current visual bar; revisit only if a future depth-ordering regression surfaces.
6. **Specular intensity** — DONE 2026-05-07. Landed alongside the §4.6.B FL10.0 bump in commit `a5659e81c` ("FL10.0 bump, 8 dyn slots, restored PL specular"). The bump restored the per-pixel point-light specular that had been dropped under the FL9.3 const-pool budget; a follow-up re-export of 19 ships with corrected SpecularFactor cluster-medians landed in `b234bc1a7`. Per-ship overrides documented in `x64Migration/specular-overrides.md` and a survey unit-test added to detect future regressions (commit `c66030648`).
7. **Fog-of-war sensor circle dark ring** — DONE 2026-05-07. *(User-reported as "too dim at edges"; diagnosis revised the framing.)* Each ship's sensor reveal circle on the Universe map showed a thin dark ring/halo at its outer boundary post-migration — pre had a smooth gradient fade. Root cause: `TextureInfo.SaveAsDds` was caching nopack Color+alpha textures (including `UI/node`, the FOW sensor mask) as DXT5 in the on-disk atlas cache. DXT5 alpha encoding has only 8 distinct alpha levels per 4×4 block; smooth alpha gradients quantize into stepped bands that read as a dark fringe at the visible edge under premul AlphaBlend. `DrawSensorNodesHighlights`, `DrawColoredEmpireBorders`, and any other consumer of `UI/node` inherited the artifact. Fix: switched the nopack cache for Color+alpha textures from DXT5 DDS to lossless PNG. `SaveAsDds` returns the actual saved path (`.dds` or `.png`); `CreateAtlas` uses that for `UnpackedPath`; `LoadAtlasFile` probes both extensions on load. `TextureAtlas.Version` bumped 26→27 to invalidate stale caches. Detailed analysis (including the 4 red-herring candidates ruled out before DXT5 surfaced) in `project_phase46_dxt5_alpha_quantization_trap.md`.
8. **User UI pass** — DONE 2026-05-08. Dedicated UI walkthrough by the user across MainMenu, Empire, Ship Designer, Universe, Combat, ColonyScreen, Diplomacy, ResearchScreen, EmpirePatrolsScreen, save/load to flag issues the prior seven items missed. Three sub-fixes landed; one investigated-and-deferred item parked.
   - **8.a Scroll-list items washing out of the frame** — visible in empire-selection (race traits list spilling past the bottom of the middle frame as "Slow Modulator", "Battle Genius") and ship-design (module-select items leaking below their housing). Root cause: `ScrollListBase.Draw` set `device.ScissorRectangle` *after* drawing items, then called End/Begin/DisableScissor with no draws between, leaving the items unscissored. The XNA pattern relied on `device.RenderState.ScissorTestEnable=true` taking effect at `End()` time, but MonoGame captures the rasterizer state at `Begin()`, not End. Fix: cached `RasterizerState ScissorEnabled` (CullCCW + ScissorTestEnable=true) in `RenderStates`; new `SafeBegin(blendMode, RasterizerState)` overload on SpriteBatch that pins the rasterizer at Begin; `ScrollListBase.Draw` reordered so the scissor wraps the items+highlight draw instead of running on an empty batch. Items now clip cleanly to `ItemsHousing.Bevel(1).Widen(8)`. Affects every scroll list (race archetype, traits, fleet design, ship design module select, colony build, espionage, diplomacy, etc.).
   - **8.b Empire-selection middle frame top edge misaligned with left/right** — the Traits SubmenuScrollList sat ~20 px below the ChooseRaceList / DescriptionTextList tops because of `traitsList.Bevel(-20)` (X+20, Y+20, W-40, H-40). User hadn't noticed pre-migration because the items leaking out below (the §8.a bug) drew the eye to the bottom. Fix: `Bevel(-20)` → `Bevel(-20, -10)` — keep the 20 px X inset (so the middle is narrower than NameMenu) but shift Y down by 10 to compensate for the texture-padding delta between Submenu's `submenu_corner_TL` and Menu1's `menu_1_corner_TL` (Submenu's drawn line lands a few px lower at the same Rect.Y, so a small +Y push lines up the visible top edges). User-tuned by eyeball: -3 was too high, -10 lined up.
   - **8.c Ship-design shipyard light too dim and too yellow** — pre-migration SunBurn `.lightRig` was content-pipeline'd; `ShipDesignScreen.SetupShipyardLighting` (Phase §4.6.B follow-up) reconstructed a 3-light Key/Fill/Back rig manually. User feedback: hulls read too warm and too dim, especially on cool-toned designs. Fix: Key DiffuseColor `(1, 0.9608, 0.8078)` → pure white `(1,1,1)`, intensity `1.0 → 1.75`; Fill DiffuseColor `(0.9647, 0.7608, 0.4078)` → soft cool white `(0.85, 0.88, 0.92)`, intensity `0.6 → 0.8`. Back rim and ambient unchanged. Trade-off: brass/gold accents now read cooler vs the pre-migration "warm shipyard" mood, but specular still has 3 half-vectors to fire on.
   - **Black squares at scroll-list header bar corners (deferred)** — `MISSILE`/`BALLISTIC CANNON` etc. headers show a small dark cutout at each corner of the `Selector` bar. Diagnosed as the rounded-corner texture (`NewUI/rounded_TL/TR/BL/BR`) deliberately having alpha=0 in its outer-corner pixels so the dest "rounded cutout" shows through. Pre-migration the dest was uniformly dark and the cutout was invisible; post-migration the layered scroll-list / Menu1-fillRect / nebula stack lets a brighter pixel leak through, reading as a small dark square. Atlas data verified correct (NewUI is in `AtlasNoCompressFolders`, premul is applied at SaveAsPng, `rounded_TL` alpha mask preserved in the cached PNG). User chose to skip the fix for now; trial fix (extend FillRectangle to cover the full rect) was applied and reverted. Re-open if it surfaces as a higher-priority polish item later.
9. **Universe-screen ship lighting variability** — DONE 2026-05-08. *(Originally framed as a per-mesh lighting-binder gap when surfaced during §4.6 #10 work, 2026-05-07.)* Root cause turned out to be much narrower: a small set of FBXs were exported with `DiffuseColor=(0,0,0)` (3 CargoHaulers — pure zero) or near-zero (9 asteroids — `(0.02, 0, 0)`). The shader formula `rgb = (ambient + diffuseAcc) * texColor.rgb + specularAcc + emissive` multiplies both ambient and diffuseAcc by `DiffuseColor`, so a near-zero source kills the entire diffuse path regardless of how good the texture sample is — only specular highlights and emissive (thrust glow) survive. Hence the screenshots: chrome-rock asteroids and silhouette cargo shuttles. Fix at the import boundary in `MeshInterface.CreateMaterialEffect`: when all three channels of `mat->DiffuseColor` are below 0.05, substitute `Vector3.One` and log which FBX hit it. The 100 other ship FBXs in the corpus have correctly non-zero diffuse and pass through untouched. The legacy exporter on `legacy/mesh_exporter_ca_patch` faithfully copied whatever the source XNB material's diffuse was, including these degenerate near-zero values — but the runtime intent in those FBXs is "use the texture unmodulated" (i.e. (1,1,1)), not "render as black", so the substitution is safe. Diagnostic test extension: `FbxMaterialDumper.MaterialRow` now captures Ambient/Diffuse/Specular/Emissive RGB + Alpha; `DumpBlackHullMaterials_AsteroidsAndCargo` is a focused diag that prints to console.
11. **Atlas cache version-bump invalidation regression test** — DONE 2026-05-07. Verified passively as a side-effect of the §4.6 #7 fix (which bumped `TextureAtlas.Version` from 26 → 27 and re-routed nopack Color+alpha through PNG). After the user's first launch on the new code, every sampled `*.atlas` descriptor in `%APPDATA%/StarDrive/TextureCache/` reads version=27 (Flags, Textures, Textures_Arcs, Textures_Buildings, Textures_Conduits, Textures_EmpireTopBar, Textures_EndGameScreen, Textures_FTL, Textures_GameScreens, Textures_Goods, …) and the `Textures_UI/` cache contains both the legacy `node.dds` (cruft from the V26 build) and the new `node.png` (V27 PNG output). Mechanism confirmed working — no regen bug.
10. **Universe-screen nebula brightness** — DONE 2026-05-07. Pre-migration scenes read with a vivid green wash dominating the entire universe screen at all zoom levels, with stars washed out. Root cause: `Clouds.fx` PS routes the macro noise layer to the G channel (`float4(detail.r, macro.g, detail.b, a)`) and `Background.cs` calls `sr.Draw(CloudTex, ..., Color(0, 255, 255, 255))` with a pure-cyan tint that zeros R and lets G+B pass through at full strength. Pre-migration SunBurn's deferred-composite tone-curve attenuated this; migration's forward path writes raw to the backbuffer. Fix: dim filterColor from `(0, 255, 255, 255)` → `(0, 48, 48, 255)` (~19% intensity) to empirically replace the missing tone-curve. Detailed analysis in `project_phase46_clouds_cyan_filter_tonemap.md`. Diagnosis cycle was lengthy because plausible-looking suspects (`Background3D` wisps, `BackgroundNebula` DXT1 quad, premul/non-premul texture-load delta, stale atlas cache) all turned out to be red herrings — the breakthrough was a "comment out background draw entirely" experiment that ruled out the obvious source and forced a re-grep of `RenderBackdrop`.

**Steps**:
1. Capture pre-state visual reference (screenshot/video) per item before touching code.
2. Implement, screenshot, commit. One item per commit; commit message references the item number above (`§4.6 #8.b: tooltip vertical centering` etc.).
3. **For item 8**: schedule the user's UI pass in a single dedicated session (1–2 hours). Capture the punch list as it surfaces; don't try to fix during the walkthrough. Triage and fix afterward, one issue per commit.
4. After all items land, run a 5-minute MainMenu→Universe→Combat smoke to confirm no cross-item regression.

**Tests added**: None automated. Visual-diff captures into `phase4-logs/visual-diff/` per item.

**Verification**: User visual sign-off per item against pre-migration reference footage. Build matrix green between commits.

**Rollback**: Per-item `git revert <sha>`. Items are independent.

**Risk**: Low–Medium. Each item is bounded; no system-wide changes. Specular magnitude and projectile dynamic light could touch the lighting effect's parameter surface — keep changes additive and uniform-gated like §3.8's `ShadowParams` packing so a regression flips one number rather than restructuring the effect.

---

## 4.6.B — Shader Profile Bump: FL9.3 → FL10.0 — DONE 2026-05-07

Landed in commit `a5659e81c`. All `.fx` files compiled under `vs_4_0` / `ps_4_0`, dynamic light slot count lifted from 2 → 8, point-light specular restored. `b234bc1a7` re-exported 19 ships with cluster-median SpecularFactor; `c66030648` added the CA-tree spec-override doc + survey tests. No regressions observed across universe + combat + UI smoke.


**Goal**: Lift `vs_4_0_level_9_3` / `ps_4_0_level_9_3` to `vs_4_0` / `ps_4_0` across all `.fx` files. The FL9.3 target was inherited from the original 2013 game's Intel HD 3000 / GeForce 8-series minimum spec — pre-DX10 hardware that's now 18+ years old and effectively extinct on Steam. The FL9.3 PS const-pool cap (32 vec4 registers) is the binding constraint behind multiple §4.6 limitations (dynamic light cap of 2, point-light specular dropped, hard shadow edges, packed FogStartEnd, packed PointLight slots) and blocks several future features (HDR composite, environment cubemap, multi-caster shadows). Bumping to FL10.0 gets the full DX10 const buffer (~4096 registers) and unblocks the rest.

**Surfaced from**: §4.6 #2 implementation review — user surfaced "why do we need to support pre-2008 GPU? it's 2026 and the game is from 2013".

**Steps**:

1. **Pre-flight: confirm minimum-spec promise.** Grep README, Steam-page metadata, in-repo docs (`*.md`), and any FAQ for "DX9", "DirectX 9", "Shader Model 3", "FL9.3", "Feature Level 9", or specific old-GPU promises (e.g. "Intel HD 3000"). If any exist, the bump becomes a user-facing minimum-spec change and needs a public note alongside the code change. If none exist (likely — the migration project's existing docs target modern dev environments only), the bump is purely internal.
2. **Update release notes / minimum-spec documentation.** Regardless of whether step 1 found a pre-existing promise, the new build *does* drop pre-DX10 support and that's a user-visible compat change. Track this in two tiers — what gets updated **now**, vs what waits for **release**:
   - **Now (in-repo, dev-facing)**: append the bump to `RELEASE_NOTES.md` / `CHANGELOG.md` (rolling release-prep doc, updated as work lands), in-repo migration docs (`MIGRATION_LIMITATIONS.md` — flag #4.6.B as resolving items #1, #2, #3, #4, #6 but introducing the DX10 floor), and the §4.6.B commit message itself so the bump is discoverable from `git log`.
   - **At release (user-facing)**: top-level `README.md`, the Steam page (if applicable), the modding-community wiki / forum sticky if any. Defer until the project actually ships — no point telling users about a minimum-spec change while the migration is still in flight, and the wording is easier to finalize once the released-state spec is fully known. Track as a release-checklist item rather than a §4.6.B step.
   - Standard wording (use in all locations): "Minimum GPU is now DirectX 10 / Feature Level 10.0 (any GeForce 8400+ / Radeon HD 2400+ / Intel HD 4000+ / 2008+). Pre-DX10 hardware is no longer supported."
3. **Audit the shader corpus.** Grep `game/Content/Effects/*.fx` for `vs_4_0_level_9_3` / `ps_4_0_level_9_3` — record the full list. Expected hits: `MeshLighting.fx`, `SkinnedEffect.fx`, plus the post-process shaders (`BloomExtract`, `BloomCombine`, `GaussianBlur`, `desaturate`, `scale`, `Clouds`, `PlanetHalo`, `BeamFX`, `Distort`, `Thrust`, `BasicFogOfWar`, `Shadow`, `Simple`, `ParticleEffect`).
4. **Bump technique passes.** Replace `_4_0_level_9_3` → `_4_0` in every `.fx` technique. Recompile each via `mgfxc` to confirm no shader hits a previously-unknown FL10.0-specific issue (none expected — FL10.0 is a strict superset of FL9.3 features).
5. **Smoke-test the build.** `dotnet build StarDrive.csproj` clean. Run the Graphics-tag unit tests (`dotnet test --filter "FullyQualifiedName~Graphics"`) — should all pass unchanged because the tests check parameter existence, not register counts.
6. **Walk the lit visuals.** Universe view, ship designer hangar, combat scene with weapons firing, planet halo at low N·L, FOW reveal at sensor-circle edge. Each should look identical to pre-bump — no FL10.0 path gives different output for the same constants. If anything diverges, root-cause before proceeding.
7. **Decide on packing rollback.** The §4.6 #2 packing (PointLight slots packed to PositionAndRadius+DiffuseAndEnabled, sun PointLight specular dropped, FogStartEnd packed) was forced by the FL9.3 register cap. With FL10.0, the cap is gone. Two paths:
   - **(a) Keep the packing**: zero downside beyond denser shader code. Status-quo behavior. Lowest risk.
   - **(b) Restore separate uniforms**: unpacks the float4-packed slots and brings back PointLight specular. Cleaner shader code, restores the §4.6 #2 limitation entry to "no longer applies". Recommend (b) as a follow-up, separate commit, after any blow-out testing.

**§4.6.B prerequisite tracker** — these limitations log entries in `MIGRATION_LIMITATIONS.md` are downstream of FL9.3 and become resolvable once §4.6.B lands:

- #1 Dynamic projectile lights cap of 2 — FL10.0 unlocks 8+ slots cheaply.
- #2 Point-light specular dropped — restore in the unpacking pass (path (b) above).
- #3 Single shadow caster — multi-caster needs more shadow samplers, fits comfortably in FL10.0.
- #4 Hard 1-tap shadow edges (no PCF) — PCF kernel adds shader instructions, FL9.3 was tight on instruction count too.
- #6 3 fixed PointLights per system anchor — more sun slots become viable.

**Tests added**: None automated. The Graphics-tag tests already cover parameter-surface regression.

**Verification**:
- All `.fx` files compile under FL10.0 via `mgfxc`.
- `dotnet test --filter "FullyQualifiedName~Graphics"` passes (currently 22 tests).
- Visual smoke (universe + combat + UI) — no perceptible regression vs the FL9.3 build.
- Optional: capture before/after shader-disasm via `mgfxc /OutputDirectory:asm` for `MeshLighting.fx` to confirm the bumped profile generates equivalent code.

**Rollback**: Single revert. Each `.fx` change is one line. The C# `LightingEffect` parameter handles still resolve under either FL9.3 or FL10.0 because the uniform names didn't change.

**Risk**: Low. FL10.0 is a strict superset; no FL9.3 feature is dropped. The user-facing risk is narrow (someone with genuinely pre-DX10 hardware loses the ability to launch — the game would fail at shader load). Pre-flight step 1 is the gate that determines whether this risk is real.

---

### 4.6.B follow-up: keeping a soft cap on visible projectile glows

After FL10.0 lifts the register cap, the question of "how many projectile glows simultaneously" becomes a perf/aesthetic decision rather than a hardware constraint. Three layers can enforce a cap independently:

1. **Shader slot count (hard cap)**. Even with abundant register budget, the shader still iterates a fixed N slots per pixel. Pick N for predictable per-pixel cost (e.g. 8 — keeps shader fast on integrated GPUs, generous enough that 99% of scenes never saturate). N can be larger if HDR/composite work lands and PS perf is no longer the bottleneck.
2. **C# binder selection (soft cap, perceptual)**. The binder (`LightingEffectBinder.Apply`) already iterates all submitted lights and picks the closest by XY distance. Whether 8 slots are filled or only 4 is decided here based on what's nearest the camera. Off-screen and far-from-camera lights drop out naturally.
3. **Submission-time queue cap (existing)**. `GlobalStats.MaxDynamicLightSources` (currently 100, configurable via `app.config` and Options screen) already gates how many lights can exist in `LightManager.ActiveLights` at once. Acts as a global throttle independent of the binder.

This three-layer setup means the cap on visible glows is intentional, not a hardware fallout. Useful for tuning busy-fleet perf without re-touching shader code: lower #2's "fill" count, or lower #3's queue cap for low-end machines via the existing Options slider.

---

## 4.7 — Mesh-Export Toolchain Decision — DONE 2026-05-08

**Decision**: Hybrid of options (1) + (2). `legacy/mesh_exporter_xna31` stays as the clean public re-export toolchain (no project-specific overrides); `legacy/mesh_exporter_ca_patch` is a local-only override stack for our blackbox/CA corpus; migration mirrors the general fixes only (hand-port, since `MeshExporter.cs` is structurally different from legacy after Phase 3.4 step 1).

**Landed**:
- ADR `x64Migration/adr-mesh-export-toolchain.md`
- Runbook `x64Migration/re-export.md`
- Migration commit `ab8184c48` — surgical port of `f964b6df7` + `5c3a218be` (parent-bone walk, cross-folder texture refs, axis-swap drop, lone-texture null-check, log-level demotion). Verified Release|x64: 0 warnings, 0 errors (WaE gate active).
- `legacy/mesh_exporter_xna31` reset to origin tip (`7b5fef051`) — clean. `legacy/mesh_exporter_ca_patch` retained at `dfb730278` as local-only.
- Memory: `project_phase4_legacy_mesh_export_sync.md` marked RESOLVED.

**Why not pure (1)**: drift between legacy and migration source trees was already 5 commits at §4.2 close; future divergence would compound. Mirroring general fixes keeps them paired.

**Why not pure (2) verbatim**: migration `MeshExporter.cs` dropped `SgMotion` / `SunBurn.LightingSystem.Effects` deps in §3.4 step 1; verbatim cherry-pick produced full-file conflicts and would have broken the WaE-gated build.

**Why not (3)**: §3.4 step 5 (`Xna31VertexDeclarationReader`) is partial; the skinned-export bridge to NanoMesh is unbuilt; export cadence does not justify the investment. Reopen if the legacy branch ever stops building or external mod authors need a single-toolchain workflow.

**Goal** (original): Pick one of the three options preserved in `project_phase4_legacy_mesh_export_sync.md` and execute it. Stop the situation where re-exports require a manual cross-branch toolchain switch.

**Options** (preserved verbatim from the memory entry):
1. **Status quo (legacy-only)** — re-export workflow always switches to `legacy/mesh_exporter_xna31`. Migration code is loader-only.
2. **Cherry-pick into migration** — port the two commits (`f964b6df7`, `5c3a218be`) as dead source so history is consistent.
3. **Resurrect exporter on migration** — adapt `MeshExporter` to the migration toolchain. Single-toolchain workflow long-term.

**Decision criteria** (apply at sub-phase start, after §4.2 has surfaced any new export bugs):
- If §4.2 surfaced no new export bugs **and** there's no near-term plan to extend the export path (new mod content, new vertex format, etc.): pick (1). Document explicitly that future re-exports use the legacy branch.
- If §4.2 surfaced bugs that only a migration-toolchain exporter can fix cleanly: pick (3) with a budget.
- (2) is the worst of both worlds — picked only if (1) loses to "legacy branch is at risk of bit-rot" and (3) loses to budget.

**Steps**:
1. Re-read `project_phase4_legacy_mesh_export_sync.md` and check whether §4.2's findings change the analysis.
2. Make the decision; commit a short ADR-style note under `x64Migration/` capturing the choice and reasoning.
3. Execute:
   - (1): write a `re-export.md` runbook capturing the exact legacy-branch commands so future re-exports don't require institutional knowledge.
   - (2): cherry-pick the two commits onto migration; mark with a comment that the source is dead-on-arrival here (won't compile on net8 + MonoGame).
   - (3): port `MeshExporter` to MonoGame's `Model` API surface; verify on a re-export of ship17a-f to confirm the migrated exporter produces byte-equivalent FBX to the legacy one.
4. Update `project_phase4_legacy_mesh_export_sync.md` with the resolution.

**Verification**: re-export workflow is documented (1) or single-branch (3); legacy branch's role in the project is settled.

**Rollback**: trivial for (1) and (2). For (3), `git revert` the port commits.

**Risk**: Low for (1)/(2). Medium for (3) — depends on how much of the original `MeshExporter` ported cleanly to MonoGame's `Model` API. The §3.4 / §3.5 work touched parts of this surface; mining those PRs first will shorten the porting time.

---

## 4.8 — NanoMesh Upstream PR

**Status**: Phase 4 deliverable (PR open) **DONE 2026-05-08** — [RedFox20/NanoMesh#1](https://github.com/RedFox20/NanoMesh/pull/1) opened with a single squashed commit (`8165536`, +687/-55) on `gkapulis/NanoMesh:upstream-pr/fbx-skin-anim`. Submodule pointer holds at `5acc08b` (`blackbox-migration` tip) so the parent repo's tree state matches the PR-pending world.

**Post-merge follow-up tracked in Phase 5** (see [migration-plan-phase5.md §5.2](migration-plan-phase5.md#52--migration-close-optional-post-release)): bumping the `SDNative/NanoMesh` submodule onto upstream `master` after merge, and dropping the local-only-branch language from `project_nanomesh_local_branch.md`. That step is gated on RedFox20's review cadence — not a Phase 4 close blocker. If upstream stalls >30 days or rejects, the Phase 5 follow-up takes the tag-fallback path (pin submodule to `blackbox-migration` head with a `blackboxplus-2026-05-07` tag).

**Goal**: Push the `blackbox-migration` branch's accumulated fixes to NanoMesh upstream as a pull request. Stop the fresh-clone breakage where a new contributor can't build SDNative without first checking out a local-only branch.

**What's on `blackbox-migration` not yet upstream** (per `project_nanomesh_local_branch.md` + recent submodule bumps):
- Cluster bind-matrix recovery in `Mesh_Fbx.cpp::ClusterTransformLinkInverseGL` (Phase 3.10.B.8)
- `Assert`-based hardening of degenerate-cluster path (latest, 2026-05-07)
- `FbxSkin` / `FbxCluster` read+write (`facf6ba`, `3f7bf0f`)
- `FbxAnimStack` / `FbxAnimLayer` / `FbxAnimCurve` read+write (`094427b`, `7f6e045`)
- `TransparencyFactor` invert on write (`42a2338`)
- `LoadMaterial` real implementation + bogus-Assert removal (`379f52c`)

**Steps**:
1. Audit each commit on `blackbox-migration` since the merge base with NanoMesh upstream `master`. Squash trivially-related commits into reviewable units. Rebase on upstream tip if it's moved.
2. For each squashed commit, write a clean commit message that stands alone (no "Phase 3.10.B.8" context — upstream maintainers don't know our project).
3. Open a single PR (or small chain of PRs if the maintainer prefers) against NanoMesh `master`. Title: "FBX skin + animation read/write + bind-matrix recovery". Body: link the FBX SDK 2020 spec sections that motivate each fix.
4. Address reviewer feedback. If upstream stalls >30 days or rejects, pin the submodule to `blackbox-migration` head with a tag (`blackboxplus-2026-05-07` or similar) and document the fork's status in `project_nanomesh_local_branch.md`.
5. After merge: bump the SDNative/NanoMesh submodule on `migration/phase4-x64-monogame` to track upstream master. Drop the local-only branch from `project_nanomesh_local_branch.md`.

**Verification**: PR open (or merged); fresh-clone of BlackBoxPlus + recursive submodule init builds without manual branch-checkout step.

**Rollback**: not applicable — this is a coordination step, not a code change on our side.

**Risk**: Low. The fixes are general-purpose; upstream is likely receptive. The risk is timing (upstream review cadence) — mitigated by the tag fallback.

---

## 4.9 — Steam SDK x64: Park + Scrub x86 Baggage

**Status**: Parked for public alpha (rescoped 2026-05-08). Full Steamworks.NET wiring deferred until the project's distribution scope changes (commercial release, closed beta gated by Steam ownership, or partner-backend access). For public alpha the binary launches standalone from its install folder — same shape as 1.51's default-folder launch — and Steam features are off.

**What this section delivers in Phase 4** (the scrub, not the wiring):
1. Delete the vendored x86 DLLs `game/GARSteamManager.dll` + `game/steam_api.dll`. Both are 32-bit and unloadable in the x64 host process; sitting on disk they're pure noise (and a `BadImageFormat` if anything ever tries to `LoadLibrary` them).
2. Delete `game/steam_appid.txt`. Without `SteamAPI_Init` ever being called the file isn't read by anything — and Steam's docs explicitly mark it dev-only, so it shouldn't ship anyway.
3. Rewrite `Ship_Game/Utils/SteamManager.cs` as a clean stub: keep the 6-method external public surface (`Initialize`, `IsInitialized`, `RequestStats`, `AchievementUnlocked`, `ActivateWebOverlay`, `Shutdown`) so callers don't change; drop the ~25 dead `[DllImport("GARSteamManager")]` declarations and the unused remote-storage / stat-setter helpers; rewrite `ActivateWebOverlay` to use `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })` so URL launches work under .NET 8 (the .NET-Core change in `Process.Start` overload defaults).
4. Drop the `GARSteamManager.dll` + `steam_api.dll` lines from `Deploy/Release/Release.txt` so the legacy MakeInstaller manifest matches reality.

**Verification**:
- Release|x64 builds clean (WaE gate).
- `game/` no longer contains `GARSteamManager.dll`, `steam_api.dll`, or `steam_appid.txt`.
- Game launches; main-menu reachable; the boot-log line "SteamManager disabled..." is gone (replaced by simply not logging anything Steam-related — the class is now a transparent no-op rather than an opt-in disabled feature).
- `Log.cs:819-821` and `StarDriveGame.cs:41-69, 203` (the only external SteamManager call sites) compile and behave as before (all paths gate on `IsInitialized == false`).

**Rollback**: `git revert` the scrub commit. The pre-§4.9 state was "DLLs on disk, ~25 dead `DllImport`s, `Initialize` short-circuits to false" — fully recoverable.

**Risk**: Low. Nothing on the runtime hot path actually used Steam in the public-alpha shape; the scrub removes baggage without changing observable behavior.

**Recipe preserved** (for when wiring is revived): full Steamworks.NET migration recipe lives in `migration-plan-phase2.md` "Deferred Final Step — Steam SDK x64 (Steamworks.NET)" appendix. The 6-method public surface there is the same surface we keep stubbed today, so the future revive is a pure internal rewrite — no caller changes.

**Reopen condition**: revisit when any of these hold:
- BlackBoxPlus moves out of public alpha into commercial / Steam-store distribution.
- Maintainer obtains partner-backend access on AppID 220680 (or forks to a new AppID).
- Closed beta needs Steam-ownership gating (achievements fire on StarDrive-1 owners specifically).

Until then: stub stays stubbed.

---

## Cross-cutting Concerns

### Test infrastructure
Phase 3's test suite (80+ tests in Data + Graphics) is the regression baseline. Don't delete tests — extend them. New tests per sub-phase:
- §4.2: `CombinedArmsExportSweepTests` (representative skinned hulls, smoke-shape).
- §4.9: `SteamManagerInitializationTests` (no-Steam-running path).

### Performance budget
Phase 2 baseline: ~16ms/frame at 1080p MainMenu. §4.1's baseline showed the migration sitting at vsync-locked 60 Hz across MainMenu/Universe/Combat; no user-visible regression surfaced through the §4.6 polish pass. **§4.4's formal Phase-2-vs-post-§4.6 delta measurement is deferred to post-release** — not a Phase 4 close gate. Soft cap (<10% MainMenu, <20% peak combat regression) and the optimization recipe live in §4.4 for that future pass.

### Mod compatibility
Combined Arms is the canary (§4.2). If time permits, run a best-effort sweep of the other 14 mod directories surveyed in `phase3-logs/asset-survey-summary.md` — most reuse vanilla content and should "just work", but a 30-minute session to confirm is cheap insurance.

### Branch hygiene
Each sub-phase commits to `migration/phase4-x64-monogame`. Open one PR per sub-phase against `migration/monogame_migration` (matches Phase 2/3's pattern). Final §4.9 PR closes Phase 4 development; the migration close (sign-off + release) lives in [Phase 5](migration-plan-phase5.md).

### What's NOT in Phase 4
The Phase 3 plan's "Phase 4 placeholder" section listed HDR, advanced lighting models, and AI improvements as out-of-scope-for-this-doc. Same applies here. Specifically:
- ARCHITECTURE.md §8 god-class refactors (Fleet/Empire/ResourceManager) — gameplay debt, not migration debt.
- `Xna31ModelReader` runtime decoder — superseded by the offline FBX pipeline; reach for it only if a mod ships an XNB Model with no `.fbx`/`.obj` sibling.
- Save-game compatibility with pre-migration XNA 3.1 saves.
- HDR tone mapping.

---

## Risk Summary

| Sub-phase | Risk | Mitigation |
|---|---|---|
| 4.1 Baseline | Low | Pure setup + measurement. |
| 4.2 Combined Arms sweep | Medium | §3.10.B.8 added thorough error-path diagnostics; any export regression should fail loudly. Re-using the `Ship17EndToEndTest` shape gives a known-good debug surface. |
| 4.3 Warnings cleanup | Low–Medium | Project warnings are mostly mechanical fixes. The CS0618 SpriteBatch / DrawIndexedPrimitives swap is the largest blast surface — single dedicated commit. |
| 4.4 Perf baseline + opt | Deferred | Deferred to post-release (rescoped 2026-05-08); §4.1 baseline at vsync-cap, no regression observed through §4.6. Future pass uses the §4.4 recipe unchanged. |
| 4.5 Backlog finishes | Medium | §4.5.A (YouLose desaturate) is genuinely diagnostic-heavy; reserve a budget and accept WONTFIX if the four hypotheses don't land. §4.5.B is small. |
| 4.6 Visual polish | Low–Medium | Per-item commits; uniform-gated; visual sign-off per item. |
| 4.7 Toolchain decision | Low | Decision-doc step. Default to (1) status-quo if no near-term need surfaces. |
| 4.8 NanoMesh PR | Low | Cross-team coordination; tag fallback if upstream stalls. |
| 4.9 Steam scrub | Low | No runtime behavior changes (call sites already gate on `IsInitialized == false`); pure source + asset cleanup. Build matrix verifies. |

(Risk rows for §5.1 1.6.0 Release and §5.2 Migration close live in [Phase 5](migration-plan-phase5.md#risk-summary).)

**Phase 4 close**: §4.9 is the last development sub-phase. The dev-phase outcome is captured in commit history + memory; the user-facing sign-off — release artefact, signed installer, GitHub release, PHASE4_RESULTS.md, ARCHITECTURE.md update — happens in [Phase 5](migration-plan-phase5.md). After §5.1 publishes `mars-release-1.6.0`, ARCHITECTURE.md §9's "Suggested Migration Order" gets a "Migration completed" marker (in §5.2 if done, or directly when convenient otherwise), and all migration-related memory entries are settled. Future work falls under "post-migration" — gameplay features, mod support extensions, engine upgrades — and is out of scope for this plan series.
