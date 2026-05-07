# Phase 4 Migration Plan — Polish, Mod Compatibility, Steam

## Context

[Phase 3](migration-plan-phase3.md) closed with the renderer at feature parity for the original 2013 art: 122 SunBurn-baked + 8 static-raw + 6 Ralyeh skinned XNBs all render through the offline FBX pipeline; particle/beam/projectile FX restored; bloom + screen-space distortion + fog-of-war post-process passes back; basic shadow maps; skinned-mesh animation playing on the Ralyeh ship17 family. The runtime is functionally complete — what's left is polish, mod compatibility verification, and the items that were always tagged as Phase 4 from the start (Steam SDK x64).

**Phase 3 carryovers** (from the Phase 3 plan's "Confirmed Phase 4 carryovers" section, in scheduling order):

| Carryover | Phase 3 status | Phase 4 sub-phase |
|---|---|---|
| **Combined Arms mod regression sweep** | §3.10.B.8 fixes apply uniformly across the corpus, but only Ralyeh ship17 a-f was visually verified | §4.2 |
| **Build hygiene: zero warnings on x64** | Baseline at Phase 3 close: 30 C# + 39 C++ warnings on Release\|x64 | §4.3 |
| **Performance vs Phase 2 baseline** | Soft cap from Phase 3 plan: <10% MainMenu, <20% peak combat. Not yet measured on Phase 3 close | §4.4 |
| **YouLose desaturate visual** | Held-state visual still doesn't match pre-migration despite multiple attempts | §4.5 |
| **Light rig data rebake** | Stub catch in `GameScreen.AssignLightRig`; LightRig has no data, SunBurn type-readers gone | §4.5 |
| **Visual polish pass** (was §3.11) | 7 items: MainMenu polish residue, projectile dynamic glow light, glow-map emissive, muzzle FX check, sun Z, specular intensity, fog-of-war map circle dimness | §4.6 |
| **Mesh-export toolchain decision** | Two export-side fixes live on `legacy/mesh_exporter_xna31` only (`f964b6df7`, `5c3a218be`); decide whether to cherry-pick, resurrect on migration, or keep legacy as the dedicated re-export branch | §4.7 |
| **NanoMesh upstream PR** | Local `blackbox-migration` branch carries 7 commits not yet pushed upstream — fresh clone breakage today | §4.8 |
| **Steam SDK x64 via Steamworks.NET** | `SteamManager.Initialize()` short-circuits to false; achievements/stats/cloud-saves inactive | §4.9 |

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
4. **Performance within budget**: <10% frame-time regression vs the Phase 2 baseline at MainMenu, <20% at peak combat. Both measured under identical scene loads on the same hardware.
5. **Visual polish pass** lands the seven items from the (former) §3.11 list. Each item shipped with a pre/post screenshot pair.
6. **NanoMesh upstream** has a merged PR (or, if upstream rejects, a documented decision to keep the fork on a fixed tag).
7. **Steam SDK x64** wired via Steamworks.NET. Achievements / stats / cloud saves work end-to-end on a real Steam build.
8. **PHASE4_RESULTS.md** committed; ARCHITECTURE.md updated to reflect post-migration state; memory entries marked RESOLVED with commit refs.

**Anti-goals for Phase 4** (out of scope; revisit only if explicitly raised):
- Pixel-exact match to 2013 SunBurn deferred-renderer output. Forward-renderer-equivalent remains the bar.
- Save-game compatibility with pre-migration XNA 3.1 saves (separate workstream if ever needed).
- Network / multiplayer (none planned per ARCHITECTURE.md §5.6).
- HDR tone mapping.
- God-class refactor of `Fleet.cs` / `Empire.cs` / `ResourceManager.cs` (ARCHITECTURE.md §8 — gameplay debt, not migration debt).
- `Xna31ModelReader` runtime decoder (ARCHITECTURE.md §9 alternative path C). The offline FBX pipeline supersedes it; reach for this only if a mod ships an XNB Model that has neither an `.fbx` nor `.obj` sibling.
- Sound / music engine changes (already working).

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
| **Steam SDK approach** | Steamworks.NET (decision preserved from Phase 2.6, 2026-05-03). Drop vendored `GARSteamManager.dll` + `steam_api.dll` (both x86, no source). | 6-method external surface; Steamworks.NET is the maintained x64 wrapper. No realistic alternative now that GARSteamManager is unbuildable. |
| **Sign-off shape** | PHASE4_RESULTS.md mirrors PHASE1/2/3_RESULTS.md. Final ARCHITECTURE.md update marks the migration roadmap §9 items DONE. | Pattern consistency. ARCHITECTURE.md is the artifact future maintainers read first. |

---

## Sub-phase Index

| # | Title | Risk |
|---|---|---|
| 4.1 | Baseline checkpoint, Phase 4 branch, runtime + perf baseline | Low |
| 4.2 | Combined Arms regression sweep (mod compat — first) | Medium |
| 4.3 | Build hygiene: zero warnings on `Release\|x64` | Low–Medium |
| 4.4 | Performance baseline + targeted optimization | Medium |
| 4.5 | Backlog finishes: YouLose desaturate, light rig data rebake | Medium |
| 4.6 | Visual polish pass (was §3.11) — 7 items | Low–Medium |
| 4.7 | Mesh-export toolchain decision (legacy-only vs port vs resurrect) | Low |
| 4.8 | NanoMesh upstream PR | Low |
| 4.9 | Steam SDK x64 via Steamworks.NET | Medium |
| 4.10 | Phase 4 close: PHASE4_RESULTS.md, ARCHITECTURE.md update, sign-off | Low |

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

**Tests added**:
- `UnitTests/Data/CombinedArmsExportSweepTests.cs` *(if Combined Arms ships are stable enough to pin)* — for one or two representative skinned hulls, assert `LoadStaticMesh → CreateSceneObject` succeeds, `IsSkinned`, `SkinningPalette` is NaN-free at rest. Mirrors `Ship17EndToEndTest` shape.

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

---

## 4.4 — Performance Baseline + Targeted Optimization

**Goal**: Validate that Phase 3's renderer additions (skinning, shadow map, post-process chain, material maps) fit within the Phase 2 perf budget. If a scene exceeds the soft cap (<10% MainMenu, <20% peak combat regression), apply targeted optimization. Maps to ARCHITECTURE.md §9 step 4c ("Performance profiling and optimization").

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

### §4.5.B — Light rig data rebake

**Status going in**: `GameScreen.AssignLightRig` catches the load failure and assigns an empty `LightRig`. Light rig XNBs are baked against SunBurn type-readers that are gone post-1.9; `LightRig` itself is a stub with no data, so even on success there's nothing to extract.

**Steps**:
1. Audit which scenes/screens currently call `AssignLightRig` and what they expect from a "real" rig.
2. If the answer is "nothing visible" (the catch-and-empty path is already correct), drop the catch and the empty-rig fallback; replace with explicit no-op + comment.
3. If real rig data is needed, rebake the rig content as plain YAML / JSON (per the original TODO) and load it via a regular `Content.Load<T>` path that doesn't depend on the SunBurn pipeline.

**Verification**: if (2), the catch goes away and the runtime behavior is unchanged. If (3), affected scenes show the intended lighting.

**Rollback**: per-step `git revert`.

**Risk**: Medium. §4.5.A is genuinely diagnostic-heavy — reserve a budget and accept WONTFIX if the four hypotheses don't land. §4.5.B is small but depends on §4.5.A's outcome (both touch lighting/visual passes).

---

## 4.6 — Visual Polish Pass (was §3.11)

**Goal**: Land the seven small finishes that surfaced during §3.3–§3.10 implementation but didn't fit any single earlier sub-phase. Each item is independently scoped, individually revertible, and gated only by user visual sign-off — no automated test gate beyond "build matrix green between commits".

**Items** (commit one per item; land in any order):

1. **MainMenu polish residue** — anything still off after §3.6's Mars sphere work (background composition, button layout, version-string placement, animation timing). Specifics captured at entry; before/after screenshots into `phase4-logs/visual-diff/`.
2. **Projectile dynamic glow light** — projectiles cast a per-projectile point/dynamic light onto nearby ships and stations. Integrate with the forward renderer's existing point-light path (`LightingEffect`). Verify against pre-migration footage that beam/projectile travel illuminates flanking hulls visibly. No new shader if the `LightingEffect` point-light path covers it; otherwise extend.
3. **Glow-map light points** *(optional / investigate)* — promote glow-map texture channels from a flat additive overlay to an actual emissive light contribution (per-pixel emissive in the lit pass, optionally seeding small dynamic point lights at high-intensity glow-map pixels for nearby-surface bounce). Only land if it improves perceived quality vs the simpler additive-overlay baseline.
4. **Muzzle effects check** — verify muzzle-flash particle emitters fire on weapon discharge end-to-end. Cross-check against §3.5 particle FX restoration; this is a regression check, not new work. If broken, root-cause and fix in this commit.
5. **Sun Z / depth ordering** — sun position relative to skybox and nearby planets reads wrong. Likely a depth-buffer-disabled-at-skybox issue, a sun-quad render-order tweak, or a near-plane / w-component issue with the sun's billboard. Diagnose, then fix.
6. **Specular intensity** — current specular reads too weak. Diagnose first (could be: specular-map sample magnitude, sRGB vs linear sample of the spec map, global specular multiplier in `MeshLighting.fx`, or eye-vector / normal interpolation precision). Compare against pre-migration footage. Land minimally: prefer a uniform multiplier over shader rewrites if the diagnosis points to a magnitude issue.
7. **Fog-of-war map circle too dim** — the per-system "explored" fog circle on the Universe map reads dimmer than pre-migration. Likely candidates: `BasicFogOfWar` shader's per-pixel attenuation curve, the fog-mask RT format (R8 vs R16F precision floor), the alpha-blend mode used to composite the fog-circle pass, or a missing premultiply on the fog texture sample. Diagnose against pre-migration screenshots first; bias the fix toward a uniform multiplier if the cause is just magnitude.

**Steps**:
1. Capture pre-state visual reference (screenshot/video) per item before touching code.
2. Implement, screenshot, commit. One item per commit; commit message references the item number above.
3. After all items land, run a 5-minute MainMenu→Universe→Combat smoke to confirm no cross-item regression.

**Tests added**: None automated. Visual-diff captures into `phase4-logs/visual-diff/` per item.

**Verification**: User visual sign-off per item against pre-migration reference footage. Build matrix green between commits.

**Rollback**: Per-item `git revert <sha>`. Items are independent.

**Risk**: Low–Medium. Each item is bounded; no system-wide changes. Specular magnitude and projectile dynamic light could touch the lighting effect's parameter surface — keep changes additive and uniform-gated like §3.8's `ShadowParams` packing so a regression flips one number rather than restructuring the effect.

---

## 4.7 — Mesh-Export Toolchain Decision

**Goal**: Pick one of the three options preserved in `project_phase4_legacy_mesh_export_sync.md` and execute it. Stop the situation where re-exports require a manual cross-branch toolchain switch.

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

## 4.9 — Steam SDK x64 via Steamworks.NET

**Goal**: Replace the vendored x86 `GARSteamManager.dll` + `steam_api.dll` with the maintained Steamworks.NET wrapper. Restore achievements, stats, cloud saves on x64.

**Recipe** (preserved in `migration-plan-phase2.md` "Deferred Final Step — Steam SDK x64 (Steamworks.NET)" appendix). Summary:
- Public surface is tiny: 6 SteamManager methods are referenced outside the class — `Initialize`, `IsInitialized`, `RequestStats`, `AchievementUnlocked`, `ActivateWebOverlay`, `Shutdown`.
- AppID `220680` is already in `game/steam_appid.txt`.
- Drop `GARSteamManager.dll` + `steam_api.dll` (both x86, no source).
- Add Steamworks.NET NuGet package; `steam_api64.dll` ships with it.

**Steps**:
1. Add Steamworks.NET to `StarDrive.csproj` (NuGet). Confirm `steam_api64.dll` lands in `game/`.
2. Rewrite `Ship_Game/Utils/SteamManager.cs` — keep the 6-method public surface unchanged; rewrite implementations to call `SteamAPI.*` / `SteamUserStats.*` / `SteamFriends.*`. Remove the `[DllImport("GARSteamManager")]` declarations + the x86 fallback comment block.
3. Delete the vendored `game/GARSteamManager.dll` and `game/steam_api.dll`. Update `.gitignore` if needed.
4. Update the §SteamManager TODO comment to reflect post-migration state (or remove if it's gone).
5. End-to-end smoke on a real Steam build:
   - Launch through Steam client (with `steam_appid.txt` for dev).
   - Verify `IsInitialized` flips to true.
   - Trigger an achievement (`AchievementUnlocked`); confirm in Steam overlay.
   - Open the Steam web overlay (`ActivateWebOverlay`); confirm the overlay opens to the right URL.
   - Stats request roundtrip (`RequestStats`).
6. Verify behavior when Steam is NOT running: `Initialize` returns false cleanly; the rest of the methods no-op via `IsInitialized` gating.

**Tests added**:
- `UnitTests/Utils/SteamManagerInitializationTests.cs` — `Initialize_WithoutSteamRunning_ReturnsFalse_AndPubMethodsNoOp`. (Real Steam interaction can't be unit-tested; gate in the smoke run.)

**Verification**:
- All 6 public-surface methods work end-to-end against a real Steam client.
- `IsInitialized` correctly reflects Steam availability.
- No P/Invoke fires when `IsInitialized == false`.
- Build matrix green.

**Rollback**: revert the SteamManager rewrite + restore vendored DLLs from git. The pre-Phase-4 state was "stub returning false" — fully recoverable.

**Risk**: Medium. Steamworks.NET integration is well-trodden territory but the smoke-on-real-Steam-build step requires an actual Steam launch (external dependency). Mitigation: implement and unit-test in an offline branch first; do the Steam smoke at the end so failure doesn't block other Phase 4 work.

---

## 4.10 — Phase 4 Close: PHASE4_RESULTS.md, ARCHITECTURE.md update, Sign-off

**Goal**: Sign off Phase 4 and the overall migration. Produce the results document. Update ARCHITECTURE.md to reflect the post-migration state. Run the final runtime smoke. Update memory to reflect resolved status.

**Steps**:
1. **Runtime smoke**: launch `game/StarDrive.exe`. Walk MainMenu → New Game → Universe → engage in combat → reach mid-game → save → reload → exit. Capture `phase4-runtime-smoke.log`. Repeat with Combined Arms loaded.
2. **Build matrix**: 5 configs × x64. Capture all 5 logs under `phase4-logs/wrap/`. Confirm 0 warnings, 0 errors on Release|x64 (warnings-as-errors gate from §4.3).
3. Author **`PHASE4_RESULTS.md`** in `x64Migration/`. Sections (mirroring PHASE3_RESULTS.md):
   - Sub-phase completion table with commit refs.
   - Build matrix outcomes.
   - Success-gate verification (each item from "Phase 4 Goals" above, ✅ / ❌).
   - Combined Arms + vanilla regression summary.
   - Performance summary table (vs Phase 2 baseline).
   - Migration retrospective: total commits across Phase 1+2+3+4, total LOC delta, what went well / what would have been done differently across the entire migration.
4. **ARCHITECTURE.md update**:
   - §8 "32-Bit Assumptions" — strike through (now resolved).
   - §9 "Migration Roadmap" — mark all sub-phases (1–4) DONE with commit refs.
   - §9 "Suggested Migration Order" — replace with a "Migration completed (2026-XX-XX)" marker pointing at PHASE4_RESULTS.md.
   - Update §6 "Native C++ Integration (SDNative)" if NanoMesh upstream PR landed (§4.8).
   - Update §7 "Third-Party Libraries" with Steamworks.NET (§4.9), FBX SDK 2020 (Phase 3 outcome).
5. **Memory file updates**:
   - `project_phase4_legacy_mesh_export_sync.md` → mark RESOLVED with §4.7 decision + commit refs.
   - `project_nanomesh_local_branch.md` → mark RESOLVED with PR link or fork-tag note.
   - `project_phase3_3_youlose_desaturate_unresolved.md` → mark RESOLVED or WONTFIX with §4.5.A outcome.
   - `MEMORY.md` → update one-line hooks for the three files above. Audit for any other entries that became stale during Phase 4.
   - Author new `project_phase4_zero_warnings_gate.md` capturing the warning-suppression patterns chosen (vendored vs first-party) so future contributors don't blanket-disable warnings.
6. **Open Phase 4 PR** and tag `phase4-end`. The Phase 4 PR closes the migration.

**Verification**:
- All Phase 4 success-gate items verified.
- PHASE4_RESULTS.md committed; ARCHITECTURE.md updated; memory files updated.
- Build matrix green; runtime smoke clean for both vanilla and Combined Arms.
- `phase4-end` tag exists; PR open or merged.

**Rollback**: N/A (sign-off step). If a regression is found post-merge, revert specific sub-phase commits — each is independently revertible by design.

**Risk**: Low. Sign-off + documentation.

---

## Cross-cutting Concerns

### Test infrastructure
Phase 3's test suite (80+ tests in Data + Graphics) is the regression baseline. Don't delete tests — extend them. New tests per sub-phase:
- §4.2: `CombinedArmsExportSweepTests` (representative skinned hulls, smoke-shape).
- §4.9: `SteamManagerInitializationTests` (no-Steam-running path).

### Performance budget
Phase 2 baseline: ~16ms/frame at 1080p MainMenu. Phase 3's renderer additions need to fit; §4.4 measures the actual delta. Soft cap: <10% MainMenu, <20% peak combat. If §4.4's measurement shows a regression outside budget, optimization work in §4.4 lands; otherwise re-baseline and document.

### Mod compatibility
Combined Arms is the canary (§4.2). If time permits, run a best-effort sweep of the other 14 mod directories surveyed in `phase3-logs/asset-survey-summary.md` — most reuse vanilla content and should "just work", but a 30-minute session to confirm is cheap insurance.

### Branch hygiene
Each sub-phase commits to `migration/phase4-x64-monogame`. Open one PR per sub-phase against `migration/monogame_migration` (matches Phase 2/3's pattern). Final §4.10 PR closes Phase 4 and the migration as a whole.

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
| 4.4 Perf baseline + opt | Medium | Hard to predict bottleneck without measurement. §4.1 captures baseline first so §4.4 has data, not vibes. |
| 4.5 Backlog finishes | Medium | §4.5.A (YouLose desaturate) is genuinely diagnostic-heavy; reserve a budget and accept WONTFIX if the four hypotheses don't land. §4.5.B is small. |
| 4.6 Visual polish | Low–Medium | Per-item commits; uniform-gated; visual sign-off per item. |
| 4.7 Toolchain decision | Low | Decision-doc step. Default to (1) status-quo if no near-term need surfaces. |
| 4.8 NanoMesh PR | Low | Cross-team coordination; tag fallback if upstream stalls. |
| 4.9 Steam SDK | Medium | External dependency on real Steam build for smoke. Implement + unit-test offline first; do Steam smoke at the end so failure doesn't block other Phase 4 work. |
| 4.10 Sign-off | Low | Documentation only. |

**Migration close**: §4.10 closes Phase 4 and the four-phase migration as a whole. After §4.10 merges, ARCHITECTURE.md §9's "Suggested Migration Order" gets a "Migration completed" marker, and all migration-related memory entries are settled. Future work falls under "post-migration" — gameplay features, mod support extensions, engine upgrades — and is out of scope for this plan series.
