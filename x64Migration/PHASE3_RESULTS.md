# Phase 3 Results — 3D Content Restoration + Advanced Rendering

**Status**: Phase 3 closed with the renderer at feature parity for the original 2013 art on a 64-bit MonoGame stack. All three Phase 2 carryovers are resolved. Skinned/animated meshes (Ralyeh ship17 family) play their clips. Bloom + screen-space distortion + fog-of-war post-process passes restored. Basic shadow maps. Material maps (normal/specular/emissive) sampled across all hulls. The §3.11 visual polish pass and the (always-deferred) Steam SDK x64 are moved to **Phase 4** — see `migration-plan-phase4.md` and "Carryover to Phase 4" below.

**Branch**: `migration/phase3-x64-monogame` (PR opened against `migration/monogame_migration`)

**Tag**: `phase3-end` (planned at PR merge)

## Sub-phase Completion

| Sub-phase | Outcome | Commits |
|---|---|---|
| 3.1 Asset inventory tool + baseline | Done | `19a323bf7` |
| 3.2 FBX SDK 2018 → 2020.3.7 ABI restoration; asteroid `.fbx` un-stub | Done | `97d680b35` |
| 3.3 Effect XNB-3.1 → MGFX shim; restore 6 broken effects | Done | `ab3c53ecc`, `ec148977d`, `e4c66f6a3`, `db9e1a935`, plus the four `.mgfxo` ships (`scale`/`Thrust`/`BeamFX`/`PlanetHalo`/`BasicFogOfWar`/`desaturate`) |
| 3.3.A Phase 2.3 SpriteFont rebake size regression | Done | `c0879d2c2` (RESOLVED) |
| 3.4 XNB Model decode — static meshes (~210/276) | **Resolved 2026-05-04** via offline FBX export pipeline + Phase B archive of Model XNBs | `e7d151f86`, `72b168f82`, `c73fb51dc`, `523f6b6c6`, `9bd3b7128` (drop), `4183f92ba` (loader accepts .fbx/.obj), `a5da742b4` (Phase B archive 121 dead .xnb) |
| 3.5 Particle / beam / projectile FX restoration | Done | `7262ddf51`, `020e974c5`, `0d543df06`, `553eb8d64`, `673dfe277`, `700bff256`, `875b7bbc3`, `1c7698a56` |
| 3.6 MainMenu Mars 3D sphere; Phase 2 cosmetic carryover cleanup | Done | `a5fc95ad0`, `83b06ba06`, `0b366b27f`, `2efb77fee` |
| 3.7 Renderer feature parity: bloom, distortion, fog-of-war, material maps | Done | `3b33679f2`, `1cf3f148d`, `cd9a5ad72`, `67dc8565c`, `693310d10`, `9ca4010c7`, `a1fe93d82`, `b7fee5419`, `53e73d114`, `ed0754bf3` |
| 3.8 Shadow maps (A + B) | Done | `fccaa1b53`, `1af5278b9` |
| 3.9 FBX TransparencyFactor write fix + legacy mesh re-export | Done | `dd2e1e3f4`, NanoMesh submodule bumps to `42a2338` |
| 3.10.A Skinned mesh extraction (FBX skin + anim writer) | Done | `056377d26`, `b438d288c`, `4376fdc03`, `400c67103` |
| 3.10.B Skinned mesh runtime + animation playback | Done — 9 sub-steps (B.0–B.8) | `aae5a27f1`, `a63198680`, `4b5a804f4`, `d1f07e6f7`, `fab64bbca`, `46defa714`, `3bb2e03b0`, `0689fcf91`, …, `60756263b` |
| 3.10.B.8 Convention-correct bind/keyframe Eulers | Done — defensive fallbacks dropped, throws on bad data | `7d8c4181a`, `bbc1a3bf8`, `2ed94d311`, `1b024acec`, `60756263b`, plus legacy/mesh_exporter_xna31 cherry-picks |
| ~~3.11~~ Visual polish | **Moved to Phase 4 (2026-05-07)** | — |
| 3.12 Phase 3 close (this document, memory updates, runtime smoke) | In progress at the time of this commit | `c5c5159ea` (TODO refresh), `bd413fbda` (Phase 4 carryover list), `0aa7c8df0` (Phase 4 plan draft), `239e4925b` (§4.11 release sub-phase) |

