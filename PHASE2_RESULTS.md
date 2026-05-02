# Phase 2 Results — Content Pipeline + Rendering Core

**Status**: Phase 2 closed with a navigable 64-bit MonoGame StarDrive. MainMenu → Universe → Combat all reachable. Three carry-over items (XNB ship/hull Models, FBX asteroid import, six broken Effect XNBs) explicitly **deferred to Phase 3** — see "Carryover to Phase 3" below.

**Branch**: `migration/phase2-x64-monogame` (PR opened against `migration/monogame_migration` by user)

**Tag**: `phase2-complete` (planned at PR merge)

## Sub-phase Completion

| Sub-phase | Outcome | Commits |
|-----------|---------|---------|
| 2.1 Baseline checkpoint, branch, integrate Phase 1 | Done | tag `phase2-start` → `49f1feba9` |
| 2.2 MGFX shader pipeline + XNB compat shims (8 steps) | Done — broken Effect XNBs (6) stubbed, **rewrite deferred to Phase 3** | `0178b76d1`, `e65217ed1`, `b8bdb8310`, `f4e972ff0`, `893731888`, `e43cb60e4`, `85acd4f9e`, `10b35d779` |
| 2.3 SpriteFont rebake via MGCB + texture pipeline cleanup | Done | `5fae0d679` |
| 2.4 Restore 2D UI; Simple.fx through MGFX | Done | `8e18d4325` |
| 2.5 VideoPlayer probe; revert ScreenMediaPlayer try/catch | Done | `10799a4d9` |
| 2.6 Steam SDK x64 swap | **DEFERRED** — graceful-disabled state retained as the final step of the overall migration | — |
| 2.6.A net48 → net8.0-windows; MonoGame 3.8.0.1641 → 3.8.1.303 | Done | `9bf638cb2` |
| 2.7 Scope A: SunBurnStubs upgraded from no-ops to data carriers | Done | `17cb88c1f` |
| 2.7.B PNG R/B channel swap cleanup + alpha premul | Done | `91aefe8ec` |
| 2.8 Pre-hardening (MGFX pipeline + render-loop NRE guards) | Done | `b2b431537` |
| 2.8 A+B Forward renderer scaffolding + RenderManager loop | Done | `310b79ef0` |
| 2.8 B4 Legacy StaticMesh.Draw overloads dispatch to forward path | Done | `bb3a76ea1` |
| 2.8 Follow-up: restore Debug console + fix test discovery | Done | `b165fe948` |
| 2.8.C Restore raw-mesh runtime path (OBJ); planets render | Done — XNB ship/hull half **deferred to Phase 3** | `c8752a311`, `0ad114035`, `224a95cfe`, `655b17641`, `1873d3458` |
| 2.9.A Particle audit + ParticleManager regression test | Done — in-battle ship/projectile mesh visuals XNB-gated, deferred | `a6402f716` |
| 2.10 FBX mesh re-enable in x64 + cleanup + sign-off | **Cleanup done; FBX SDK 2018→2020 ABI fix DEFERRED to Phase 3** | this doc |

## Build Matrix (5 configs × x64)

All clean — 0 errors. Captured at wrap-up; logs under `C:/Users/gkapu/.claude/plans/phase2-logs/wrap/`.

| Configuration | Time | Errors | Warnings |
|---------------|------|--------|----------|
| Debug \| x64               | 10.16s | 0 | 4016 |
| Release \| x64             | 9.26s  | 0 | 4016 |
| Debug - Auto Fast \| x64   | 8.24s  | 0 | 4017 |
| Release - Auto Fast \| x64 | 9.15s  | 0 | 4016 |
| Deploy \| x64              | 6.52s  | 0 | 4008 |

Warning bulk: `Color.TransparentBlack` deprecation hints across `Ship_Game/UI/`, plus `C4267 size_t→int` in `SDNative/SDNativeTests/`. Both pre-existing, both cosmetic, both punted to Phase 3 cleanup.

## Phase 2 Success Gate — Outcome

| Gate criterion | Result |
|---|---|
| Phase 1 success criteria still hold (x64 process, window opens, blackbox.log, clean exit) | ✅ |
| Boot reaches the main menu; splash plays or skips | ✅ Splash plays end-to-end (since 2.6.A) |
| Text renders (Arial, Pirulen, etc. via SpriteBatch + Simple.mgfx) | ✅ All 24 fonts load |
| 2D UI works (buttons, panels, scroll lists, hover/click) | ✅ |
| Navigate to at least one secondary screen without crash | ✅ Ship Designer, Race Designer, Universe, Combat all reached |
| 3D viewport renders the hull mesh | ⚠️ **Partial** — planet bodies (OBJ raw-mesh path) render with sun lighting and atmosphere. Ship hulls (276 XNB Models) still stubbed; ships move/fight via the 2D module-overlay tab — see Carryover §1 |
| Phase 1 tolerance patches removed | ✅ All four reverted (Shader.FromFile, SpriteRenderer null tolerance, ScreenManager env skip, ScreenMediaPlayer try/catch) |
| Build matrix still green across 5 configs × x64 | ✅ See table above |

