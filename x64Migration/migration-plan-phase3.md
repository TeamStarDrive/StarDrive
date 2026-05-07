# Phase 3 Migration Plan — 3D Content Restoration + Advanced Rendering

## Context

[Phase 2](migration-plan-phase2.md) closed 2026-05-02 with the runtime booting end-to-end on net8 + MonoGame 3.8.1.303 (MainMenu → Universe → Combat reachable for ≥5 minutes interactively). The Phase 2 success gate was met, but **three deferred items** were handed forward — the rendering side of the migration is functional but visually thin without them. See [PHASE2_RESULTS.md](PHASE2_RESULTS.md) §"Carryover to Phase 3":

| Carryover | Phase 2 status | Phase 3 sub-phase |
|---|---|---|
| **276 XNB Model files** (ship/hull/projectile/station/effect meshes — some skinned/animated) | Stubbed at `GameContentManager.LoadStaticMesh`; returns minimum-viable `StaticMesh(name, unitBounds)` | §3.4 (static) + §3.10 (skinned/animated) |
| **9 asteroid `.fbx` meshes** | `NANOMESH_NO_FBX=1` retained in x64; `Mesh::LoadFBX` returns `false` | §3.2 |
| **6 broken Effect XNBs** (BeamFX, scale, Thrust, desaturate, BasicFogOfWar, PlanetHalo) | `Phase2BrokenEffectXnbs` set in `GameContentManager`; one-time warning, callers null-guard | §3.3 |
| **Steam SDK x64** (parked at the very end of the migration) | `SteamManager.Initialize()` short-circuits to `false`; achievements/stats inactive | **Deferred to Phase 4** (2026-05-07 re-prioritization) |
| **Cosmetic** (MainMenu Mars sphere; VideoPlayer.IsLooped) | Mars renders as a flat strip; Loading 2 video plays once instead of looping | §3.6 / §3.12 polish pass |

**The user's framing**: this is the **most important phase**. "Issues are expected with XNB Models. Some of them also contain animations." Skinned-model XNBs embed bone hierarchies + animation clip data that depended on the now-purged `XNAnimation` library at runtime, plus SunBurn `LightingMaterialReader_Pro` for material data. The 8 static-raw XNBs (`ThrustCylinderB`, `Window`, `muzzleEnergy`, `projBall/Long/Tear`, `Kulrathi/ship12b/c`) additionally need an XNA 3.1 VertexDeclaration binary decoder.