86 commits total on `migration/phase3-x64-monogame` since the merge base. LOC delta: `+15,503 / −3,037` = `+12,466` net. Submodule bumps to `SDNative/NanoMesh` track the local `blackbox-migration` branch (PR pending — Phase 4 §4.8).

## Build Matrix (5 configs × x64)

All clean — 0 errors. Captured at wrap-up; logs under `x64Migration/phase3-logs/wrap/`.

| Configuration | Errors | Warnings |
|---|---|---|
| Debug \| x64                 | 0 | 100 |
| Debug - Auto Fast \| x64     | 0 | 100 |
| Release \| x64               | 0 | 99  |
| Release - Auto Fast \| x64   | 0 | 99  |
| Deploy \| x64                | 0 | 91  |

**Warning headline**: Phase 2 closed at ~4016 warnings per config; Phase 3 closes at ~100. The 4× drop is mostly `CA1416` (Windows-only API) — suppressed repo-wide via `Directory.Build.props` (commit `a03f68139`). Remaining 100-warning baseline is itemized in §4.3 of the Phase 4 plan: 30 C# (CS0618 obsolete API, CS0108 hides-inherited, CS0649 unassigned-readonly, CS8509 non-exhaustive switch, CS8981 lowercase type names, CS8600 nullable, SYSLIB0014 WebClient, CA2014 stackalloc-in-loop) + 70 C++ in SDNative + tests. Phase 4 §4.3 drives this to zero.

## Phase 3 Success Gate — Outcome

| Gate criterion | Result |
|---|---|
| 1. All Phase 2 success-gate criteria still hold | ✅ |
| 2. 3D ship hulls render with materials in Ship Designer / Universe / combat | ✅ via offline FBX export pipeline (§3.4 resolution path B) — all 122 SunBurn-baked + 8 static-raw + 6 Ralyeh skinned XNBs exported and routed through `LoadStaticMesh` |
| 3. Asteroids render via FBX path | ✅ — `NANOMESH_NO_FBX` define dropped (§3.2) |
| 4. Beam weapons fire visually; scale/Thrust/desaturate/PlanetHalo/BasicFogOfWar each render | ✅ — all six restored via `.mgfxo` ports + the XNB-3.1 → MGFX shim work (§3.3, §3.5) |
| 5. Animated meshes play their clips | ✅ — Ralyeh ship17a–f tentacles articulate cleanly. `Ship17EndToEndTest` pins the data flow end-to-end (skin = identity at rest, valid keyframes, no NaN in palette) |
| 6. MainMenu Mars renders as 3D sphere | ✅ — composited overlays + sphere mesh visible (§3.6) |
| 7. Beam/projectile particle effects work end-to-end | ✅ — every named template loads and emits |
| 8. Build matrix green across 5 configs × x64 | ✅ — see table above |
| 9. Visual polish pass (§3.11) | **Moved to Phase 4** — was always low-risk independent items; pulling them out of Phase 3 lets sign-off land on visible-quality wins instead of stretching for cosmetic items |

## What Actually Works at Runtime