Five of seven gates fully met; one partial (3D viewport — planets yes, ship hulls no); Steam (originally §2.6) explicitly deferred to migration final step.

## What Actually Works at Runtime

- 64-bit MonoGame process boots cleanly on net8.0-windows + MonoGame 3.8.1.303
- Splash + loading videos play (Media Foundation backend; IsLooped silently dropped pending MonoGame upgrade past 3.8.1.303)
- MainMenu renders with all UI text and atlas-fed buttons; mouse hover/click work
- Ship Designer: 2D module overlay works; 3D hull renders only for assets that come through the OBJ raw-mesh path (none of the in-game ships do — they're all XNB)
- Universe screen: planet bodies render with correct sun-direction lighting, clouds, atmosphere, rings; sun visible; UI overlays correct
- Combat screen: explosions visible; weapon-fire particles emit; projectiles fly (mesh stubbed → invisible flight, but state propagates correctly)
- Engine trail particles emit and follow ships (ParticleManager loads all 27 named templates — guarded by `ParticleManagerTests.Reload_PopulatesAllNamedTemplates`)
- 519 unit tests discoverable via dotnet test / VS Test Explorer (post 2.8 follow-up MSTest bump to 3.11.1)

## Carryover to Phase 3

These three items were touched, scoped, and explicitly deferred — not left undiscovered. Each has a dedicated memory entry with full execution detail.

### 1. XNB Model format drift — 276 ship/hull/projectile/station/effect meshes

**State**: stubbed at `GameContentManager.LoadStaticMesh` — broken loads return a minimum-viable `StaticMesh(name, unitBounds)` so callers don't NRE; `StaticMesh.Draw` no-ops for these meshes. `MeshImporter` (raw-mesh OBJ/FBX path) is fully un-stubbed and produces real geometry — the gap is XNB-only.

**Why deferred**: two layered problems, neither solvable in remaining Phase 2 budget. (a) XNB Model files reference SunBurn `LightingMaterialReader_Pro` / `SceneEnvironmentReader_Pro` ContentTypeReaders that no longer resolve (SDSunBurn purged in Phase 1.9). (b) XNA 3.1 VertexDeclaration XNB binary format is undocumented — empirical hex dump of `Effects/ThrustCylinderB.xnb` shows 28 bytes for 3 elements (≈9.33 bytes/element), matching no published layout. No public XNA 3.1 source, no MonoGame community gist, no PR ever shipped a working compat reader.

**Workable in current state**: Ships move and fight via the 2D module-overlay tab; combat resolves; particles + explosions render. The visual gap is purely "no ship hull silhouette in 3D viewport."

**Resolution paths in Phase 3** (memory `project_phase2_xnb_model_drift.md` has hex dumps + decode hints):
- (A) Reverse-engineer the 3.1 VertexDeclaration binary format empirically
- (B) Bypass XNB by writing SunBurn ContentTypeReader stubs that translate to NanoMesh `.obj`/`.fbx` source loads (requires source meshes — possibly not in repo for original 2013 content)
- (C) Rebake all Model XNBs through MonoGame Content Builder (requires `.fbx` sources)

### 2. FBX mesh import disabled in x64 — 9 asteroid meshes

**State**: `NANOMESH_NO_FBX=1` preprocessor define in `SDNative.vcxproj` for `Debug|x64` and `Release|x64`. `Mesh::LoadFBX` returns false; `MeshImporter.ImportStaticMesh` skips `.fbx` paths.

**Why deferred**: FBX SDK 2018 → 2020 changed the `FbxArray` template signature from `template <class T>` to `template <class T, const int Alignment = 16>`. The `Alignment` param sizes a `char Padding[Alignment]` member of `FbxArray::tData` and participates in allocation arithmetic — surgical patch is unsafe because vendored 2018 headers and 2020 binaries would have different memory layouts. Full header replacement to FBX SDK 2020.3.7 is the safe fix; medium-effort and isolated to `SDNative/NanoMesh/`.

**Resolution path in Phase 3** (memory `project_phase2_backlog_fbx.md`):
- Replace `SDNative/3rdparty/fbxsdk/fbxsdk/` headers with FBX SDK 2020.3.7 from `C:\Program Files\Autodesk\FBX\FBX SDK\2020.3.7\include\`
- Audit downstream API drift in `Mesh_Fbx.cpp`
- Drop the `NANOMESH_NO_FBX=1` define
- Add an FBX-specific test analogous to `MeshImporterTests.ImportStaticMesh_PlanetSphereObj_HasNonZeroGeometry`

### 3. Six broken Effect XNBs — rewrite as MGFX

**State**: `GameContentManager.LoadAsset<T>` returns null for the 6 names listed below (with one-time warning per name). Callers null-guard and skip rendering. Stub set is `Phase2BrokenEffectXnbs` in `GameContentManager.cs`. Pinned by `EffectXnbCompatTests.StubbedEffectXnbs_ReturnNullWithoutThrowing` — the test fails if any of these starts loading successfully, forcing list maintenance.

**The six**: `Effects/BeamFX`, `Effects/scale`, `Effects/Thrust`, `Effects/desaturate`, `Effects/BasicFogOfWar`, `Effects/PlanetHalo`.

**Visual cost in current state**: beam weapons render without their custom FX (still functional but flat); some thruster/scale/halo flourishes absent. None block gameplay; all noted as cosmetic by user.

**Why deferred**: only `Simple.fx` and `Clouds.fx` exist as `.fx` source in `game/Content/Effects/`. The other six need either (a) the XNB-3.1 → MGFX shim (write `ContentTypeReader<Effect>` that reads XNA 3.1's Effect XNB layout — D3D9 bytecode + parameter metadata — and re-emits MGFX in-memory; covers all six in one fix), or (b) `fxc /dumpbin` disassembly per file (lossy), or (c) manual rewrite from gameplay observation (last resort).

**Resolution path in Phase 3** (memory `project_phase2_effect_xnb_drift.md` priorities #1-#3).

### 4. Steam SDK x64 swap — full Steamworks.NET migration

**State**: `SteamManager.Initialize()` short-circuited to `IsInitialized = false`; every public method gates on the flag. Achievements, stats, cloud-saves inactive but graceful. AppID `220680` already in `game/steam_appid.txt`.

**Why deferred**: explicit decision (2026-05-03) to push to the absolute final step of the overall migration. Single-player game, Steam features are nice-to-have not load-bearing, and the work is mechanical-but-noisy (NuGet add, x86 DLL removal, 11-method API rewrite, RunCallbacks loop integration). Doing it now would risk regressing the clean §2.5/§2.6.A boot path with no boot-path payoff.

**Resolution path** (kept intact in `migration-plan-phase2.md` "Deferred Final Step" appendix; not moved to Phase 3 plan since it sits after Phase 3 too).

### 5. Cosmetic / minor

- **VideoPlayer.IsLooped** silently dropped in MonoGame 3.8.1.303 → Loading 2 video plays once. Lifts on next MonoGame upgrade.
- **`Color.TransparentBlack` → `Color.Transparent` sweep** (~40 call sites in `Ship_Game/UI/`). Trivial regex replace; left as Phase 3 cleanup-pass starter.
- **Skinned animation** (XNAnimation/SgMotion equivalent). No in-game use case found; deferred indefinitely per Phase 2 plan.

## Anti-goals — confirmed still deferred (per original plan)

Shadow maps; deferred rendering pipeline; bloom + post-processing; HDR / tone mapping; full universe-screen polish (atmospheric scattering shader, starfield depth, parallax stars); full combat-screen polish (beam shaders, tractor effects, shockwaves); save/load round-trip with newly-baked content; multiplayer (none planned).

## Phase 2 Sign-off

- Build matrix green across 5 configs × x64.
- All four Phase 1 tolerance patches reverted.
- Game boots to MainMenu, navigates Universe + Combat for ≥5 minutes interactively without crash (verified during 2.8.C hotfix #6 sun-lighting validation).
- Three known visual gaps (XNB ship hulls, FBX asteroids, 6 broken Effect XNBs) documented above with concrete resolution paths and dedicated memory entries.
- 519 unit tests discoverable; new tests added: `MeshImporterTests` (2), `ParticleManagerTests` (1), `EffectXnbCompatTests` (1 stub-contract pin), `Xna31Texture2DReader` coverage paths, `LightManagerTests`, `SceneInterfaceTests`, plus the §2.4 SpriteRenderer/TextureAtlas suite.
- No new regressions in Phase 1's foundation. SDSunBurn / XNAnimation / XNA 3.1 / SunBurn binding redirects remain fully purged.

**Tag (planned at merge)**: `phase2-complete`