**§3.1 inventory close-out (2026-05-02 — see [phase3-logs/asset-survey-summary.md](phase3-logs/asset-survey-summary.md))**:
- **122 confirmed static-sunburn Models** (+ ~12 likely-static in the tool's unreadable set, runtime-confirmed).
- **8 confirmed static-raw Models** (the 3.1 VertexDeclaration drift cluster).
- **1 confirmed skinned-sgmotion Model** (`Model/Ships/Ralyeh/ship17a.xnb`) + 5 likely-skinned siblings (`ship17b-f.xnb`). **Realistic §3.10 scope: 6 XNBs** (Ralyeh ship17 family), not the original "30-60" estimate.
- The asset inventory tool at `x64Migration/Tools/Phase3AssetInventory/` is one-shot and feeds sub-phase scoping; the §3.4 extraction pipeline uses MonoGame's runtime `ContentManager` (proven path), not this tool.

**2026-05-02 architectural unlock — developer note from the original mesh-export author**: the previous developer had finished the C++ side of mesh export (NanoMesh FBX writer + complete bone APIs in `SDNative/SdMesh/SdAnimation.h`: `SDMeshAddBone`, `SDMeshAddSkinnedBone`, `SDMeshCreateAnimationClip`, `SDMeshAddBoneAnimation`, `SDMeshAddAnimationKeyFrame`). The mesh-conversion work was never finished only because of skeletal-mesh import on the C# side — bones + skin weights weren't being walked from the runtime-loaded XNB into the SDNative DLL. The intended architecture: **load XNB at runtime via `ContentManager` → walk bones/weights/clips in C# → call SDNative bone APIs → emit FBX**. This consolidates §3.4 + §3.10 into one extraction pipeline reusing the existing C++ infrastructure rather than building a parallel system. The strategy below reflects this.

**Related memory** (read these before starting any sub-phase):
- [project_phase2_xnb_model_drift.md](c:/Users/gkapu/.claude/projects/c--Development-stardrive-BlackBoxPlus/memory/project_phase2_xnb_model_drift.md) — empirical hex dump + 3 resolution paths
- [project_phase2_effect_xnb_drift.md](c:/Users/gkapu/.claude/projects/c--Development-stardrive-BlackBoxPlus/memory/project_phase2_effect_xnb_drift.md) — XNB-3.1 → MGFX shim approach
- [project_phase2_backlog_fbx.md](c:/Users/gkapu/.claude/projects/c--Development-stardrive-BlackBoxPlus/memory/project_phase2_backlog_fbx.md) — `FbxArray` ABI break recipe
- [project_phase2_backlog_runtime.md](c:/Users/gkapu/.claude/projects/c--Development-stardrive-BlackBoxPlus/memory/project_phase2_backlog_runtime.md) — backlog index, Steam deferral

## Phase 3 Goals (Success Gate)

The user runs `game/StarDrive.exe`. The process:

1. All Phase 2 success-gate criteria still hold (boot, MainMenu, navigation, build matrix green).
2. **3D ship hulls render with materials** in Ship Designer, Universe combat, and Empire screens — not stub bounding boxes. Original 2013 art is visible.
3. **Asteroids render** in asteroid fields (FBX path enabled).
4. **Beam weapons fire visually** (BeamFX restored). Scale/Thrust/desaturate/PlanetHalo/BasicFogOfWar each render with their original effect.
5. **Animated meshes play their clips** (turrets traverse, doors open, planet bodies rotate via animation track if any) — OR the animated subset is explicitly classified and the runtime gracefully renders the bind-pose for the remainder, with a list documented for future work.
6. **MainMenu Mars renders as a 3D sphere** (composited overlays + sphere mesh visible — currently flat strip).
7. **Beam/projectile particle effects work** end-to-end (now that BeamFX/Thrust XNBs load).
8. **Build matrix still green** across 5 configs × x64. No regressions.
9. **Visual polish pass** (§3.11) lands a curated set of small finishes: projectile dynamic light, glow-map emissive contribution, muzzle effect verification, sun Z/depth ordering, specular intensity, fog-of-war map circle dimness, and any residual MainMenu polish. Steam SDK (was §3.11) is deferred to Phase 4.

**Anti-goals for Phase 3** (deferred to a Phase 4 or treated as won't-fix):
- Pixel-exact match to 2013 SunBurn deferred-renderer output. Forward-renderer-equivalent is the bar.
- Save-game compatibility with pre-migration XNA 3.1 saves (separate workstream if needed).
- Network / multiplayer (none planned per ARCHITECTURE.md §5.6).
- HDR tone mapping. Bloom is in §3.7.
- Sound / music engine changes (already working in Phase 2).

## Confirmed Strategic Decisions

| Decision | Choice | Rationale |
|---|---|---|
| **XNB Model decode strategy** | **Runtime ContentTypeReader stubs + restore `MeshExporter.Export`**. Use MonoGame's existing `ContentManager` (proven LZX + ModelReader path) to load each XNB once. Stub readers consume SunBurn / SgMotion / 3.1-VD bytes so manifest resolution succeeds; the runtime then constructs a real `Microsoft.Xna.Framework.Graphics.Model`. `MeshExporter.Export(Model)` (currently a Phase 1 stub) walks the loaded Model and feeds NanoMesh via the existing SDNative bone DLLAPIs (`SDMeshAddBone`, `SDMeshAddVertex`, etc.) → NanoMesh emits `.fbx` sidecar → `RawContentLoader` reloads it on next access. | Reuses the proven runtime LZX/Model path and the original developer's already-finished C++ exporter. Avoids the standalone XNB decoder + research-grade VertexDeclaration parser the original plan called for. The previous mesh-conversion attempt got blocked specifically on the C#-side skeletal extraction; that's now narrowly scoped. Sidecars are commit-gated. |
| **XNB Model — material data** | **SunBurn ContentTypeReader stubs** that read the `LightingMaterialReader_Pro` payload and convert to a portable material struct (`MaterialDef { diffuse, normal, specular, emissive, shininess }`). Stubs live in `Ship_Game/Data/Mesh/SunBurnReaderStubs.cs`. | The 130+ static XNBs all reference SunBurn type readers that XNA's `ContentReader.GetTypeReader` can't resolve (`SDSunBurn` was purged in Phase 1.9). Stubs only need to *consume* the bytes correctly so the manifest resolves — `MeshExporter` reads material data from the consumed structs and re-emits as FBX `FbxSurfacePhong`. |
| **Skinned mesh extraction** | **SgMotion ContentTypeReader stubs + extend `MeshExporter` with skinned path**. Stubs (`SkinnedModelReader`, `SkinnedModelBoneReader`, `AnimationClipReader`, `TimeSpanReader`) consume the SgMotion-baked bones + skin weights + animation clips and surface them as portable C# structs. `MeshExporter` calls `SDMeshAddSkinnedBone` + `SDMeshCreateAnimationClip` + `SDMeshAddBoneAnimation` + `SDMeshAddAnimationKeyFrame` — APIs that already exist in `SdAnimation.h`. NanoMesh's FBX writer serializes the skin cluster + anim stack. | One pipeline handles static and skinned (skinned just adds the bone DLLAPI calls). The C++ side is already finished per the developer note; this is the C# bridge that was missing. Realistic scope is 6 XNBs (Ralyeh ship17 family — only confirmed-skinned set in the inventory). |
| **Skinned mesh runtime rendering** | **Re-import-via-FBX path is the default**. Once §3.10 emits a skinned `.fbx` sidecar, the existing `RawContentLoader` + NanoMesh FBX import handles it at runtime. The skin shader + `BoneAnimationPlayer` are only required if we want clip playback in-engine — for §3.10's success-gate (mesh visible with bones), bind-pose-only rendering via the FBX import is acceptable. Custom skin shader / `BoneAnimationPlayer` is §3.10.B (optional follow-up) if clip playback is needed. | Decouples extraction (the hard part the previous developer was stuck on) from runtime animation playback (well-trodden territory we can land in a follow-up step). Lets §3.10 ship with visible Ralyeh hulls + bone hierarchies even if clip playback isn't wired. |
| **3.1 VertexDeclaration binary decode** | **Custom `Xna31VertexDeclarationReader`** registered alongside the existing `Xna31Texture2D/3DReader`. Decodes the 3.1 binary format empirically from the 8 known static-raw XNBs and produces a MonoGame `VertexDeclaration`. | Narrow scope (8 XNBs, all known) makes empirical decode tractable. The 28-bytes-for-3-elements hex from `ThrustCylinderB.xnb` is preserved in `project_phase2_xnb_model_drift.md`; with 8 different samples we can build a reliable hypothesis. Failure fallback: hand-construct the equivalent `VertexDeclaration` in C# for each unique pattern (lookup table). |
| **Animation classification** | **§3.1 inventory CSV** (`phase3-logs/asset-survey.csv`) drives scheduling. | The 276-asset surface was hand-audited via the inventory tool; results show much smaller skinned scope than originally feared. |
| **Effect XNB-3.1 → MGFX shim** | **Custom `ContentTypeReader<Effect>` registered via `ContentTypeReaderManager.AddTypeCreator`** (same pattern as the §2.2 `Xna31Texture2DReader`). Reads XNA 3.1's `Effect` XNB layout (header + D3D9 shader bytecode + param metadata) and synthesizes an MGFX byte stream in-memory before constructing the MonoGame `Effect`. | One fix covers all 6 broken effects + any Effect XNB the codebase doesn't currently route. Avoids needing source `.fx` recovery (`BeamFX.fx`, `scale.fx` etc. don't exist on disk). Disassembly via `fxc /dumpbin` is the fallback per `project_phase2_effect_xnb_drift.md`. |
| **FBX SDK** | **Full header swap to FBX SDK 2020.3.7** (already installed on dev machine). No surgical 1-arg → 2-arg `FbxArray` patch. | Per `project_phase2_backlog_fbx.md`: `FbxArray` template ABI changed AND `FbxArray::tData` layout has different padding. Surgical patches risk silent memory layout drift. Full swap + per-call-site fixup is the safe path. |
| **Steam SDK** | **Deferred to Phase 4** (2026-05-07). Recipe preserved: full Steamworks.NET migration (Option A from `project_phase2_backlog_runtime.md`). Replace `GARSteamManager.dll` (no source available) with the maintained Steamworks.NET wrapper. | Public surface is tiny — only 6 SteamManager methods are referenced outside the class (`Initialize`, `IsInitialized`, `RequestStats`, `AchievementUnlocked`, `ActivateWebOverlay`, `Shutdown`). AppID 220680 already in `game/steam_appid.txt`. Reprioritization: Phase 3 should close on visible-quality wins, not Steam plumbing. |
| **Order: low-risk first** | FBX (mechanical) → Effect shim (medium R&D) → XNB Models static (high) → XNB Models skinned (very high) → renderer features → Steam | Builds momentum and proves the test harness; pushes the open-ended R&D to the middle of the phase where bailout / replan is cheapest. |
| **Sidecar storage** | Generated `.fbx`/`.obj` sidecars committed under `game/Content/Model/.../meshname.fbx` next to original `.xnb`. Mod-routing precedence in `GameContentManager.AssetName` already prefers `.fbx`/`.obj` over `.xnb` when both exist. | Free win — the routing is already there from Phase 2's `RawContentLoader` work. No code change needed for fallback. |

## Sub-phase Index

| # | Title | Risk |
|---|---|---|
| 3.1 | Baseline checkpoint, Phase 3 branch, asset inventory CSV | Low |
| 3.2 | FBX SDK 2018 → 2020 ABI restoration; un-stub asteroid `.fbx` path | Medium |
| 3.3 | Effect XNB-3.1 → MGFX shim; restore 6 broken effects | Medium–High |
| 3.3.A | Fix Phase 2.3 SpriteFont rebake size regression (RESOLVED 2026-05-03, commit c0879d2c2) | Medium |
| 3.4 | ~~XNB Model decode — static meshes (~210 of 276)~~ **RESOLVED 2026-05-04 via offline FBX export pipeline (commit 9bd3b7128); Phase B archived all Model XNBs** | **High** |
| 3.5 | Particle / beam / projectile FX restoration end-to-end (incl. Phase C texture XNB→DDS migration) | Medium |
| 3.6 | MainMenu Mars 3D sphere; Phase 2 cosmetic carryover cleanup | Low–Medium |
| 3.7 | Renderer feature parity: bloom, distortion, fog-of-war post-process passes, material maps | Medium |
| 3.8 | Shadow maps (basic) | Medium–High |
| 3.9 | FBX TransparencyFactor write fix + legacy mesh re-export (Combined Arms ships) | Medium |
| 3.10 | XNB Model decode — skinned/animated meshes + animation runtime | **Very High** |
| ~~3.11~~ | ~~Small finishes: visual polish~~ **Moved to Phase 4 (2026-05-07)** — see `migration-plan-phase4.md` | — |
| 3.12 | Phase 3 close: PHASE3_RESULTS.md, runtime smoke, final memory cleanup | Low |

Each sub-phase ends with a commit and is rollback-able via `git revert <sha>` or `git reset --hard <tag>`.

---

## 3.1 — Baseline Checkpoint, Phase 3 Branch, Asset Inventory

**Goal**: Tagged starting point for Phase 3 with Phase 2 fully merged. Produce a complete inventory of the 276 XNB Models classified as static / skinned / animated, plus a runtime baseline log against which Phase 3 progress is measured.

**Steps**:
1. Confirm `migration/phase2-x64-monogame` has been merged to `migration/monogame_migration` (PR #242 must be green and merged before §3.2 starts). If not yet merged, wait for the user.
2. From the integration branch, branch `migration/phase3-x64-monogame` (already exists locally per `git branch`; confirm it tracks `migration/monogame_migration` head).
3. `git tag phase3-start`.
4. Build all 5 configs × x64. Confirm 0 errors and warning counts match Phase 2 close-out (4016 / 4016 / 4017 / 4016 / 4008).
5. Launch `game/StarDrive.exe`. Capture `blackbox.log` showing the current set of "Phase 2 stub" warnings: `LoadStaticMesh ... XNB Model load failed`, `Phase 2 broken effect XNB: <name>`. Save as `phase3-baseline.log` in `phase3-logs/`.
6. **Asset inventory tool** (`x64Migration/Tools/Phase3AssetInventory/`):
   - Walk `game/Content/Model/**/*.xnb`. For each file: open as raw binary, decode the XNB header (compression flag, size), LZX-decompress (use the `Xna31Compat.DumpXnbTypeReaders` helper from Phase 2.2 — already reflects on MonoGame's internal `LzxDecoder`), parse the type-reader manifest.
   - Classify by reader chain:
     - Contains only `ModelReader` + `VertexDeclarationReader` + `VertexBufferReader` + `IndexBufferReader` + `BasicEffectReader` (or `LightingMaterialReader_Pro`) → **static**.
     - Additionally contains `SkinningDataReader` / `AnimationClipReader` / `BoneReader` (whatever XNA 3.1 + SgMotion baked) → **skinned**.
   - For skinned meshes, extract bone count + clip count + clip names if reachable in the manifest.
   - Output `phase3-logs/asset-survey.csv`: `path, kind (static|skinned|unknown), bone_count, clip_count, clip_names, vertex_decl_bytes_hex`.
   - Output `phase3-logs/asset-survey-summary.md`: counts by kind × directory; flag any directory where ≥1 asset is skinned (those folders need the §3.10 runtime to be visible at all).
7. Cross-reference the survey with code: `grep -rn "LoadStaticMesh\|LoadModel\(" Ship_Game --include="*.cs"` — for each call site, mark whether the asset is critical-path (Universe combat, Ship Designer, splash) or peripheral (mod menu thumbnails). The CSV will drive §3.4 vs §3.10 priority.
8. Inventory `// TODO Phase 3:` markers: `grep -rn "TODO Phase 3" Ship_Game SDGraphics SDUtils SDNative` — surface every Phase 2 deferred site.

**Verification**:
- Build matrix green; baseline log captured; CSV produced and committed under `phase3-logs/`.
- CSV + summary doc allow §3.4 / §3.10 scope to be costed before either sub-phase starts.

**Rollback**: `git checkout migration/monogame_migration && git branch -D migration/phase3-x64-monogame`.

**Risk**: Low. Pure setup + read-only inventory. The asset inventory tool can be one-shot (not committed as production code) — its output drives planning; the tool itself can live under `Tools/` and rot.

---

## 3.2 — FBX SDK 2018 → 2020 ABI Restoration; Un-stub Asteroid `.fbx` Path

**Goal**: Replace the FBX SDK 2018 vendored headers with FBX SDK 2020.3.7. Drop `NANOMESH_NO_FBX=1` from x64 build configs. Verify `Mesh::LoadFBX` actually loads `game/Content/Model/Asteroids/asteroid1.fbx` end-to-end and the asteroid renders in-game.

**Why this is first**: smallest-blast-radius of the three Phase 2 carryovers. Mechanical work, isolated to `SDNative/NanoMesh/`. Builds momentum and re-warms the SDNative build pipeline before §3.4 starts changing C# mesh-loading code.

**Steps**:
1. Replace the entire `SDNative/3rdparty/fbxsdk/fbxsdk/` header tree with FBX SDK 2020.3.7 headers from `C:\Program Files\Autodesk\FBX\FBX SDK\2020.3.7\include\`. Don't try a partial merge — the `FbxArray<T>` template signature change cascades into `FbxArray::tData` layout (per `project_phase2_backlog_fbx.md`).
2. Verify `SDNative/3rdparty/fbxsdk/x64/libfbxsdk.lib` is the matching 2020.3.7 binary (check PE timestamp + `dumpbin /headers` for the `FbxArray` mangled symbol — the `$0BA@` template arg should be present).
3. Drop the `NANOMESH_NO_FBX=1` define from `Debug|x64` and `Release|x64` `<PreprocessorDefinitions>` in `SDNative/SDNative.vcxproj`. Rebuild SDNative for x64.
4. **Audit the compile errors**. Beyond `FbxArray`, expect: removed/renamed methods (`KFbxLayer*` → `FbxLayer*` was 2014, but check for 2018→2020 deltas), changed default args (`FbxImporter::Initialize` may have new optional param), and enum renames. Fix per call-site rather than wrapping in compatibility macros.
5. Verify `Mesh::LoadFBX` end-to-end:
   - Add unit test `UnitTests/Data/MeshImporterTests.ImportStaticMesh_AsteroidFbx_HasNonZeroGeometry` mirroring the OBJ test from §2.8.C.
   - Assert vertex count > 0, index count > 0, materials.Count > 0 (asteroids have a single albedo).
6. In-game smoke: load a save with an asteroid field; visually confirm asteroids render with texture (not stub bounds).

**Tests added**:
- `UnitTests/Data/MeshImporterTests.cs` — `ImportStaticMesh_AsteroidFbx_*` (one per asteroid `.fbx`; 9 assertions). Catches FBX loader regressions in the 2020.3.7 SDK.

**Verification**:
- SDNative builds clean for `Debug|x64` and `Release|x64`.
- All 9 asteroid `.fbx` files load via `MeshImporter.ImportStaticMesh` and produce non-empty geometry.
- In-game asteroid field renders meshes (not bounding-box stubs).
- Build matrix still green across 5 configs × x64.

**Rollback**: `git revert HEAD`. The 2018 headers + `NANOMESH_NO_FBX=1` return.

**Risk**: Medium. The header swap is mechanical but the API delta surface between 2018 and 2020 isn't fully audited until compile fails. Mitigation: budget two days for fixup; have the surgical-patch fallback documented (in `project_phase2_backlog_fbx.md`) if the full swap explodes — though that fallback is unsafe long-term.

---

## 3.3 — Effect XNB-3.1 → MGFX Shim; Restore 6 Broken Effects

**Goal**: Write a custom `ContentTypeReader<Effect>` that reads the XNA 3.1 Effect XNB binary layout and synthesizes an MGFX byte stream in-memory, then hands it to MonoGame's `Effect(GraphicsDevice, byte[])` constructor. This single shim restores all 6 broken effects (BeamFX, scale, Thrust, desaturate, BasicFogOfWar, PlanetHalo) plus any Effect XNB the codebase doesn't currently route. Remove `Phase2BrokenEffectXnbs` set from `GameContentManager`.

**Why this is second**: bounded R&D — the XNA 3.1 Effect XNB format is documented (XNA Game Studio open-source binaries had it) and `BeamFX.xnb`'s bytes are sitting on disk for empirical decode. Lower risk than the VertexDeclaration unknown of §3.4. ALL 6 broken effects fall to one fix.

**Steps**:
1. **Inventory the wire format**: open `game/Content/Effects/BeamFX.xnb`, LZX-decompress, dump the type-reader manifest (use `Xna31Compat.DumpXnbTypeReaders`). Confirm the reader is `Microsoft.Xna.Framework.Content.EffectReader` (XNA 3.1 base type) — not a SunBurn reader.
2. **Decode the XNA 3.1 `Effect` payload**:
   - 4 bytes: shader bytecode length (Int32).
   - N bytes: D3D9 shader bytecode blob (compiled `ps_2_0` / `vs_2_0` — what `fxc` produced in 2010).
   - Followed by parameter metadata: param count, then per-param (name, semantic, type, register, default value).
   - Cross-reference with the public XNA 3.1 source at github.com/XNAGameStudio (the SilverSpring / FNA / Spritebatch reference implementations — many archived forks document this).
3. **Write `Ship_Game/Data/Xna31EffectReader.cs`** (mirrors `Xna31Texture2DReader` pattern):
   - `class Xna31EffectReader : ContentTypeReader<Effect>`
   - In `Read(ContentReader, Effect)`:
     - Read XNA 3.1 fields.
     - Synthesize an MGFX byte stream using the MonoGame MGFX format: header `MGFX` + version byte + profile byte (D3D9 = 0, D3D11 = 1) + technique array + parameter array + shader array.
     - For shader bytecode: try direct re-use first (D3D9 bytecode runs on `ps_2_0`-class hardware via `_level_9_x` on D3D11; if D3D11 path rejects it, disassemble via `D3DDisassemble` (DX11 SDK) → recompile with `D3DCompile` to `ps_4_0_level_9_x`).
     - Construct `Effect(graphicsDevice, mgfxBytes)`.
   - Register via `ContentTypeReaderManager.AddTypeCreator("Microsoft.Xna.Framework.Content.EffectReader", () => new Xna31EffectReader())`.
4. Remove `Phase2BrokenEffectXnbs` set from [Ship_Game/Data/GameContentManager.cs](Ship_Game/Data/GameContentManager.cs). The null-check guards at call sites can stay (defense-in-depth) but the `LoadAsset<T>` short-circuit is gone.
5. Update `EffectXnbCompatTests`:
   - Rename `StubbedEffectXnbs_ReturnNullWithoutThrowing` → `Phase2BrokenEffectXnbs_NowLoadSuccessfully`.
   - Iterate the 6 effect names; assert `Load<Effect>` returns non-null with `CurrentTechnique != null` and `Passes.Count > 0`.
   - Pin parameter expectations per effect (e.g. BeamFX should have `WorldViewProj`, `Color`, `Texture` — confirm against game-side parameter binding sites).
6. **In-game verification per effect**:
   - **BeamFX**: fire a beam weapon in combat; confirm beam renders with color/texture, not invisible.
   - **scale / Thrust**: spawn a ship with thrusters; confirm thruster trail visible.
   - **desaturate**: trigger desaturation post-process (likely on game-pause overlay or screen transition).
   - **BasicFogOfWar**: enter Universe view with unexplored systems; confirm fog overlay renders.
   - **PlanetHalo**: orbit a planet at close range; confirm halo glow visible at limb.

**Tests added**:
- `UnitTests/Content/EffectXnbCompatTests.cs` — rewritten per step 5.
- `UnitTests/Content/Xna31EffectReaderTests.cs` — synthetic small `.xnb` (constructed in test fixture from a known minimal effect compiled via mgfxc) round-trips through `Xna31EffectReader`. Catches reader regressions without requiring all 6 game effects to compile.

**Verification**:
- All 6 effects load via `content.Load<Effect>` and report non-null `CurrentTechnique`.
- Each effect produces visible output in its respective in-game scenario per step 6.
- `Phase2BrokenEffectXnbs` set is gone from `GameContentManager`.
- Build matrix green; no boot-log warnings about broken effect XNBs.

**Rollback**: `git revert HEAD`. The shim file disappears; `Phase2BrokenEffectXnbs` set returns; effects null again.

**Risk**: Medium–High. Variable risk centers on whether D3D9 bytecode survives the `_level_9_x` D3D11 path or needs a disassemble-recompile detour. Mitigation: budget the two-step decode (try direct → fall back to D3DDisassemble) up front; document any effect that needs the slower path. If a specific effect's HLSL turns out to use a 3.0-only feature, hand-rewrite that one effect (Strategy 3 from `project_phase2_effect_xnb_drift.md`) — narrower scope than wholesale rewrite.

---

## 3.3.A — Fix the Phase 2.3 SpriteFont rebake size regression

**Status: RESOLVED 2026-05-03 in commit `c0879d2c2`.** Option A (restore pre-migration .xnb fonts) was chosen, with the squares-as-text Dxt3 risk addressed by adding software BC2 decompression in `Ship_Game/Data/Xna31Texture2DReader.cs` (`Xna31Compat.DecompressDxt3ToRgba8888`). Decoded RGBA8888 is uploaded as `SurfaceFormat.Color`, bypassing MonoGame WindowsDX 3.8's broken GPU BC2 alpha sampling for the XNA 3.1 font atlas layout. Regression pinned by `UnitTests/Content/SpriteFontXnbCompatTests`. The historical context below is preserved in case a similar XNA 3.1 → MonoGame texture-format issue arises elsewhere.

**Priority**: HIGH — visibly annoying across every screen with text. User-flagged 2026-05-03 as the next-session priority. Pre-existing since Phase 2.3 (`5fae0d679`); not introduced by §3.3 effect work.

**Goal**: in-game text renders at the same visible pixel size as pre-migration. UI layouts authored against XNA Content Pipeline glyph metrics stop overflowing / getting cropped / overlapping. No regression on the squares-as-text Dxt3 sampling issue Phase 2.3 fixed.

**Root cause** (already diagnosed; full detail in `memory/project_phase2_3_font_rebake_size.md`): commit `5fae0d679` rebaked all 24 SpriteFont XNBs via MGCB after Phase 2.2 made the original Dxt3-baked XNBs render as white squares. MGCB's font rasterizer (FreeType / SharpFont) emits visibly larger glyphs than XNA Content Pipeline's GDI+ rasterizer at the same `<Size>` value — empirically ~20–30% larger across all sizes. The rebake fixed the squares issue but silently broke the visual baseline.

**Three resolution paths**, in order of preference:

1. **Option A — restore the XNA-baked XNBs from git history, fix the squares issue another way**
   - `git show 5fae0d679^:game/Content/Fonts/<Font>.xnb > game/Content/Fonts/<Font>.xnb` for all 24 fonts. Or `git checkout 5fae0d679^ -- game/Content/Fonts/`.
   - Pre-migration appearance restored exactly.
   - Then re-attack the Phase 2.2 step 8 squares-as-text issue: it was Dxt3 BC2 alpha sampling under MonoGame WindowsDX 3.8. Phase 3.3 has shipped texture-loading shims (`Xna31Texture2DReader` with R/B swap + premul-heuristic for `SurfaceFormat.Color`) but NOT for Dxt3. Probe whether the squares issue can be fixed at the SpriteBatch sampler-state level (e.g., force `SamplerState.PointClamp` for SpriteFont rendering), or by adjusting how SpriteFont's glyph atlas SamplerState is set in MonoGame. If neither works, fall back to (B).
   - Effort estimate: 1-2 hours if sampler-state fix works; 1 day if Dxt3 needs a deeper sampler audit.

2. **Option B — re-rebake .spritefont with shrunk `<Size>`**
   - For each of 24 .spritefont files, reduce `<Size>` by ~20% empirically (14 → 11, 12 → 9, 16 → 12, 20 → 16, etc.). Iterate per-font until visible size matches pre-migration.
   - Run the rebake via `mgcb -build` using the `Directory.Build.props` configuration set up in `5fae0d679`.
   - Keeps MGCB pipeline + `TextureFormat=Color` + Cyrillic glyph regions from Phase 2.3.
   - Effort estimate: 0.5-1 day for full calibration sweep + visual verification.

3. **Option C — hybrid** (mention only as fallback; probably overkill).

**Steps for the recommended Option A path**:

1. Restore originals: `git checkout 5fae0d679^ -- game/Content/Fonts/`. Build, run game, confirm fonts visible at pre-migration size and confirm whether the squares issue is back.
2. If squares are back: probe the SpriteBatch sampler state at SpriteFont draw time. Add a temporary `Log.Info($"SpriteFont sampler: {device.SamplerStates[0]}")` right before a `batch.DrawString` call. Confirm whether it's `LinearWrap` / `PointClamp` / something else.
3. Try forcing `SamplerState.PointClamp` for SpriteFont passes. SpriteBatch's `Begin(SpriteSortMode, BlendState, SamplerState, ...)` overload accepts it; the codebase's `SafeBegin` wrapper may need a new overload that propagates the sampler choice.
4. Verify Dxt3 alpha sampling works correctly with PointClamp. If still squares, fall back to Option B.
5. **Tests**: add `UnitTests/Content/SpriteFontSizeRegressionTests.cs` that loads `Arial14Bold.spritefont`'s baked XNB and asserts `LineSpacing` is in the pre-migration range (need to capture the value from a `git show 5fae0d679^:game/Content/Fonts/Arial14Bold.xnb`-extracted reading first). Pin the regression so it can't silently re-occur.

**Verification**:
- Side-by-side screenshot comparison of main menu, options screen, and universe-screen labels. Pre-migration release-build vs post-fix debug build. Both at Windows DPI scale = 100% on the same machine. No DPI-aware divergence.
- Game shipping in 5+ languages — Cyrillic rendering must still work (the Phase 2.3 rebake added Cyrillic glyph regions because `Fonts.cs:72-94` substitutes runtime files for slavic locales). If Option A is taken, the original XNA-baked XNBs need to also have Cyrillic regions, OR the runtime substitution path needs validation.

**Rollback**:
- Option A rollback: `git checkout HEAD -- game/Content/Fonts/` to bring MGCB-baked back. Squares issue returns but layout is "right" until fix.
- Option B rollback: revert the .spritefont edits + MGCB rebake commit; Phase 2.3 baseline restored.

**Risk**: Medium. Option A's risk is that the squares issue isn't sampler-state-fixable and we have to fall through to Option B anyway, costing 0.5 day. Option B's risk is per-font calibration is empirical and may not produce pixel-perfect parity for every size. Both options preserve the squares-fix outcome by construction.

**Why this jumped the queue**: text size is in every UI screen; the Mars 1.51-era code has hand-tuned UI layouts that depend on the XNA-baked metrics. Continuing further migration work without fixing this means every new layout decision risks getting baked against the wrong baseline.

---

## 3.4 — XNB Model Decode — Static Meshes (~134 of 276) — **RESOLVED 2026-05-04**

**Resolution summary**: the original strategy (runtime XNB decode via reader stubs + `MeshExporter.Export` FBX emit) was **superseded mid-flight** by an offline export pipeline. On `legacy/mesh_exporter_xna31` (XNA 3.1 + .NET 4.8, can read original XNB Models), `MeshExporter` was re-derived and run over the entire static corpus, producing FBX+DDS sidecars. Output committed to migration as `9bd3b7128 content: legacy mesh re-export drop (FBX + DDS + PNG, 147 MB)`. Phase B (commits `6f68b9396` + `a5da742b4`) then archived all 276 Model XNBs out of `game/Content/Model/` to `game/LegacyMesh/`. `GameContentManager.LoadStaticMesh` prefers `.fbx` → `.obj` → `.xnb`, so production load goes straight to the FBX corpus. **End-to-end visually verified at §3.6 close** — every faction's hulls render with materials; modded ships (Vulfen Type WIII, etc.) work identically.

**What landed vs. plan**:
- Steps 1, 4 (`MeshExporter.Export` runtime path) — superseded by offline export
- Step 2 (SunBurn ContentTypeReader stubs, `LightingMaterialReader_Pro`) — done; lives at `Ship_Game/Data/Mesh/SunBurnReaderStubs.cs`
- Step 3 (smoke `Load<Model>` for 5 representative XNBs) — superseded; the FBX corpus is its own coverage
- Step 5 (`Xna31VertexDeclarationReader`) — partial (decode only; Model-XNB drift requires a companion `Xna31ModelReader`). Currently unreachable in production because no Model XNB ships. Re-tagged Phase 4 — the empirical decode is preserved at `Ship_Game/Data/Xna31VertexDeclarationReader.cs` with unit tests, ready for a future contributor if a mod ever needs it.
- Step 6 (run extraction over static subset) — done via legacy branch + `9bd3b7128`
- Step 7 (`.fbx`-first wire-up in `LoadStaticMesh`) — done; `.fbx` → `.obj` → `.xnb` precedence in [GameContentManager.cs:767](Ship_Game/Data/GameContentManager.cs#L767)
- Step 8 (end-to-end validation) — done at §3.6 close

**Carryovers**: cross-branch sync question (cherry-pick legacy commits `f964b6df7` + `5c3a218be` into migration vs. keep status-quo) lives in `project_phase4_legacy_mesh_export_sync.md` for Phase 4 debate.

---

### Original plan (kept for archaeological reference)

**Goal**: All static (non-skinned) ship/hull/projectile/station meshes load and render with original geometry + materials. The `LoadStaticMesh` Phase 2 stub is gone for static assets. The §3.1 CSV identifies the static subset: 122 confirmed static-sunburn + ~12 likely-static-sunburn (in the tool's unreadable set, runtime-confirmed). The 8 static-raw XNBs (3.1 VertexDeclaration drift) are handled in §3.4 step 5.

**Strategy** (revised per developer note + §3.1 inventory): use MonoGame's runtime `ContentManager` (proven LZX + ModelReader path) by stubbing the missing reader chain, then walk the runtime-loaded `Microsoft.Xna.Framework.Graphics.Model` and emit FBX via the already-finished SDNative bone APIs in `MeshExporter.Export`. No standalone XNB decoder; no parallel LZX / VertexBuffer / IndexBuffer parser. The previous developer's mesh-export work is the foundation — we're filling the C#-side import gap they couldn't reach.

**Steps**:
1. **Audit `MeshExporter.Export(Model)` git history**. Pre-Phase-1 there was a non-stub implementation that walked the runtime Model and called `SDMeshAddBone` / `SDMeshAddVertex` / `SDMeshAddMaterial`. `git log -p -- Ship_Game/Data/Mesh/MeshExporter.cs` will surface it. Restore the static-mesh path (bone walk + vertex/index buffer pull + material walk → SDNative DLLAPIs → `Mesh::SaveAsFBX` via NanoMesh, which is re-enabled in §3.2). This is the keystone — everything below feeds into it.
2. **SunBurn ContentTypeReader stubs** (`Ship_Game/Data/Mesh/SunBurnReaderStubs.cs`):
   - For each SunBurn reader the Model XNBs reference — primarily `LightingMaterialReader_Pro`, plus any others surfaced by §3.1 inventory's `reader_chain` column — write a `ContentTypeReader<T>` that consumes the bytes correctly. The point is making `ContentTypeReaderManager.GetTypeReader` resolve so the manifest read succeeds; the consumed data is exposed via portable C# structs that `MeshExporter` reads when walking the Model's `Tag` / extra data.
   - Register via `ContentTypeReaderManager.AddTypeCreator` with the exact full type strings from the XNB manifest (Phase 2 §2.2 confirmed exact-match keys are required — fully-qualified `Type, Assembly, Version, Culture, PublicKeyToken`).
   - Bytes layout for `LightingMaterialReader_Pro` will need empirical decode from one or two example XNBs (likely fields: diffuse texture name, normal texture name, specular texture name, Phong exponent, emissive color). The same byte-format applies to all 122+ XNBs since they were all baked by the same SunBurn version (1.3.2.8).
3. **`ContentManager.Load<Model>` smoke**: with the SunBurn stubs in place, `content.Load<Model>("Model/Asteroids/asteroid1")` should now succeed and return a populated `Model`. Add a test that loads 5 representative static-sunburn XNBs (asteroid1, spacejunk1, torpedo, Caravan, Thorn) and asserts non-null Model with Bones.Count > 0 and Meshes.Count > 0.
4. **`MeshExporter.Export(Model)` walk-through and FBX emit** for static path (depends on step 1's restored implementation):
   - Walk `Model.Bones` → `SDMeshAddBone` per bone (static; no skin weights).
   - Walk `Model.Meshes[*].MeshParts` → for each, pull vertex buffer bytes (via `VertexBuffer.GetData<byte>`) and index buffer bytes; convert to SDNative vertex/index format; call `SDMeshAddVertex` / `SDMeshAddIndex` (or whatever the existing API surface is — confirm in step 1 audit).
   - Walk `Model.Meshes[*].MeshParts[*].Effect` → if it's a stub'd SunBurn material, read the portable struct and call `SDMeshAddMaterial` with diffuse/normal/spec texture references.
   - Trigger `Mesh::SaveAsFBX(path)` via the SDNative DLLAPI.
5. **3.1 VertexDeclaration drift fix** for the 8 static-raw XNBs (`ThrustCylinderB`, `Window`, `muzzleEnergy`, `projBall/Long/Tear`, `Kulrathi/ship12b/c`):
   - Write `Xna31VertexDeclarationReader : ContentTypeReader<VertexDeclaration>` (mirrors the §2.2 `Xna31Texture2DReader` pattern). Register against the exact reader string from the XNB manifest.
   - Empirical decode of the 3.1 binary format:
     - Dump the VertexDeclaration bytes from all 8 static-raw XNBs (use a one-shot diagnostic — read raw bytes after manifest, dump hex). Preserved hex for ThrustCylinderB.xnb (3 elements / 28 bytes) is in `project_phase2_xnb_model_drift.md`.
     - Build hypothesis (likely `int elementCount + per-element { offset:UInt16, format:UInt16, usage:Byte, usage_index:Byte, padding/extra }`). Validate against all 8 samples; iterate.
   - **Bail-out**: if empirical decode is intractable, fall back to a lookup table — for each unique byte pattern in the 8 XNBs, hand-construct the equivalent `VertexDeclaration` and dispatch by hash. Likely 1-3 unique patterns total, given how few XNBs are in the cluster.
6. **Run extraction over the entire static subset**:
   - Walk every `Model/**/*.xnb` classified static-sunburn or static-raw in §3.1's CSV.
   - For each: `MeshExporter.Export(content.Load<Model>(name), name, sidecarPath)`.
   - Commit `.fbx` sidecars under `game/Content/Model/.../mesh.fbx` next to originals (separate commit so reverting sidecars doesn't lose the code changes).
7. **Wire runtime fallback in `GameContentManager.LoadStaticMesh`**:
   - Verify `AssetName` already prefers `.fbx` over `.xnb` when both exist (Phase 2.8.C work). If not, add the precedence.
   - Drop the Phase 2 try/catch + stub fallback. Real exception propagation returns for any unconverted XNB (fail loud).
8. **End-to-end validation**: load a save with diverse ship classes (Terran Battleship, Vulfen Cruiser, Kulrathi Carrier, Remnant XenoCruiser); confirm hulls render with materials. Combat scene: lasers fire against textured ship meshes (not bounding-box stubs).

**Tests added**:
- `UnitTests/Content/SunBurnReaderStubsTests.cs` — feed a known XNB byte chunk to each stub reader; assert the produced struct matches expected (diffuse texture name, etc.).
- `UnitTests/Data/MeshExporterTests.cs` — extend the existing tests to cover `Export(Model)` for a runtime-loaded asteroid1.xnb. Assert the emitted FBX file exists, NanoMesh re-imports it, vertex/index counts are non-zero.
- `UnitTests/Content/Xna31VertexDeclarationReaderTests.cs` — for each of the 8 static-raw XNBs, assert `content.Load<Model>` succeeds and the resulting `VertexDeclaration` has the expected element count + total stride.
- `UnitTests/Data/StaticMeshXnbConversionTests.cs` — bulk pin: assert ≥95% of static-sunburn XNBs in the inventory CSV produce non-null `StaticMesh` with `Geometry.Length > 0` after the §3.4 work.

**Verification**:
- ≥95% of static-sunburn XNBs have a sidecar `.fbx` and render correctly.
- All 8 static-raw XNBs (Thrust cylinder, Window, projectiles, Kulrathi ship12b/c) load via the new VertexDeclarationReader.
- Ship Designer hull tab shows real geometry for all common hull classes.
- Universe combat: ships fire weapons against textured ship meshes.
- Build matrix green.

**Rollback**: revert in two commits — (1) revert the runtime extraction code (stubs + MeshExporter changes), (2) revert the FBX sidecars. After (1) only, sidecars stay loadable so the runtime visual gain is preserved during partial rollback.

**Risk**: **Medium–High**. The runtime + reader-stubs path is bounded (no research-grade decoding for the 122 SunBurn XNBs — they share a single byte format we decode once). The 8-XNB static-raw cluster is the main remaining unknown but its scope is small (8 files, hand-author fallback always available). The biggest unknown is whether the previous developer's `MeshExporter.Export` implementation is recoverable from git history or needs to be re-derived; if it's been deleted entirely, plan an extra day for the static-mesh export bridge.

---

## 3.5 — Particle / Beam / Projectile FX Restoration End-to-end

**Goal**: All particle and weapon visual FX work in combat, depending on §3.3's restored effects (scale, Thrust — BeamFX deferred to step 5) and the §2.9 ParticleManager already wired in Phase 2. **Pulled forward (was §3.9) on 2026-05-04** — projectiles/particles take priority over skinned-mesh animation (the original §3.5) since particles unlock the visible game-feel for the much larger non-Ralyeh ship roster, while skinned animation is scoped to 6 Ralyeh ships (per §3.1 inventory).

**Steps**:
1. Audit the ParticleEmitter / Beam / Projectile / FogOfWar render paths — each was Phase 2 wired against fallback null-effect handling. Outcome of 2026-05-04 audit: no `Effect`-XNB null-guards exist in the particle pipeline (`ParticleEmitter.cs`, `ParticleEffect.cs`, `Particle.cs`, `ParticleManager.cs`) — particle shaders go through `LoadEffect("3DParticles/...")` which throws on missing rather than null-stubbing. The remaining guards are correctly placed: `Beam.cs:170,199` (BeamEffect) and `UniverseScreen.Draw.cs:373` (basicFogOfWarEffect) **stay** until step 5 lands their .mgfxo, and `Projectile.cs:468` is a `BasicEffect` cast-guard unrelated to broken Effect-XNB. The already-restored sites (`ShieldManager.cs:63,135`, `Thruster.cs:71-72`) keep defense-in-depth guards per established pattern. Step 1's only edit-action: refresh stale "Phase 3.3.A" / "Phase 3.3" comment text on the still-stubbed sites to point at "§3.5 step 5".
2. Validate visually in combat: lasers, missiles, beam weapons, ship explosions, shield hits, warp transitions. **Carryover deferred to §3.7** (renderer feature parity): same-empire pre/post screenshots show ship hulls render flat without normal/specular/emissive material maps, which makes engine bells lose their warm hull-side glow and made the engine-trail palette read differently between pre- and post-migration shots even though the Thrust shader formula is op-for-op correct (cross-checked against `phase3-logs/thrust-chunks/thrust_ps_3_0.dis.txt`). Reference shots saved at `phase3-logs/visual-diff/engine-trail_{pre,post}.jpg`. Root cause is `Ship_Game/Data/Mesh/SunBurnStubs.cs::ApplyToBasicEffect` only pushing diffuse + lights to the underlying `BasicEffect` — `NormalMapTexture` / `SpecularColorMapTexture` / `EmissiveMapTexture` slots are populated by `MeshInterface.CreateMaterialEffect` but never sampled. Restoration plan in §3.7 step 4.
3. Per-effect parameter audit — XNA 3.1 effect parameter names may differ subtly from the new shim's exposed names; reconcile via `Effect.Parameters[name]` lookups and update game code if needed.
4. **Phase C texture migration sub-task** (per `project_phase35_phaseC_textures.md`) — replace the 9 retained .xnb texture files (`shieldgradient.xnb` + 8 projectile texture XNBs in `Model/Projectiles/textures/`) with their .dds equivalents. **RESOLVED 2026-05-04** via Option 1 (memory note): copied the four FBX-material `.dds` files (`missile_d`, `missile_s`, `torpedo_d`, `torpedo_s`) into `Model/Projectiles/textures/` so `LoadProjectileTextures` finds them; archived the 9 `.xnb` to `game/LegacyMesh/`; pointed `ShieldManager.cs:60` at the existing `shieldgradient.png`; the four `_0` mip-suffix duplicates were exporter-side artifacts (stripped by the legacy exporter at [RawContentLoader.cs:207](Ship_Game/Data/RawContentLoader.cs#L207)) — never read from `ProjTextDict` so dropped without replacement. `game/Content/Model/` now contains zero `.xnb` files (the Phase C completion marker).
5. **Deferred §3.3 carryover — restore the last 2 broken Effect XNBs** (moved here on 2026-05-04 after steps 1–4 closed §3.3 at "4 of 6 restored"). Order matters: tackle the easier one first to not block the rest of §3.5 on R&D.
   - **`Effects/BasicFogOfWar.xnb`** — disassembly + a previous rewrite attempt already exist (see `project_phase3_3_effects_partial.md`). Open question is RT-state coupling on the LightsTarget RT (s1 binding, `saveState:true` on SafeBegin, sampler/RT-format compatibility). Lives naturally with the §3.7 post-process pass restoration; if §3.7 lands first, fold this in there and drop step 5's BasicFogOfWar bullet.
   - **`Effects/BeamFX.xnb`** — **RESOLVED 2026-05-05**. Apparent "garbled manifest" was a byte-order bug in `x64Migration/Tools/EffectXnbDump`'s LZX framing (`frameSize = (b3 << 8) | lo` was reversed; correct is `(lo << 8) | b3`). For the other 4 effects the wrong-endian frame_size happened to exceed `decompressedSize` and got clamped to the right value, masking the bug. BeamFX's bytes decoded backwards to a value below `decompressedSize` so the clamp didn't fire and the LZX produced shifted output. Fixed both `Program.cs` and `ExtractFxBlob.cs`; XNB then disassembled cleanly to a trivial vs_1_1 + ps_2_0 program (WVP + UV-scroll VS, single tex2D PS). Hand-rewrote as `BeamFX.fx` and shipped `BeamFX.mgfxo`; dropped from `Phase2BrokenEffectXnbs`; null-guards in `Beam.cs` removed. Disassembly preserved at `phase3-logs/beamfx-chunks/`.
   - When each lands: ship a `<Effect>.mgfxo` next to its `.xnb`, drop the entry from `GameContentManager.Phase2BrokenEffectXnbs`, move it from `StubbedEffects` to `RestoredEffects` in `EffectXnbCompatTests.cs`, remove the matching step-1 null guard.

**Tests added**:
- `UnitTests/Graphics/ParticleEffectTests.cs` — emit one particle of each type; assert non-zero pixel coverage on RenderTarget.

**Verification**:
- Combat scene renders all weapon FX correctly.
- No null-effect warnings in `blackbox.log`.
- Zero `.xnb` files remain in `game/Content/Model/` (Phase C completion marker).

**Rollback**: `git revert HEAD`. Null guards return; FX silently absent.

**Risk**: Medium. Most likely failure is a parameter name mismatch between original SunBurn-baked effects and the §3.3 shim's exposed names — pin by test, fix per call-site.

---

## 3.6 — MainMenu Mars 3D Sphere; Phase 2 Cosmetic Carryover Cleanup

**Goal**: Restore the MainMenu Mars planet to a 3D sphere with overlay panels. Address the small set of cosmetic issues left over from Phase 2 close-out.

**Why now**: §3.4 already unblocks the planet sphere mesh; the planet shaders (`PlanetHalo` from §3.3) render; the overlay sprites already work post-§2.7.B PNG fix. This sub-phase wires it all up. (Skinned-mesh §3.10 is unrelated — the Mars sphere is a static mesh.)

**Steps**:
1. Audit the MainMenu planet construction code — pre-migration it composed 5 child overlay panels (`shadow`, `Lights_edge`, `Dust`, `Lights_center`, `Aurora`) on top of a sphere mesh + base `MMenu/planet.png` texture. Confirm the panel composition still works (per Phase 2.7.B PNG fix).
2. Replace the flat strip rendering with a sphere mesh draw call (the `planet_sphere.obj` is loadable since Phase 2.8.C). Wire `PlanetHalo` for the limb glow (loadable since §3.3). Use the §2.8 forward renderer.
3. Confirm Mars renders as a 3D sphere with overlays + halo + correct sun direction.
4. **VideoPlayer.IsLooped**: if Phase 3 ships before a MonoGame upgrade, leave the workaround in place. If the MonoGame version has been bumped past 3.8.1.303 and `IsLooped` is now implemented, restore the setter. Update memory accordingly.
5. **Color.TransparentBlack sweep cleanup** (carried from Phase 2.10): grep for any remaining `Color.TransparentBlack` references and replace with `new Color(0, 0, 0, 0)` per the MonoGame removal.
6. **Final `// TODO Phase 2:` marker sweep**: every remaining marker should be either resolved or re-tagged `// TODO Phase 4:` if genuinely deferred.

**Tests added**:
- Manual screenshot capture of MainMenu — `phase3-3.6-mainmenu-mars.png`.

**Verification**:
- MainMenu Mars renders as 3D sphere with overlays + halo.
- No `// TODO Phase 2:` markers remain in the codebase.

**Rollback**: `git revert HEAD`. Returns to the flat-strip Mars.

**Risk**: Low–Medium. Most failure modes (sphere mesh missing, halo shader missing) are resolved upstream by §3.3/§3.4.

---

## 3.7 — Renderer Feature Parity: Bloom, Distortion, Fog-of-War, Material Maps

**Goal**: Restore the post-process pass chain on top of the §2.8 forward renderer. Bloom from `BloomExtract.xnb` + `BloomCombine.xnb` (Phase 2-loadable; not in the broken set). Screen-space distortion from `Distort.xnb`. Fog-of-war overlay from `BasicFogOfWar` (loadable since §3.3). Plus per-mesh material maps (normal / specular / emissive) that the Phase 1.9 `LightingEffect` stub silently dropped.

**Why now**: depends on §3.3 (BasicFogOfWar) and the renderer's per-frame RenderTarget plumbing already in place from Phase 2.8. The material-maps restoration depends on §3.4's FBX corpus already exposing `NormalMapTexture` / `SpecularColorMapTexture` / `EmissiveMapTexture` slots on the `LightingEffect` (`MeshInterface.CreateMaterialEffect` populates them today; `ApplyToBasicEffect` in `SunBurnStubs.cs` doesn't consume them — only diffuse + lights are pushed to the underlying `BasicEffect`).

**Steps**:
1. **Bloom**:
   - Add a `BloomFilter` class (port the well-known XNA bloom sample logic).
   - Wire as final post-process pass in the forward renderer's `RenderScene`.
   - 4-step pipeline: extract → blur (horizontal + vertical) → combine.
2. **Distortion**:
   - Wire `Distort.xnb` as a per-pixel offset pass on a copy of the back buffer.
   - Used by shield-hit visuals; integrate with the existing shield-hit notification code.
3. **Fog of war**:
   - `BasicFogOfWar` shader operates on a per-system fog mask; integrate with the Universe screen's per-system explored/visible state.
4. **Material maps (normal / specular / emissive)** (§3.5 carryover, see `visual-diff/engine-trail_*.jpg`):
   - Today `LightingEffect` extends `BasicEffect` (Phase 1.9 stub at `Ship_Game/Data/Mesh/SunBurnStubs.cs`). `BasicEffect` natively supports diffuse + 3 directional lights + per-vertex specular but has no normal-map sampler and no emissive-map sampler. Result: ships render flat-shaded — engine bells lose their warm hull-side glow, hulls lose specular highlights and emissive panel-light detail.
   - Write a custom HLSL shader (`Ship_Game/Content/Effects/MeshLighting.fx`) that samples diffuse + normal + specular + emissive maps and the existing 3-directional-light setup. Compile via mgfxc to `MeshLighting.mgfxo`.
   - Subclass `LightingEffect` from `Effect` instead of `BasicEffect`, load the new MGFX, and bind `World/View/Projection` + lights + maps the same way `ApplyToBasicEffect` does today. Maintain the existing public API (`DiffuseMapTexture`, `NormalMapTexture`, `SpecularColorMapTexture`, `EmissiveMapTexture`) so call-sites (`MeshInterface.CreateMaterialEffect`, `PlanetType.CreateMaterial`, etc.) keep working unchanged.
   - Validation: re-shoot the engine-trail comparison and confirm engine bells regain warm hull-side glow + specular highlights matching `phase3-logs/visual-diff/engine-trail_pre.jpg`. Add new pre/post pairs to `visual-diff/` for any other regressions exposed by hull-shader work (likely candidates: planet detail, station panels).
5. Integration tests — render a synthetic scene with a known input, capture the post-process output, assert pixel pattern.

**Tests added**:
- `UnitTests/Graphics/BloomFilterTests.cs` — `RenderTexturedScene_BloomCombines_ProducesBrightening` (compare brightness sum pre/post bloom).
- `UnitTests/Graphics/FogOfWarTests.cs` — synthetic mask + scene → expected output pattern.
- `UnitTests/Graphics/MeshLightingEffectTests.cs` — load a sphere mesh with normal + specular + emissive maps, render to RT, assert non-uniform luminance distribution (specular highlights present, emissive lit areas brighter).

**Verification**:
- Universe map shows fog-of-war correctly.
- Combat shield hits visibly distort.
- Bright weapon impacts produce bloom highlights.
- Ship hulls render with normal-mapped surface detail, specular highlights, and emissive panel glow (verify against `visual-diff/engine-trail_pre.jpg` reference).
- Build matrix green; no perf regression >10% in the §2.10 perf baseline.

**Rollback**: `git revert HEAD`. Post-process passes disable; renderer is bare-pass.

**Risk**: Medium. Bloom and distortion are well-trodden ground; fog-of-war integration with Universe state is the variable. Mitigation: ship each pass as its own commit so any one can be reverted without losing the others.

---

## 3.8 — Shadow Maps (Basic)

**Goal**: Single directional-light shadow map (sun light) for ships in combat. Static-mesh shadow casting only; skinned-mesh shadows are a polish item.

**Approach**: Three sequential phases, each its own commit so any one can revert without losing the others. Strict-additive: failures fall back to the existing unshadowed forward path cleanly.

### §3.8.A — Depth-pass infrastructure

1. Decide the shadow caster's source. `LightingEffectBinder.Apply` (Ship_Game/Data/Mesh/LightingEffectBinder.cs) picks the closest PointLight as the scene's "sun"; the three DirLight0..2 slots also feed lighting. Pick whichever expresses the dominant directional contribution after binder resolution and surface its world-space direction as `ShadowLightDirection`.
2. Add a `ShadowMapComponent` (Ship_Game/Graphics/) that owns a **1024×1024** RT (R32F / `SurfaceFormat.Single`). Start smaller than the original 4096² target; scale up only if §3.8.B's quality demands it. RT lives next to BloomComponent / DistortionComponent in the renderer's component list.
3. **Shadow pass**: depth-only render of all `RenderableMeshes` + `AddedModelMeshes` from the sun's POV (orthographic projection sized to the active scene's AABB). Reuse the iteration order in `SunBurnStubs.SceneInterface.RenderScene` so the shadow caster set matches the lit-pass caster set exactly.
4. **Test in isolation** before §3.8.B touches the lit shader. `UnitTests/Graphics/ShadowMapTests.cs` Phase A: render two boxes from a known sun direction, GetData() the shadow map, assert the front box's pixels read closer-to-camera depth than the back box's pixels at the corresponding texel.

### §3.8.B — Lit-pass sampling

1. Extend `MeshLighting.fx` (NOT BasicEffect — that path was replaced in §3.7 step 4 Phase A) with `ShadowMap` texture + `LightViewProjection` matrix uniforms. Both default-disabled so meshes/scenes that don't bind shadows fall through to the existing path.
2. PS samples the shadow map at the surface's projected light-space UV; computes `shadowFactor = (sampledDepth + bias < surfaceLightDepth) ? 0 : 1`. Multiply `shadowFactor` into the diffuse + specular contributions (per-light if multiple lights cast; for §3.8 only the single sun-light caster).
3. **Bias**: small constant offset (start `0.001`) to suppress acne. Tune against the `ShadowMapTests` two-box scene before extending to ships.
4. Wire `ShadowMapComponent` into `SceneInterface.RenderScene` as a pre-pass before `LightingEffectBinder.Apply`; the binder pushes `ShadowMap` + `LightViewProjection` onto SharedFx alongside the existing lights so `CopySharedLighting` propagates them to per-mesh effects.

### §3.8.C — PCF (only if §3.8.B's 1-tap output is unshippable)

1. 3×3 PCF (or 4-tap rotated jitter) for soft edges. Skip entirely if the 1-tap result already looks acceptable in combat — additional taps cost real frametime.

**Tests added**:
- `UnitTests/Graphics/ShadowMapTests.cs` — Phase A: depth-only correctness on two boxes; Phase B: assert receiver pixels are darkened where the occluder's shadow falls.

**Verification**:
- Combat ships cast shadows on each other under the sun light.
- Build matrix green; shadow render adds <2ms per frame at 1080p (perf budget).

**Rollback**: `git revert <phase>`. Reverting Phase B leaves the depth pass running but unused (cheap waste); reverting Phase A removes the component entirely. Each phase is independently revertable.

**Risk**: Medium–High. Shadow acne / Peter Panning are well-known issues; budget tuning time. Mitigation: phase split keeps the lit-shader change (the riskiest piece) in §3.8.B, separately revertable from the infra in §3.8.A. Shadow rendering is a strict additive feature — failures fall back to unshadowed cleanly.

---

## 3.9 — FBX TransparencyFactor Write Fix + Mesh Re-Export

**Goal**: Fix the legacy mesh-exporter's FBX `TransparencyFactor` write/read inversion at the source, then re-run the legacy export so the corpus round-trips correctly without the C# workaround. Pulled forward from the Phase 4 candidate list because the **Combined Arms mod** introduces many additional ships that will go through the same exporter; landing the fix before that re-export saves a re-export cycle.

**Reference**: see `Mesh_Fbx.cpp:812` (write) and `Mesh_Fbx.cpp:513` (read). The read path correctly inverts FBX's `TransparencyFactor` (FBX convention: 1.0 = fully transparent) into the engine's opacity-flavored `Alpha` (1.0 = opaque) via `Alpha = 1 - TransparencyFactor`. The write path stuffs opacity directly into `TransparencyFactor` without the inverse, so an opaque XNA material round-trips as `Alpha=0` and ships render fully transparent.

The C# workaround landed in commit `76c9cdccb` ([SunBurnStubs.cs](Ship_Game/Data/Mesh/SunBurnStubs.cs) `SetTransparencyModeAndMap`) — when the call site declares `TransparencyMode.None`, force `Alpha=1` regardless of what the material reports. This is what makes ships visible today.

**Steps**:
1. Submodule (NanoMesh, on the local `blackbox-migration` branch — see `project_nanomesh_local_branch.md`):
   ```cpp
   // Mesh_Fbx.cpp:812
   mat->TransparencyFactor.Set((double)(1.0 - m.Alpha));
   ```
   Commit on `blackbox-migration`. Bump parent submodule pointer.
2. **Switch to `legacy/mesh_exporter_xna31`** parent branch — that's where the working MeshExporter and the XNA-3.1 toolchain live (per `project_phase4_legacy_mesh_export_sync.md`). Re-run the export over the full corpus, including the Combined Arms mod ship folders.
3. Validate FBX round-trip: spot-check `ship15h.fbx` and `Kulrathi_Station.fbx` for `Alpha=1.00` in the [mat] log we used during the migration session (re-add temporarily if needed). Confirm no faction-specific transparent ships were broken (the few that were genuinely Alpha<1 should retain it).
4. **Switch back to `migration/phase3-x64-monogame`**. Hand-copy the new FBX corpus into `game/Content/Model/...` (replaces the corpus committed in `9bd3b7128`).
5. Drop the C# workaround at `SunBurnStubs.cs:626-641` — restore the simpler `Alpha = alpha` line. Re-test.
6. Smoke-test: every faction's hull, the modded stations, the Combined Arms ships. Lit textured rendering, no invisible ships.

**Tests added**: none — visual smoke is the validation gate.

**Verification**:
- All ships (vanilla + Combined Arms) render textured, no invisible/transparent regressions.
- The `[mat]` log (when re-enabled briefly) shows `Alpha=1.00` for the previously-broken set.
- The C# `TransparencyMode.None` branch in `SetTransparencyModeAndMap` is gone — confirms the fix is at the right layer.

**Rollback**: keep the C# workaround in place — that path makes ships visible regardless. The C++ + re-export change is purely about correctness of the FBX corpus round-trip; if the re-export goes sideways, revert the corpus to `9bd3b7128` and leave the workaround.

**Risk**: Medium. The C++ fix itself is one line. The risk is in the re-export — coverage of all factions including modded ones, whether the new FBXes happen to regress some other property the C# loader cares about, and the toolchain churn of switching branches and copying ~150 MB of binary content.

**Dependencies / cross-refs**:
- `project_phase4_legacy_mesh_export_sync.md` — the legacy/migration branch split. This sub-phase deliberately keeps the split; it doesn't resurrect the exporter on migration. The cross-port question stays open for Phase 4.
- `project_nanomesh_local_branch.md` — the submodule's local-only branch state. The §3.9 fix lands on the same `blackbox-migration` branch.
- `project_phase35_phaseC_textures.md` — Phase C texture migration. Folded into §3.5 step 4 (see above) since the particle/projectile rendering pipeline is what consumes those textures.

---

## 3.10 — XNB Model Decode — Skinned/Animated Meshes (Ralyeh ship17 family)

**Goal**: The 6 Ralyeh ship17 XNBs (`ship17a.xnb` confirmed skinned-sgmotion, `ship17b-f.xnb` likely-skinned siblings per §3.1 inventory) load with bone hierarchy + skin weights + animation clips, and the meshes render in-game with their bind-pose deformation visible. Clip playback at runtime is **§3.10.B (optional follow-up)** — the success-gate for §3.10.A is "skinned meshes visible with bones, even if clips don't play yet."

**Why split into 3.10.A and 3.10.B**: per the developer note, the previous mesh-conversion attempt was specifically blocked on the C#-side skeletal extraction — bone walk + skin weight extraction + clip data into the SDNative bone APIs. Once that's done (§3.10.A), the meshes are visible. Runtime clip playback (custom skin shader + `BoneAnimationPlayer`) is well-trodden territory and decoupled from extraction; we can land it in §3.10.B if there's time.

### §3.10.A — Skinned mesh extraction (the hard part)

**Steps**:
1. **SgMotion ContentTypeReader stubs** (`Ship_Game/Data/Mesh/SgMotionReaderStubs.cs`):
   - Reader chain from §3.1's inventory of `ship17a.xnb` (verified):
     ```
     SgMotion.Pipeline.SkinnedModelReader, XNAnimation, Version=0.7.0.0
     SgMotion.Pipeline.SkinnedModelBoneReader, XNAnimation, Version=0.7.0.0
     SgMotion.Pipeline.AnimationClipReader, XNAnimation, Version=0.7.0.0
     Microsoft.Xna.Framework.Content.TimeSpanReader  (built-in)
     ```
   - Plus the standard chain (`ModelReader`, `VertexDeclarationReader`, `VertexBufferReader`, `IndexBufferReader`) and SunBurn `LightingMaterialReader_Pro` (already stubbed in §3.4).
   - Decode SgMotion's wire format empirically from `ship17a.xnb` bytes (`SkinnedModelReader` is likely a thin wrapper over the standard ModelReader plus a list of `SkinnedModelBone` and `AnimationClip` instances). XNAnimation's source is on GitHub if needed for ground truth (`XNAnimation/Pipeline/Skinned*Reader.cs` historical archives).
   - Stubs surface portable C# structs: `SkinnedBoneData[]`, `AnimationClipData[]` (each with `keyFrames: BoneKeyframe[]`).
2. **Extend `MeshExporter.Export`** (built in §3.4) with a skinned-mesh path:
   - Detect skinned via `model.Tag` (or however the SkinnedModelReader stashes its data).
   - Walk `SkinnedBoneData[]` → for each, call `SDMeshAddSkinnedBone(name, boneIndex, parentBone, bindPose, inverseBindPoseTransform)` (the API signature already exists in `SdAnimation.h`).
   - Walk vertex buffer's skin weights — XNA's `VertexElementUsage.BlendIndices` + `BlendWeight` carry 4 indices + 4 weights per vertex. Pass through to NanoMesh via the existing vertex-attribute API.
   - Walk `AnimationClipData[]` → `SDMeshCreateAnimationClip(name, duration)` per clip; per bone-track in clip, `SDMeshAddBoneAnimation(clip, skinnedBoneIndex)`; per keyframe, `SDMeshAddAnimationKeyFrame(clip, anim, keyFrame)`.
   - Trigger `Mesh::SaveAsFBX` — NanoMesh's existing FBX writer serializes skin clusters + AnimStack via FBX SDK 2020 (re-enabled in §3.2; already supports skin/anim writes per FBX SDK).
3. **Run extraction over the 6 Ralyeh ship17 XNBs**:
   - Convert ship17a (the runtime-confirmed skinned). Verify the FBX round-trips through NanoMesh's FBX importer with bone count + clip count matching the inventory.
   - Convert ship17b-f. These are in the inventory's "unreadable-by-tool" set but the runtime path will succeed (proven in §3.1).
   - Commit FBX sidecars under `game/Content/Model/Ships/Ralyeh/ship17*.fbx` (separate commit from code changes).
4. **End-to-end validation (§3.10.A success gate)**: load a save where a Ralyeh ship is on-screen; confirm the mesh renders with deformed bind-pose geometry (NOT a stub). The mesh appears correctly-shaped even though no animation clip is playing yet.

**§3.10.A tests added**:
- `UnitTests/Content/SgMotionReaderStubsTests.cs` — feed `ship17a.xnb` byte chunks to each stub; assert produced structs match expected bone count + clip count + clip names.
- `UnitTests/Data/MeshExporterTests.cs` — extend with `Export_SkinnedModel_ProducesValidFbx` test for ship17a.xnb. Round-trip via NanoMesh FBX importer; assert bones + skin weights + clips survive.

**§3.10.A verification**:
- All 6 Ralyeh ship17 XNBs convert to FBX with bone hierarchy intact.
- In-game Ralyeh ships render visibly (bind-pose, but visible).
- Build matrix green.

### §3.10.B — Runtime animation playback (optional follow-up)

**Goal**: clips actually play in-engine. Skip if §3.10.A shipped on a tight schedule; the bind-pose-only path is acceptable for Phase 3 close.

**Steps**:
1. **Animation runtime** `SDGraphics/Mesh/BoneAnimationPlayer.cs`:
   - Holds current clip, current time, blend state.
   - `Update(GameTime)` — advance time; sample interpolated bone transforms per skinned bone; write to `Matrix[]` palette.
   - Interpolation: linear for translation, slerp for rotation. Loop / hold / one-shot per clip.
2. **Skin shader** `game/Content/Effects/SkinnedEffect.fx`:
   - VS: read 4 bone weights + 4 bone indices per vertex; matrix-palette skin.
   - PS: same forward-lit path as §2.8's `LightingEffect`.
   - Compile via mgfxc (Phase 2.6.A toolchain) → `SkinnedEffect.mgfx`.
3. **Forward renderer integration**:
   - Add `SkinnedMesh` polymorphism over `Mesh`; bind `SkinnedEffect` + matrix palette per draw.
4. **Animation triggers**:
   - Survey what gameplay event triggers Ralyeh ship animations (likely turret traverse or capital-ship engine flares — gameplay code-driven, not clip-driven). If no clip-driven triggers exist in the original codebase, §3.10.B is a no-op for clip playback and only the runtime infrastructure ships.

**§3.10.B tests added**:
- `UnitTests/Graphics/BoneAnimationPlayerTests.cs` — `Sample_AtZero_ReturnsBindPose`, `Sample_AtClipDuration_LerpsToFinalKey`, `Sample_BeyondDuration_LoopsByDefault`.
- `UnitTests/Graphics/SkinnedEffectTests.cs` — palette overflow guard + render-skinned-quad pixel match.

**§3.10.B verification**:
- Ralyeh ship animation clip plays visibly in-game (if a trigger exists).
- Build matrix green.

**Rollback**:
- §3.10.A revert: drop SgMotion stubs + MeshExporter skinned extension + FBX sidecars in 2-3 commits. Ralyeh ships return to Phase 2 stub bounding boxes.
- §3.10.B revert: drop `SkinnedEffect.mgfx` + `BoneAnimationPlayer` + forward-renderer skinned dispatch. Ralyeh ships fall back to bind-pose-only rendering (still a §3.10.A win).

**Risk**: **Medium–High** (revised from "Very High"). The C++ infrastructure already exists; only the C#-side bridge is new. SgMotion's wire format is empirically decoded from ship17a.xnb's bytes — bounded R&D. §3.10.B is well-trodden territory once §3.10.A lands. Mitigation: **§3.10.B is explicitly optional** — Phase 3 close-out can ship with §3.10.A only (bind-pose Ralyeh hulls visible, clips not playing). Surface this trade-off to the user when entering §3.10.

---

## 3.11 — *Moved to Phase 4 (2026-05-07)*

The visual polish pass that previously lived here (projectile dynamic glow light, glow-map emissive, muzzle FX check, sun Z, specular intensity, fog-of-war map dimness, MainMenu polish residue) has been moved to Phase 4. Phase 3 closes on the renderer feature parity already landed in §3.5/§3.7/§3.8/§3.10 — visible quality wins shipped per sub-phase, sign-off is what's left.

The full item list is preserved in `migration-plan-phase4.md`. No information lost.

---

## 3.12 — Phase 3 Close: PHASE3_RESULTS.md, Runtime Smoke, Final Memory Cleanup

**Goal**: Sign off Phase 3. Produce a results document mirroring `PHASE1_RESULTS.md` / `PHASE2_RESULTS.md`. Run the full runtime smoke test. Update memory entries to reflect resolved status.

**Steps**:
1. **Runtime smoke**: launch `game/StarDrive.exe`. Walk through MainMenu → New Game → Universe → engage in combat → return to MainMenu → exit. Capture `phase3-runtime-smoke.log`.
2. **Build matrix**: 5 configs × x64. Capture all 5 logs under `phase3-logs/wrap/`.
3. Author **`PHASE3_RESULTS.md`** in `x64Migration/`. Sections (mirroring PHASE2_RESULTS.md):
   - Sub-phase completion table with commit refs.
   - Build matrix outcomes.
   - Success-gate verification (each item from "Phase 3 Goals" above, ✅ / ❌).
   - What works at runtime (3D hulls, animations, beam weapons, particles, shadows, post-process).
   - Carryover to Phase 4 (any remaining polish items).
   - Migration retrospective: total commits across Phase 1+2+3, total LOC delta, what went well / what would have been done differently.
4. **Memory file updates**:
   - `project_phase2_xnb_model_drift.md` → mark RESOLVED with §3.4/§3.10 commit refs.
   - `project_phase2_effect_xnb_drift.md` → mark RESOLVED with §3.3 commit ref.
   - `project_phase2_backlog_fbx.md` → mark RESOLVED with §3.2 commit ref.
   - `project_phase2_backlog_runtime.md` → final status: all carryovers resolved or explicitly deferred to Phase 4.
   - `MEMORY.md` → update one-line hooks for the four files above.
   - Author new `project_phase3_xnb_model_decode.md` capturing what we learned about the XNA 3.1 VertexDeclaration binary format — for future migration projects in this codebase or others.
5. **Open Phase 3 PR** and tag `phase3-end`.

**Verification**:
- All Phase 3 success-gate items verified.
- PHASE3_RESULTS.md committed; memory files updated.
- Build matrix green; runtime smoke clean.

**Rollback**: N/A (sign-off step). If a regression is found post-merge, revert specific sub-phase commits — each is independently revertible by design.

**Risk**: Low. Sign-off + documentation.

---

## Cross-cutting Concerns

### Test infrastructure carryover from Phase 2

Phase 2's `EffectXnbCompatTests` pinned the broken-effect list — that test gets rewritten in §3.3. Similarly, Phase 2's `MeshImporterTests` cover the OBJ runtime; §3.2 extends them to FBX. Don't delete the Phase 2 tests — extend them.

### Performance budget

Phase 2 baseline (post-2.8) was ~16ms/frame at 1080p in MainMenu (60fps achievable). Phase 3's renderer additions (skinning, shadow map, post-process) need to fit in that budget. Per §3.7/§3.8, soft cap: <10% regression vs Phase 2 baseline at MainMenu, <20% at peak combat. Capture frametime in §3.12 smoke logs.

### Mod compatibility

Mods under `game/Mods/` ship their own `*.hull` files (15 mod directories per §3.1 survey). The §3.4 sidecar `.fbx` strategy means mods that referenced original 2013 ship XNBs will pick up the converted FBX automatically (mod routing falls through to vanilla). Mods that ship their own custom XNB Models go through the same `LoadStaticMesh` path and the restored `MeshExporter.Export` will handle them too — at runtime, on first access, with the FBX sidecar cached for subsequent loads. No separate CLI utility needed; mod authors get the conversion for free via the regular game launch.

### Branch hygiene

Each sub-phase commits to `migration/phase3-x64-monogame`. Open one PR per sub-phase against `migration/monogame_migration` (matches Phase 2's pattern; PRs are easier to review per-step). Final §3.12 PR closes the phase.

### Phase 4 placeholder

After Phase 3 ships, ARCHITECTURE.md §9 "Suggested Migration Order" lists "Phase 4: Polish" — this is where HDR, advanced lighting models, AI improvements, and any remaining Phase 3 carryovers land. Out of scope for this plan document; mention only.

**Confirmed Phase 4 carryovers** (as of 2026-05-07; full plan in `migration-plan-phase4.md`):
- **Combined Arms mod regression sweep** — re-export every Combined Arms hull through the legacy/mesh_exporter_xna31 pipeline (§3.10.B.8 fixes apply uniformly across the mesh corpus, but only the Ralyeh ship17 family was visually verified) and run Combined Arms in-game to confirm no regressions vs pre-migration. Likely first Phase 4 item.
- **Build hygiene: zero warnings on x64** — drive both the C# (`StarDrive.csproj`) and the C++ (`SDNative.vcxproj`) build to zero warnings on `Release|x64`. Baseline at Phase 3 close (commit c5c5159ea): C# 30 warnings (8× CS0618 obsolete API, 4× each of CS0108/CS0649/CS8509/CS8981, 2× each of CS8600/SYSLIB0014/CA2014), C++ 39 warnings (20 in third-party `lodepng.cpp`, 5 in `SlabAllocator.h`, smaller clusters elsewhere). Two-track fix: project-internal warnings get fixed at the source; vendored-third-party warnings (`SDNative/3rdparty/`) get suppressed in `SDNative.vcxproj` via per-file `<DisableSpecificWarnings>`. After the cleanup, treat the build as warnings-as-errors gated so future regressions can't sneak in.
- **Visual polish pass (was §3.11)** — 7 items: MainMenu polish residue, projectile dynamic glow light, glow-map emissive promotion, muzzle FX regression check, sun Z / depth ordering, specular intensity, fog-of-war map circle dimness.
- **NanoMesh upstream PR** — push the local `blackbox-migration` branch (FbxSkin/FbxCluster read+write, FbxAnimStack/FbxAnimCurve read+write, TransparencyFactor write fix, robust cluster bind-matrix recovery, ClusterTransformLinkInverseGL Assert hardening) to NanoMesh upstream as a pull request so a fresh clone of the project doesn't depend on a local-only branch.
- **Steam SDK x64 via Steamworks.NET** *(last Phase 4 item)* — moved out of §3.11. Full recipe in `migration-plan-phase2.md` "Deferred Final Step" appendix. 6-method external surface (`Initialize`, `IsInitialized`, `RequestStats`, `AchievementUnlocked`, `ActivateWebOverlay`, `Shutdown`); AppID 220680 already in `game/steam_appid.txt`. Drop vendored x86 `GARSteamManager.dll` + `steam_api.dll`; rely on Steamworks.NET's `steam_api64.dll`.

---

## Risk Summary

| Sub-phase | Risk | Mitigation |
|---|---|---|
| 3.1 Inventory | Low | ✅ Closed 2026-05-02. Tool committed (`19a323bf7`); CSV + summary in `phase3-logs/`. |
| 3.2 FBX SDK swap | Medium | Per-call-site fixup; surgical fallback documented |
| 3.3 Effect XNB shim | Medium–High | Two-step decode (direct → disassemble); per-effect hand-rewrite as last resort |
| **3.4 Static XNB Models** | **Medium–High** (revised down from High) | SunBurn stubs + restored `MeshExporter.Export` use the proven runtime path. The 8-XNB static-raw cluster has lookup-table fallback. The 122 SunBurn XNBs share one byte format — decode once, reuse for all. |
| 3.5 Particle FX | Medium | Parameter name reconciliation per call-site. Includes the Phase C texture XNB→DDS migration sub-task. |
| 3.6 MainMenu Mars | Low–Medium | Depends on §3.3/§3.4 |
| 3.7 Post-process + material maps | Medium | Per-pass commits; revert independently. Material-maps step pulled in from §3.5 carryover. |
| 3.8 Shadow maps | Medium–High | Strictly additive; falls back cleanly |
| 3.9 FBX export fix + re-export | Medium | One-line C++ fix; risk is in the re-export coverage (~150 ships) and whether new FBXes regress anything visible. Combined Arms mod ships are why this matters. |
| **3.10.A Skinned mesh extraction** | **Medium–High** (revised down from Very High) | Scope shrunk to 6 Ralyeh XNBs per §3.1 inventory. C++ side already finished (per developer note). SgMotion wire format is bounded R&D. Moved to last so its R&D doesn't block visible quality wins. |
| 3.10.B Skinned runtime playback | Medium (optional follow-up) | Decoupled from §3.10.A. Bind-pose-only is acceptable for Phase 3 close. |
| ~~3.11 Small finishes~~ | — | **Moved to Phase 4 (2026-05-07).** See `migration-plan-phase4.md`. |
| 3.12 Sign-off | Low | Documentation only |

**Risk delta from the 2026-05-02 architectural unlock**: §3.4 dropped from High to Medium–High, the skinned-mesh sub-phase split into §3.10.A (Medium–High) + optional §3.10.B (Medium). The previous developer's mesh-export work + the §3.1 inventory together collapsed the original "research-grade XNB decoder + skeletal runtime" surface into "ContentTypeReader stubs + restore `MeshExporter` + reuse SDNative bone APIs." The 8-XNB static-raw cluster (3.1 VertexDeclaration drift) is now the only research-grade item, and its scope is tightly bounded.

**Order delta from 2026-05-05**: skinned-mesh sub-phase renumbered §3.6 → §3.10 and physically moved to the end of Phase 3 (just before Steam SDK). Reasoning: it affects only 6 Ralyeh ships and is the highest-R&D item, while §3.7 (post-process + material maps) and §3.8 (shadows) deliver visible quality wins across the entire ship/planet/station roster. Doing the broad-impact work first means a bad spiral on §3.10 doesn't block the rest of Phase 3's visual gains; if §3.10 has to slip into Phase 4, Ralyeh ships keep rendering as static FBX bind-pose meshes (their state today), which is acceptable per `project_phase2_xnb_model_drift.md`.

Phase 3 still ships with explicit fallback levels at each step:
- §3.4 fallback: hand-construct `VertexDeclaration` for the 8 static-raw XNBs as a lookup table if empirical decode is intractable.
- §3.10.A fallback: ship FBX sidecars for ship17a only (the runtime-confirmed skinned XNB) if ship17b-f's wire format diverges.
- §3.10.B is itself the fallback for §3.10 — bind-pose-only rendering is acceptable.

Negotiate fallbacks explicitly with the user when entering each sub-phase.