- 64-bit MonoGame process boots cleanly on net8.0-windows + MonoGame 3.8.1.303.
- All 24 fonts load (Phase 2.3 SpriteFont rebake regression resolved by §3.3.A).
- MainMenu: Mars 3D sphere composited from base planet texture + 5 overlay panels + sphere mesh; ships and asteroids visible in the background.
- Ship Designer: 3D hulls render with materials, normal maps, specular highlights, emissive contributions. Module overlay layered on top still works.
- Universe: planet bodies render with sun-direction lighting, atmosphere, rings, clouds; fog-of-war post-process composites correctly with the per-system "explored" circles; the per-empire UI overlays remain crisp.
- Combat: beam weapons fire with the BeamFX shader (UV-scroll + alpha), projectiles fly with their FX-mesh trails, missiles/drones/rockets are visible in motion (`ProjSO.Visibility=Rendered` fix, §3.5), shield-hit screen-space distortion pass triggers on impact, particles burst on explosions.
- Skinned ships (Ralyeh ship17a–f) animate their tentacles correctly. Bind pose is authored bind, keyframes are intrinsic-XYZ degrees from a single helper (`QuatToEulerXYZDegrees`), and `BoneAnimationPlayer.ComputeBindWorldInverse` throws with full bone context if any future FBX has corrupt bind data.
- Basic shadow maps cast off the directional sun light onto receivers. Soft cap (4-bone palette per skinned mesh, single cascade) — sufficient for the ship/station/planet roster.
- Post-process chain: bloom → fog-of-war composite → distortion. Wired through `DepthStencilState`/`RenderTarget2D` ping-pong (§3.7 step 3 fixed `RenderTarget2D.GetTexture()` snapshot semantics that broke under MonoGame 3.8.1.303).
- 80+ Data + Graphics unit tests green; including `Ship17EndToEndTest` (whole skinned-mesh load + skin chain), `MeshImporterTests` (OBJ + FBX), `ParticleManagerTests`, `EffectXnbCompatTests`, `BoneAnimationPlayerTests` (8 tests including topological-sort regression).

## Architectural Pivots

Phase 3 made one substantial pivot relative to the original plan:

### XNB Model decode (§3.4) — switched from runtime decoder to offline FBX export

**Original plan**: ContentTypeReader stubs + restore `MeshExporter.Export` to walk loaded XNB Models and emit FBX sidecars at runtime, on first access.

**What shipped**: a complete offline FBX corpus generated by the `legacy/mesh_exporter_xna31` branch (which keeps the XNA 3.1 + XNAnimation + SunBurn 1.3 stack alive specifically for this purpose) and dropped wholesale into `game/Content/Model/`. Migration's `RawContentLoader` then loads `.fbx`/`.obj` directly via NanoMesh — no XNB decode needed at runtime. Resolved 2026-05-04 (commit `9bd3b7128`); Phase B archived 121 unused .xnb to `game/LegacyMesh/` (commit `a5da742b4`).

**Why this won**: bypassing XNB removes both the SunBurn ContentTypeReader resolution problem (no longer needed on the read side) and the XNA 3.1 VertexDeclaration binary-format unknown (the offline exporter has an XNA 3.1 runtime to do the decode). The `Xna31VertexDeclarationReader` written in §3.4 step 5 is preserved for future Phase 4 reach if a mod ships an XNB Model with no `.fbx`/`.obj` sibling. See memory `project_phase2_xnb_model_drift.md`.

**Trade-off**: re-exports require a manual cross-branch toolchain switch (legacy → migration) until §4.7 picks one of the three options preserved in `project_phase4_legacy_mesh_export_sync.md`.

### §3.10 skinned mesh — convention drift hunt

The original §3.10.B succeeded structurally on the first pass, but in-game animation looked wrong (180° root flip + "broken limbs" articulation across the entire ship17 corpus). Root-causing took §3.10.B.8 through six iterations:

1. Defensive skin-matrix path + topological bone traversal — animated bones rendered, but with the wrong rotations.
2. Frame-0-as-bind heuristic — hid the bug for one ship, surfaced for others.
3. Real-bind-pose-from-T/R/S — exposed the convention mismatch fully.
4. SDMeshAddBone Scale/Rotation swap fix — fixed the C++ side.
5. SDMeshAddBoneTRS — added a direct T/R/S entry point so the legacy exporter could route bind poses through the same `QuatToEulerXYZDegrees` helper as keyframes, avoiding `rpp::Matrix4::getRotationAngles` on row-vector XNA bytes.
6. Drop defensive fallbacks; `BoneAnimationPlayer.ComputeBindWorldInverse` now throws `InvalidDataException` on NaN/non-invertible bind matrices with full bone context.

Memory entry `project_phase310_legacy_fbx_bind_pose.md` captures the full diagnosis.

## Carryover to Phase 4

The full Phase 4 plan is in `migration-plan-phase4.md`. Eleven sub-phases ordered:

1. **§4.1 Baseline + perf baseline capture** — re-baseline Phase 2 timings on current hardware so §4.4 has data to compare against.
2. **§4.2 Combined Arms regression sweep** *(first per user direction)* — re-export Combined Arms hulls through legacy/mesh_exporter_xna31 (the §3.10.B.8 fixes apply uniformly across the corpus, but only Ralyeh ship17 a–f was visually verified) and confirm no regressions.
3. **§4.3 Build hygiene: zero warnings on Release|x64** + warnings-as-errors gate so future regressions can't sneak in.
4. **§4.4 Performance baseline + targeted optimization** (ARCHITECTURE.md §9 step 4c).
5. **§4.5 Backlog finishes**: YouLose desaturate visual (deferred 2026-05-03 with diagnostic checklist preserved), light rig data rebake (the catch-and-empty path in `GameScreen.AssignLightRig`).
6. **§4.6 Visual polish pass** — 7 prepared items + an open-ended user-driven UI walkthrough.
7. **§4.7 Mesh-export toolchain decision** — pick one of the three options in `project_phase4_legacy_mesh_export_sync.md`.
8. **§4.8 NanoMesh upstream PR** — push `blackbox-migration` to NanoMesh upstream so fresh-clones don't depend on a local-only branch.
9. **§4.9 Steam SDK x64 via Steamworks.NET** — was always the always-deferred final step.
10. **§4.10 Phase 4 close** — PHASE4_RESULTS.md, ARCHITECTURE.md update, sign-off.
11. **§4.11 Cut 1.6.0 release** — signed installer + ZIP + Steam-folder install path with UAC elevation, capturing the 1.51 release tooling we already have under `Deploy/` plus the new signing + Steam-folder requirements.

### Smaller Phase 3 leftover items folded into Phase 4 sub-phases

| Item | Phase 3 state | Phase 4 home |
|---|---|---|
| YouLose / YouWin desaturate held-state visual | deferred 2026-05-03 with hypotheses preserved | §4.5.A |
| Light rig data rebake | catch-and-empty stub in `GameScreen.AssignLightRig` | §4.5.B |
| Build hygiene (~100 warnings/config) | down 4000× from Phase 2 close, but not yet zero | §4.3 |
| Performance vs Phase 2 baseline | not yet measured on Phase 3 close | §4.4 |
| Mesh-export branch sync (legacy vs migration) | three options preserved in memory | §4.7 |
| NanoMesh local-only branch | submodule pinned to `blackbox-migration` head | §4.8 |
| Steam SDK x64 (always-deferred) | `SteamManager.Initialize()` short-circuits to false | §4.9 |
| 1.6.0 release (signed binaries + Steam-folder install) | not started — 1.51 was the prior release | §4.11 |

## Anti-goals — confirmed still deferred

Per Phase 3 plan + ARCHITECTURE.md §8 / §9: pixel-exact match to 2013 SunBurn deferred-renderer output (forward-renderer-equivalent is the bar); save-game compatibility with pre-migration XNA 3.1 saves; network/multiplayer (none planned); HDR tone mapping; god-class refactors of `Fleet.cs` / `Empire.cs` / `ResourceManager.cs` (gameplay debt, not migration debt); `Xna31ModelReader` runtime decoder (offline FBX pipeline supersedes it).

## Migration retrospective (Phase 1 + 2 + 3)

| Metric | Value |
|---|---|
| Phase 3 commits | 86 |
| Phase 3 LOC delta | +15,503 / −3,037 = +12,466 net |
| Phase 3 sub-phases | 12 (§3.1, 3.2, 3.3, 3.3.A, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 3.12) — 3.11 punted, 3.10 split into A + B |
| Build matrix outcome | 5/5 green at Phase 3 close (was 5/5 green at Phase 2 close too — never broke) |
| Tests added in Phase 3 | `Ship17EndToEndTest`, `BoneAnimationPlayerTests` (8), `MeshImporterTests` (FBX paths), `EffectXnbCompatTests` extensions, `Xna31VertexDeclarationReader` coverage |

**What went well**:
- The §3.4 pivot from runtime XNB decoder to offline FBX export saved an unbounded research-grade workstream (the XNA 3.1 VertexDeclaration binary format is genuinely undocumented). Trading it for a one-time legacy-branch export run was a much cheaper solution.
- §3.5 audit-then-fix pattern — checking each of the 6 effect XNBs against the runtime path one by one, with sub-step commits, kept regressions traceable.
- §3.10 splitting into A (extraction) + B (runtime + animation) decoupled the hard part (data) from the well-trodden part (skinning math), and B.8's six-iteration root-cause hunt produced a clean architectural fix at the source rather than a layered fallback.
- Memory entries written *during* the work (not after) caught the convention-drift evidence that made the §3.10.B.8 root-causing tractable.

**What would have been done differently**:
- The §3.10.B "auto-play first frame" surfaced bind/keyframe convention drift at the absolute worst time — between getting "ships visible" and "animation looks right". Earlier plumbing of a runtime diagnostic dump (BindPose vs frame-0 dump for each skinned bone) would have caught the drift in step B.0/B.1 instead of B.8.
- The §3.7 step 3 fog-of-war ping-pong RT issue (commit `cd9a5ad72`) and the §3.8 single-float uniform packing trap (memory `project_phase38_mgfxc_uniform_packing.md`) both came down to MonoGame 3.8.1.303 quirks vs XNA 3.1. A pre-Phase-3 audit of the MonoGame migration guide would have headed off ~3 commits each.
- The convention-drift iterations on §3.10.B.8 used six commits in two days; the root cause (`rpp::Matrix4::getRotationAngles` operating on transposed bytes) would have been findable in one commit if I'd dumped both bind and frame-0 Eulers as the very first diagnostic.

## Runtime Smoke (2026-05-07)

**Result**: clean. Captured at `x64Migration/phase3-logs/phase3-runtime-smoke.log` (560 lines, 37 KB).

| Metric | Value |
|---|---|
| Session length | 09:08:16 → 09:11:27 (~3m 11s wall, MainMenu → Universe → combat → exit) |
| Errors / exceptions | 0 |
| Engine warnings | 0 |
| Content-data warnings | 6 (all mod content quirks: 1 newer ship-design override, 3 building UID/filename mismatches, 2 mistyped Blueprint XML names) — pre-existing, unrelated to migration |
| StaticMesh loads | 126 (incl. ship17 a–i family) |
| Skinned-mesh load + animation playback | OK (Ralyeh ship17 family articulated correctly during combat) |
| GameVersion string | `Mars : 1.51.15100` (Phase 4 §4.11.A bumps to 1.6.0.\<build\>) |
| Steam state | disabled cleanly (`SteamManager disabled (x64 wrapper not yet available)`) — expected, Phase 4 §4.9 |

User signed off after the walkthrough.

## Phase 3 Sign-off

- Build matrix green across 5 configs × x64 (logs in `phase3-logs/wrap/`).
- Runtime smoke clean (table above).
- Three Phase 2 carryovers fully resolved (XNB ship/hull Models, FBX asteroids, 6 broken Effect XNBs).
- Skinned/animated meshes play correctly in-game, verified end-to-end on Ralyeh ship17 a–f.
- Renderer feature parity reached for the original 2013 art surface (bloom, distortion, fog-of-war post-process, normal/specular/emissive material maps, basic shadow maps).
- Eight Phase 4 carryovers documented in `migration-plan-phase4.md` with concrete resolution paths and dedicated memory entries where applicable.

**Tag (planned at PR merge)**: `phase3-end`
