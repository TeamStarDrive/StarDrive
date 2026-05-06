# Phase 2 Migration Plan — Content Pipeline + Rendering Core

## Phase 2 Wrap-up Status (2026-05-02)

**Phase 2 closed.** Outcomes captured in [PHASE2_RESULTS.md](PHASE2_RESULTS.md). Game boots end-to-end on net8 + MonoGame 3.8.1.303; MainMenu → Universe → Combat all reachable; planet bodies render with correct sun-direction lighting.

**Three sub-phase items deferred to Phase 3** (rather than left looking unfinished):

| Item | Where it lived | Status |
|---|---|---|
| §2.8.D — XNB Model format drift (276 ship/hull/projectile/station/effect meshes) | §2.8 carry-over; gap was documented at 2.8.C close | **Deferred to Phase 3.** Stub at `GameContentManager.LoadStaticMesh` keeps runtime functional. Resolution paths in `project_phase2_xnb_model_drift.md`. |
| §2.10 — FBX SDK 2018→2020 ABI fix (9 asteroid `.fbx` meshes) | §2.10 step 1 | **Deferred to Phase 3.** `NANOMESH_NO_FBX=1` retained on x64. Resolution path in `project_phase2_backlog_fbx.md`. |
| §2.2 — Six broken Effect XNBs (BeamFX, scale, Thrust, desaturate, BasicFogOfWar, PlanetHalo) | §2.2 step 1 left these stubbed pending source recovery | **Deferred to Phase 3.** Stubs in `Phase2BrokenEffectXnbs`; pinned by `EffectXnbCompatTests`. Resolution paths in `project_phase2_effect_xnb_drift.md`. |

§2.6 (Steam SDK x64) remains parked at the very end of the migration in the "Deferred Final Step" appendix (unchanged). §2.10 cleanup work that wasn't FBX-dependent (Color.TransparentBlack sweep, remaining `// TODO Phase 2:` markers) is also handed to Phase 3 cleanup.

The sub-phase narratives below are preserved unedited as the historical record. Read PHASE2_RESULTS.md for the as-shipped outcome.

---

## Context

[Phase 1](migration-plan-phase1.md) delivered the foundation: x64 process, MonoGame integrated, SDSunBurn / XNAnimation / XNA 3.1 fully purged, build matrix green across 5 configs × x64. The runtime smoke test (§1.10 step 7) confirmed the process boots, MonoGame `GraphicsDevice` initializes, and the game reaches `ScreenManager.LoadContent` — but stops there with three classes of failures, all explicitly anti-goal'd in Phase 1 ("3D rendering working, content loading working — Phase 2+"). See [PHASE1_RESULTS.md](PHASE1_RESULTS.md).

[ARCHITECTURE.md §9](ARCHITECTURE.md) sketched the post-Phase-1 work as two phases:
> **Phase 2: Rendering Core** — 2a SunBurn out of ScreenManager, 2b MonoGame SpriteBatch 2D, 2c MGFX shaders, 2d DeferredRenderer rewrite.
> **Phase 3: Content Pipeline** — 3a XNB through MGCB, 3b RawContentLoader validation, 3c mod routing.

Phase 1's runtime smoke inverted that ordering. **Content loading is upstream of rendering at boot**: the game can't open the main menu without loading SpriteFonts (XNB), and SpriteFont XNBs from XNA 3.1 fail `Texture2D.ValidateParams` with a 4× size mismatch. That's the actual binary-format rebake problem — not a stub-able edge case. So this Phase 2 plan reorders: **content pipeline first, then rendering core, then SunBurn replacement**. ARCHITECTURE.md's "Phase 4: Polish" is deferred unchanged.

This document expands the work into 10 sequenced, individually-committable sub-phases with explicit treatment of the **Phase 1 leftovers** (tolerance patches and stubs left behind in §1.10 to push past the smoke-test crashes).

The intended outcome of Phase 2 is **a 64-bit MonoGame StarDrive process that boots through to a navigable main menu, with text rendering, 2D UI, basic 3D scenes, and the Phase 1 tolerance patches removed**. Post-processing (bloom), shadow maps, deferred rendering, and skeletal animation are deferred to **Phase 3** (Polish / Advanced Rendering).

## Phase 1 Leftovers (Inputs to Phase 2)

These were intentional tolerance patches in `migration/phase1-x64-monogame` to keep the foundation provable. Each must be reversed as its underlying issue is fixed:

| File | Patch | Reverted in |
|---|---|---|
| `SDGraphics/Shaders/Shader.cs` | `FromFile` returns null | 2.2 (MGFX pipeline) |
| `SDGraphics/Sprites/SpriteRenderer.cs` | ctor + Begin/End/ShaderBegin/ShaderEnd null-tolerant | 2.4 (Restore 2D UI) |
| `Ship_Game/GameScreens/ScreenManager.cs` | `LoadContent` skips `Load<SceneEnvironment>` | 2.7 (SunBurn replacement) |
| `Ship_Game/GameScreens/ScreenMediaPlayer.cs` | VideoPlayer.Volume/IsLooped wrapped in try/catch | 2.5 (Media Foundation) |

Also unstubbed during Phase 2:

- `Ship_Game/Data/Mesh/SunBurnStubs.cs` — entire SunBurn replacement (2.7)
- `Ship_Game/Data/Mesh/MeshImporter.cs` / `MeshExporter.cs` returning null/false (2.10)
- `Ship_Game/Data/Mesh/StaticMesh.cs` Draw is no-op (2.8)
- `GameContentManager.LoadEffect` throws (2.2)
- FBX mesh import disabled in x64 (2.10)

## Confirmed Strategic Decisions

| Decision | Choice | Rationale |
|---|---|---|
| XNB rebake strategy | **Targeted: rebake only what XNA → MonoGame format drift broke** (SpriteFonts) — *not* a wholesale content rebuild | The codebase already has a non-XNB content path: `GameContentManager.AssetName` routes by extension; `.png`/`.dds`/`.fbx`/`.obj` go to `RawContentLoader` (works in MonoGame). `Content/Textures/` is dominated by `.dds`/`.png` already; `Content/Fonts/*.xnb` is the only directory exclusively bound to the broken XNB pipeline. Effects rebake from `.fx` source via §2.2 MGFX. |
| MGCB content output | **In-place under `game/Content/`** | Match existing GameContentManager paths and mod routing. Avoid forking the directory layout. |
| Extension-less `content.Load<T>` calls | **Audit; prefer adding the extension over rebaking** | Adding `.png`/`.dds` to the load string routes through the existing RawContentLoader fast path and avoids dragging more assets into MGCB. Reserve rebake for assets that genuinely need it (SpriteFont, possibly Texture3D for `Effects/NoiseVolume`). |
| HLSL shader profile | **PS_4_0_level_9_x / VS_4_0_level_9_x minimum** | XNA 3.1's `ps_2_0` is gone in MonoGame's MGFX. `_level_9_x` is the minimum that compiles on D3D11 (WindowsDX). |
| MGFX compilation | **MSBuild target via MonoGame.Content.Builder.Task** | Already added in §1.7. Compile `.fx` → `.mgfx` at build time, no manual checked-in `.xnb`. |
| SunBurn replacement | **Forward renderer using MonoGame `BasicEffect` (Phase 2)** + **deferred renderer port (deferred to Phase 3)** | Get to a working main menu + basic 3D fast. BasicEffect supports diffuse + 3 directional lights natively. Defer post-process / shadow-map / deferred rendering until the boot path is solid. |
| `LightingEffect` stub | **Inherits `BasicEffect` for compatibility** | Already done in Phase 1's SunBurnStubs. Phase 2 fleshes out the surrounding pipeline (LightManager, SceneInterface). |
| Splash/cinematic videos | **Tolerate failure, do not block boot** | If Media Foundation codec stack isn't available, skip splash and proceed to MainMenu. Keep the §1.9 gracefulness. |
| Steam SDK | **Swap to Steamworks x64 build** | Cleanest fix. `0x8007000B` BadImageFormat at boot is the only blocker. |
| Skeletal animation | **Stay disabled in Phase 2** | XNAnimation removed in Phase 1; no in-game ship/UI uses it (only test fixture). Defer SgMotion-equivalent to Phase 3 if a use case surfaces. |
| Shadow maps | **Disabled in Phase 2** | SunBurn shadow stack is gone. BasicEffect has no shadow map support. Defer to Phase 3 deferred renderer port. |
| Deferred rendering | **Disabled in Phase 2; forward only** | DeferredRenderer port is Phase 3. |
| Bloom / post-process | **Disabled in Phase 2** | Phase 3 work. |
| Particle system | **Restore in Phase 2 with current MonoGame Effect path** | Already API-fixed in §1.8. Verify vertex buffer compatibility; cosmetic but visible. |

## Phase 2 Success Gate

The user runs `game/StarDrive.exe`. The process:

1. All Phase 1 success-gate criteria still hold (x64 process, window opens, blackbox.log, clean exit).
2. **Boot reaches the main menu.** Splash either plays or is gracefully skipped.
3. **Text renders.** Fonts (Arial, Pirulen, etc.) draw via SpriteBatch + precompiled Simple.fx → SpriteRenderer.
4. **2D UI works.** Buttons, panels, scroll lists are visible and respond to mouse/keyboard.
5. **Navigate to at least one secondary screen** (Ship Design, Race Design, or a non-game screen) without crash.
6. **3D viewport in Ship Design renders the hull mesh.** No SunBurn — MonoGame BasicEffect via the new forward renderer. Lighting may be flat/unshadowed.
7. **All Phase 1 tolerance patches removed**, replaced with real implementations or graceful runtime degradation that's not a `try/catch + log + return null` shim.
8. **Build matrix still green** across 5 configs × x64. No regressions in Phase 1's foundation.

**Anti-goals for Phase 2** (deferred to Phase 3+):
- Shadow maps, dynamic lighting beyond BasicEffect's 3-directional-light limit
- Deferred rendering pipeline
- Bloom + post-processing
- HDR / tone mapping
- Skeletal animation
- Universe screen full functionality (planet shaders, atmospheric scattering, starfield depth)
- Combat screen full functionality (beam effects, explosions)
- Save/load round-trip with newly-baked content
- Network / multiplayer (none planned per ARCHITECTURE.md §5.6)

---

## Sub-phase Index

| # | Title | Risk |
|---|---|---|
| 2.1 | Baseline checkpoint, Phase 2 branch, integrate Phase 1 | Low |
| 2.2 | MGFX shader pipeline; revert `Shader.FromFile` null patch | Medium |
| 2.3 | SpriteFont rebake + content-load audit | Medium |
| 2.4 | Restore 2D UI: SpriteRenderer, fonts, atlas — main menu milestone | Medium |
| 2.5 | VideoPlayer / Media Foundation; revert `ScreenMediaPlayer` try/catch | Low–Medium |
| 2.6 | Steam SDK x64 swap | **DEFERRED** — see "Deferred Final Step" |
| 2.6.A | .NET 8 framework upgrade; MonoGame 3.8.0.1641 → 3.8.1+; re-enable VideoPlayer | Medium |
| 2.7 | SunBurn replacement: data-carrier upgrade (Scope A) — renderer deferred to 2.8 | **High** |
| 2.8 | Mesh rendering: `StaticMesh` + `RenderableMesh` restoration | Medium–High |
| 2.9 | Particle system + ship-design 3D viewport | Medium |
| 2.10 | FBX mesh import re-enable in x64; cleanup; Phase 2 sign-off | Medium |

Each sub-phase ends with a commit and is rollback-able via `git revert <sha>` or `git reset --hard <tag>`.

---

## 2.1 — Baseline Checkpoint, Phase 2 Branch, Integrate Phase 1

**Goal**: Tagged starting point for Phase 2 with Phase 1 fully applied. Reproduce the Phase 1 runtime baseline crash (SpriteFont XNB) so the §2.3 fix is verifiable.

**Steps**:
1. Confirm `migration/phase1-x64-monogame` is merged into `migration/monogame_migration` (PR opened post-Phase-1; verify merge before starting).
2. From the integration branch, create `migration/phase2-rendering-content` (or continue on existing `migration/phase2-x64-monogame` if user prefers).
3. `git tag phase2-start`.
4. Build all 5 configs × x64. Confirm 0 errors (Phase 1 build matrix should hold).
5. Launch `game/StarDrive.exe`. Capture `blackbox.log` showing the SpriteFont `Texture2D.ValidateParams` crash. Save as `phase2-baseline.log` in plans dir for diff'ing later.
6. Inventory leftovers: `grep -rn "TODO Phase 2" Ship_Game SDGraphics SDUtils` to surface every Phase 1 TODO marker. Cross-reference with `project_phase2_backlog_runtime.md` memory.

**Verification**: Build matrix green; baseline crash reproduced; TODO markers inventoried.

**Rollback**: `git checkout migration/monogame_migration && git branch -D migration/phase2-rendering-content`.

**Risk**: Low. Pure setup.

---

## 2.2 — MGFX Shader Pipeline; Revert `Shader.FromFile` Null Patch

**Goal**: Compile `Content/Effects/*.fx` to `.mgfx` at build time. Restore `Shader.FromFile` to load a precompiled effect via `ContentManager.Load<Effect>`. Remove the null-shader tolerance from `SpriteRenderer`.

**Steps**:
1. Inventory all `.fx` files: `find game/Content/Effects -name "*.fx"` (typically `Simple.fx`, `Particle.fx`, plus any SunBurn .fx that we don't need).
2. Verify the MonoGame.Content.Builder.Task NuGet package added in §1.7 is wired up in [StarDrive.csproj](StarDrive.csproj). If not, add an `<MonoGameContentReference Include="Content\Content.mgcb" />` plus the .mgcb manifest.
3. Author / generate `Content/Content.mgcb` with one entry per `.fx`, importer `EffectImporter`, processor `EffectProcessor`, profile `Reach` (D3D9 / `_level_9_x`).
4. Update HLSL where XNA 3.1 syntax doesn't survive MGFX:
   - `pixelshader_2_0` / `vertexshader_2_0` → `_level_9_x` (e.g. `compile vs_4_0_level_9_x …`).
   - `texture` declarations may need `sampler2D`+ `Texture2D` split.
   - Inline matrix transposes if MonoGame's column-major D3D11 matrix differs from XNA's expectation.
5. Run a one-off `mgcb -build` to verify all .fx compile. Capture compile log; iterate on HLSL until clean.
6. Restore [SDGraphics/Shaders/Shader.cs](SDGraphics/Shaders/Shader.cs):
   - Replace the Phase-1 `return null` body with `return new Shader(content.Load<Effect>(assetName))`.
   - Drop the `IncludeHandler.Open` body (MGFX handles `#include` at build time, not at load time).
   - Adjust `FromFile` signature if needed: `(GraphicsDevice device, GameContentManager content, string assetName)`.
7. Update call sites: [SDGraphics/Sprites/SpriteRenderer.cs](SDGraphics/Sprites/SpriteRenderer.cs) ctor uses the new signature.
8. Restore [Ship_Game/Data/GameContentManager.cs](Ship_Game/Data/GameContentManager.cs) `LoadEffect` (currently throws NotImplementedException). It should now `Load<Effect>(assetName)` and return.

**Tests added**:
- `UnitTests/Graphics/MGFXShaderLoadTests.cs`: load `Effects/Simple` via `content.Load<Effect>`; assert `CurrentTechnique != null`, `Passes.Count > 0`, parameters `ViewProjection` / `Texture` / `UseTexture` / `Color` are present. One assertion per `.fx` in the inventory. Catches HLSL drift and MGFX silently producing a degenerate effect.

**Verification**:
- `Content/Content.mgcb` builds clean via `mgcb`.
- Build outputs `.xnb` files under `game/Content/Effects/`.
- `Shader.FromFile` returns a non-null Shader at boot.
- `SpriteShader` ctor doesn't NRE on `shader.CurrentTechnique.Passes[0]`.
- Game still crashes — but later (at SpriteFont load), not at SpriteRenderer ctor.
- New shader load tests pass.

**Rollback**: `git revert HEAD`. Phase 1 null-tolerance returns.

**Risk**: Medium. HLSL syntax migration is the variable. Simple.fx is small; SunBurn shaders are excluded so the surface area is bounded.

---

## 2.3 — SpriteFont Rebake + Content-Load Audit

**Goal**: Unblock the §2.1 baseline crash (SpriteFont XNB → `Texture2D.ValidateParams`) by rebaking the 24 fonts in `Content/Fonts/` via MGCB. Audit any other extension-less `content.Load<T>` calls and either (a) add explicit extensions to route through `RawContentLoader`, or (b) include in the MGCB rebake.

**Why this is narrower than originally scoped**: The codebase already has a working non-XNB content path. [Ship_Game/Data/GameContentManager.cs](Ship_Game/Data/GameContentManager.cs) `AssetName` routes by extension — `.xnb`/`.wmv` → XNA `ContentManager` (the broken path); everything else (`.png`/`.dds`/`.fbx`/`.obj`) → [Ship_Game/Data/RawContentLoader.cs](Ship_Game/Data/RawContentLoader.cs) → custom readers via [Ship_Game/Data/Texture/TextureImporter.cs](Ship_Game/Data/Texture/TextureImporter.cs) (PNG via `ImageUtils.LoadPng`, DDS via `Texture2D.FromStream`). The disk inventory confirms `Content/Textures/` is dominated by `.dds`/`.png`; `Content/Fonts/` is the only directory that's exclusively `.xnb` with no source. Effects under `Content/Effects/` have both `.fx` source and pre-baked `.xnb`; §2.2's MGFX work rebakes them from `.fx` — no separate work here.

So Phase 1's §1.10 baseline crash is **specific to SpriteFont** (XNA's `SpriteFontReader` reads an embedded `Texture2D` whose binary layout drifted between XNA 3.1 and MonoGame 3.8). The fix is targeted, not a wholesale content pipeline rebuild.

**Steps**:
1. Inventory the actual XNB-bound surface:
   - `Content/Fonts/*.xnb` — 24 files. No `.spritefont` source on disk. Confirmed crash site.
   - `grep -rn "content\.Load<\|Content\.Load<\|RootContent\.Load<\|TransientContent\.Load<" Ship_Game --include="*.cs"` → identify every call. Mark each as **extension-bearing** (already routed to RawContentLoader, OK) or **extension-less** (XNB path, may break).
   - From the §2.1 inventory, the extension-less Texture/Effect/Model calls of concern are: `Effects/*` (covered by §2.2), `lightRig` (SunBurn, removed in §2.7), `3DParticles/[name]`, `Textures/[path]`, `Effects/NoiseVolume` (Texture3D), `Model/Projectiles/shieldgradient`. For each, verify whether the disk has only `.xnb` or also a `.png`/`.dds`/etc. source. **If a non-XNB source exists, the simpler fix is to add the extension to the load call** — no MGCB rebake needed for that asset.
2. SpriteFont rebake — **synthesize `.spritefont` sources**. The 9 distinct font families in `Content/Fonts/` are: Arial, Tahoma, Pirulen, Visitor, Verdana, Stratum72, Consolas, Corbel, Laserian. For each `Arial10.xnb`/`Arial14Bold.xnb`/etc., create a corresponding `Content/Fonts/Arial10.spritefont` MGCB descriptor:
   ```xml
   <FontName>Arial</FontName>
   <Size>10</Size>
   <Spacing>0</Spacing>
   <UseKerning>true</UseKerning>
   <Style>Regular</Style>
   <CharacterRegions><CharacterRegion><Start>&#32;</Start><End>&#126;</End></CharacterRegion></CharacterRegions>
   ```
   Pirulen / Visitor / Stratum72 / Laserian aren't standard system fonts — locate the original `.ttf` files in repo history or vendor under `Content/Fonts/Sources/`. Extended character regions for non-English locales (the `slavic` branch in [Ship_Game/UI/Fonts.cs](Ship_Game/UI/Fonts.cs) hints Russian/Ukrainian glyphs are needed): expand `CharacterRegions` to cover Cyrillic + Latin Extended.
3. Set up MGCB:
   - Verify the `MonoGame.Content.Builder.Task` NuGet package added in §1.7 is wired up in [StarDrive.csproj](StarDrive.csproj). If not, add `<MonoGameContentReference Include="Content\Content.mgcb" />`.
   - Author `Content/Content.mgcb` with **only the rebake-bound entries** for now: Fonts (and any audit-discovered extension-less Texture loads that genuinely need rebake). Effects come in via §2.2's MGFX target. **Forward-compat**: organize the .mgcb by asset category (Fonts, Effects, [future categories]) using `Build → Folder` groupings rather than per-file entries — Phase 3 additions (HDR cubemaps, normal maps) drop in cleanly.
4. Run `mgcb -build`. Iterate on each font compile failure (most likely: missing TTF source for non-system fonts).
5. Audit-driven fixups — if §2.3 step 1 found extension-less Texture loads where the on-disk file is PNG/DDS, prefer the **add-extension fix** over MGCB rebake. Example: `content.Load<Texture2D>("Textures/comet2")` → if `Content/Textures/comet2.png` exists, change call to `content.Load<Texture2D>("Textures/comet2.png")`. This is a smaller, more targeted change than a content pipeline rebuild.
6. Verify by running the game: SpriteFont load must succeed. The next-downstream crash will likely be `scene_environment` (addressed in 2.7) or a missing asset in the audit list.

**Tests added**:
- `UnitTests/Content/SpriteFontRebakeTests.cs`:
  - `LoadSpriteFont_Arial14Bold`: assert non-null, `LineSpacing > 0`, `MeasureString("test").X > 0`.
  - `LoadAllRebakedFonts`: parametrized over the 24 font names from `Fonts.LoadFonts`. Catches partial rebake regressions where one font compiles but another doesn't.
- `UnitTests/Content/ContentRoutingTests.cs`:
  - `LoadTexture_PNG_RoutesViaRawContentLoader`: load a known PNG asset; assert `Width`/`Height` match source. Documents and locks in the existing PNG fast-path.
  - `LoadTexture_DDS_RoutesViaRawContentLoader`: same for a `.dds`. Documents the existing path and prevents regressions.
  - `AssetName_ExtensionRouting`: unit test for the `AssetName` ctor — ".png"/".dds"/".xnb" produce expected `NonXnaAsset` flag. Pure logic, no GraphicsDevice.

These three tests collectively guard the content pipeline from future drift. Each test uses `TestGameDummy` for the GraphicsDevice where one is needed.

**Verification**:
- `game/Content/Fonts/*.xnb` regenerated by MGCB; file mtimes new; old XNA-format ones replaced (or moved aside under `Content/Fonts.legacy/` for reference).
- Game boots past `Font.ctor` → `content.Load<SpriteFont>("Fonts/Arial20Bold")` without `Texture2D.ValidateParams` exception.
- `Fonts.Arial14Bold.MeasureString("test")` returns a non-zero size at runtime.
- All `SpriteFontRebakeTests` and `ContentRoutingTests` pass.

**Rollback**: `git revert HEAD`. The MGCB project file and `.spritefont` descriptors stay (cheap to keep around for re-attempt); only the new XNBs revert.

**Risk**: Medium (downgraded from High). The bulk of original scope (texture/model/audio rebake) was already handled by RawContentLoader pre-Phase-2. Remaining unknowns: locating original TTF sources for non-system fonts (Pirulen, Visitor, Stratum72, Laserian — likely in repo history or vendored installer assets), and any audit-discovered extension-less loads where no non-XNB source exists. Mitigation: synthesize fonts using closest system-font substitute if originals can't be found (visual fidelity is a Phase 2+ acceptance — main menu legibility is the gate, not exact glyph match).

---

## 2.4 — Restore 2D UI: SpriteRenderer, Fonts, Atlas — Main Menu Milestone

**Goal**: Revert all SpriteRenderer null-tolerance from §1.9. Verify the main menu screen renders with text and buttons. Mouse input over UI elements works.

**Steps**:
1. Revert [SDGraphics/Sprites/SpriteRenderer.cs](SDGraphics/Sprites/SpriteRenderer.cs) Phase-1 patches:
   - ctor: `DefaultEffect = new SpriteShader(simple)` (no null-conditional).
   - `Begin`: `CurrentEffect.SetViewProjection(viewProjection)` (no `?.`).
   - `End`: unconditional `Batcher.DrawBatches(this)`.
   - `ShaderBegin` / `ShaderEnd`: drop the `if (CurrentEffect == null) return;` guards.
2. Verify [Ship_Game/UI/Fonts.cs](Ship_Game/UI/Fonts.cs) `LoadFonts` succeeds (depends on 2.3 SpriteFont rebake).
3. Verify [Ship_Game/SpriteSystem/TextureAtlas.cs](Ship_Game/SpriteSystem/TextureAtlas.cs) loads cached atlases. Atlas creation path (`TextureAtlas.CreateAtlas.cs`) writes `.png`; verify cache HIT path works (DDS read).
4. Boot game; observe MainMenuScreen. Expect: text renders ("New Game", "Load Game", "Mods", etc.), buttons visible, hover/click work.
5. Click "New Game" or "Mods" — navigate to a secondary screen. Confirm no crash on screen transition.
6. Capture screenshot for `phase2-2.4-mainmenu.png`.

**Tests added**:
- `UnitTests/Graphics/SpriteRendererTests.cs`:
  - `RenderTexturedQuad_ProducesExpectedPixels`: render a known texture to a 32×32 `RenderTarget2D` via SpriteRenderer + Simple.fx; read back pixels via `GetData`; assert center pixel matches expected color (within tolerance for premultiplied alpha). **The single most valuable test in Phase 2** — end-to-end smoke for SpriteRenderer + shader + texture binding. Catches broken uniforms, wrong premul, swapped channels, missing texture binds.
  - `RenderColoredFillRect_PixelMatch`: same pattern, using `FillRect` (no texture path).
- `UnitTests/SpriteSystem/TextureAtlasTests.cs`:
  - `Atlas_RoundTrip_HitsCacheOnReload`: generate an atlas from a synthetic source dir, dispose, reload — assert hash match and lookup hits without re-pack. Catches future regressions in the cache hash logic.

**Verification**:
- MainMenu visible with all UI text.
- Mouse hover changes button state (visual feedback).
- Clicking a button triggers a transition without exception.
- `blackbox.log` shows no `Texture2D.ValidateParams`, no NRE in `Font.MeasureString`, no `SpriteRenderer` failures.
- New SpriteRenderer + TextureAtlas tests pass.

**Rollback**: `git revert HEAD`. Reverts the un-stubbing; SpriteRenderer null tolerance returns.

**Risk**: Medium. Most likely failure: a font/texture cache lookup miss surfaces a different broken XNB asset; iterate via 2.3.

---

## 2.5 — VideoPlayer / Media Foundation; Revert `ScreenMediaPlayer` Try/Catch

**Goal**: Splash + cinematic videos play, OR the game proceeds to MainMenu if codec stack isn't available. Phase 1's swallow-everything try/catch is replaced with a single, deliberate startup probe.

**Steps**:
1. Investigate the §1.10 `VideoPlayer.PlatformSetIsLooped()` failure: enable Media Foundation in MonoGame WindowsDX (likely a `MediaFoundation.Startup()` call somewhere in `Program.Main` before `new StarDriveGame()`).
2. Confirm `SharpDX.MediaFoundation.dll` is present in `game/`. Verify codec stack (WMV9 / H.264) on the dev machine. Most Win10/11 installs have it; some N/KN editions don't.
3. Revert [Ship_Game/GameScreens/ScreenMediaPlayer.cs](Ship_Game/GameScreens/ScreenMediaPlayer.cs) ctor try/catch wrapping. Object initializer returns to inline form.
4. Add a single Media Foundation startup probe in `StarDriveGame` or `Program`. If it fails, set a global flag `GlobalStats.VideoDisabled = true`.
5. In `GameLoadingScreen` ctor, gate `LoadingPlayer` / `SplashPlayer` construction on the flag. If disabled, skip splash and jump straight to MainMenu.
6. Verify both paths: with codec stack (splash plays); simulate without (forced flag to true; menu loads instantly).

**Verification**:
- Splash video plays end-to-end on a fresh Win11 dev machine.
- Forcing `VideoDisabled = true` skips splash with no exception.
- No try/catch around property setters; exceptions are deliberate, not swallowed.

**Rollback**: `git revert HEAD`. Phase 1's broad try/catch returns.

**Risk**: Low–Medium. Most likely failure: codec stack difference between machines. Mitigated by graceful skip.

---

## 2.6 — Steam SDK x64 Swap — **DEFERRED**

**Status (2026-05-03)**: Deferred to the final step of the overall migration. See "Deferred Final Step — Steam SDK x64 (Steamworks.NET)" at the bottom of this document.

**Why deferred**: Phase 1 already left SteamManager in a clean graceful-disabled state (`IsInitialized = false`; every public method gates on the flag). The boot log is clean and no functionality elsewhere depends on Steam working. Achievements and cloud-saves stay inactive — acceptable for the migration timeline since this is a single-player game and the Steam features were nice-to-have, not load-bearing.

**Skipping rationale**: doing it now buys nothing the success gate needs and adds NuGet + DLL churn that risks regressing the clean §2.5/§2.6.A boot path. Reviving it later is straightforward (Steamworks.NET is well-documented and the call-site surface is tiny — only 6 SteamManager methods are referenced outside the class).

**Rollback if Steam becomes blocking earlier than planned**: jump straight to the deferred section and execute it in-place; no upstream sub-phase depends on Steam.

---

## 2.6.A — .NET 8 Framework Upgrade; MonoGame 3.8.0.1641 → 3.8.1+; Re-enable VideoPlayer

**Goal**: Move the four csprojs (`SDUtils`, `SDGraphics`, `Ship_Game`, `UnitTests`) off `net48` to `net8.0-windows`. Bump `MonoGame.Framework.WindowsDX` to 3.8.1.303+ (the first release that fixes the WindowsDX `VideoPlayer.Play`/`GetTexture` NullReferenceException bug — empirically verified in §2.5). Drop the polyfill DLLs and MGCB roll-forward hack that the `net48` pin forced on us. Re-enable splash + loading videos.

Inserted after 2.6 in the doc but typically run before it (the version pin to 3.8.0.1641 from §1.7 is the upstream cause of several Phase 2 workarounds; clearing it unblocks future phases). Numbered `2.6.A` so phases 2.7–2.10 don't renumber.

**Why now (and not in Phase 1)**: Phase 1's success gate required net48 to keep XNA-era references compatible during the SDSunBurn / XNAnimation purge. Now that the foundation is x64, MonoGame-only, and SunBurn-stripped, the framework pin is the last remaining XNA-era constraint. Lifting it costs less now (focused, isolated change) than it would after 2.7 (SunBurn replacement) where deeper rendering work would multiply the surface area.

**Steps**:

1. **NuGet inventory + compatibility audit**. Run `dotnet list package --outdated` and `dotnet list package --vulnerable` against the current solution. For each direct dependency, note the highest version that supports `net8.0-windows`:
   - `MonoGame.Framework.WindowsDX` 3.8.0.1641 → 3.8.1.303 (or latest 3.8.x stable).
   - `MonoGame.Content.Builder.Task` 3.8.0.1641 → matching 3.8.1.x.
   - `Newtonsoft.Json`, `NAudio`, `Sentry`, `System.Memory`/`System.Threading.Tasks.Extensions`/`System.Buffers` (probably droppable since net8 has these built-in), `Microsoft.Bcl.AsyncInterfaces`, `System.Text.Json`, `System.Collections.Immutable`, `System.Reflection.Metadata`, `System.Runtime.CompilerServices.Unsafe`, `System.Numerics.Vectors`, `Microsoft.Win32.Registry`, `System.Security.AccessControl`, `System.Security.Principal.Windows`, `System.Text.Encodings.Web`, `System.ValueTuple` — most of these become redundant on net8 and should be dropped.
   - Steamworks.NET / GARSteamManager — verify the wrapper is net8-compatible (probably already is).

2. **csproj migration** for all four projects in parallel (they have to move together — cross-project refs require matching TFMs):
   - `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>` → `<TargetFramework>net8.0-windows</TargetFramework>`.
   - Drop `<TargetFrameworkProfile></TargetFrameworkProfile>` (legacy XNA Client Profile leftover).
   - Drop the redundant `System.*` polyfill `<PackageReference>` entries.
   - If projects are in the old "non-SDK-style" csproj format, this is also a good moment to convert to SDK-style (`<Project Sdk="Microsoft.NET.Sdk">`); much shorter files. Optional but worth the small extra effort while we're here.

3. **MonoGame package bump**: `MonoGame.Framework.WindowsDX` 3.8.0.1641 → 3.8.1.303 (or latest), `MonoGame.Content.Builder.Task` similarly. Verify `mgfxc` still produces compatible `.mgfx` output (Simple.fx and ParticleEffect.fx will need recompile validation — see §2.2 / §2.4).

4. **Drop the MGCB roll-forward hack**: [Directory.Build.props](Directory.Build.props) was added in §2.3 to force `dotnet --roll-forward Major` so `mgcb.dll` (netcoreapp3.1) would load on .NET 8+. With MGCB upgraded to 3.8.1+ targeting net8, the hack is no longer needed. Delete the file (or shrink it to empty if other props get added later).

5. **Drop polyfill DLL deploys** from [.gitignore](.gitignore) lines 53–86 (the `game/System.*.dll` and `game/Microsoft.Bcl.*.dll` entries) and from any csproj `<ItemGroup>` that copies them to output. They'll come back automatically as net8 framework references if anything still uses them; mostly they shouldn't be needed.

6. **Smoke build**: `MSBuild StarDrive.csproj -p:Configuration=Debug -p:Platform=x64`. Expect a wave of "API removed in net8" / "package reference no longer valid" warnings; fix iteratively. Common ones:
   - `BinaryFormatter` is obsoleted in net8 — replace if still used.
   - `Marshal.SizeOf<T>()` etc. minor signature changes.
   - `ServiceProcessInstaller`-style legacy installer types may need a separate net8 NuGet.
   - WinForms / WPF interop — the `net8.0-windows` TFM enables these; should be fine.

7. **Re-enable VideoPlayer in `StarDriveGame.ProbeVideoBackend`**: drop the unconditional `GlobalStats.VideoDisabled = true` line added in §2.5. Keep the construction-and-Volume probe (still useful for codec-missing-on-N-edition cases). Empirically verify on this machine: splash and loading videos play, transition to MainMenu after splash ends.

8. **Re-enable IsLooped** in `ScreenMediaPlayer` ctor / `PlayVideo`: MonoGame 3.8.1+ implements `PlatformSetIsLooped`; restore the setter (with the `looping` ctor parameter that §2.5 dropped, OR keep the parameter dropped if no caller uses it — re-evaluate at the time).

9. **Verify Phase 1.10 smoke test still passes**: process boots, MonoGame `GraphicsDevice` initializes, ScreenManager loads content, MainMenu reaches steady state. No new exceptions in `blackbox.log`.

**Verification**:
- All four projects build clean on `net8.0-windows` x64 Debug.
- Game boots through to MainMenu identically to post-§2.5 state, plus splash now plays.
- VideoPlayer.GetTexture probe (the `videoprobe3.csx` from §2.5) returns valid texture frames on the new MonoGame version (regression test for the upstream fix).
- `Directory.Build.props` deleted; polyfill DLLs no longer in `game/`.
- `blackbox.log` shows the splash playing (no `VideoPlayer force-disabled` warning anymore).

**Rollback**: `git revert HEAD` (single commit ideal; if split into multiple commits — csproj migration, MonoGame bump, video re-enable — revert in reverse order). The §2.5 force-disable should not be reverted; it's a useful sentinel for future codec-missing scenarios.

**Risk**: Medium. Most likely failure modes:
- A NuGet dependency lacks a net8-compatible version → either find an alternative or pin its specific assembly via `<Reference HintPath>`.
- A MonoGame API moved between 3.8.0 and 3.8.1 → low likelihood since 3.8.x is supposed to be source-compatible, but `mgfxc` profile output may differ; recompile shaders.
- Steam SDK wrapper (GARSteamManager.dll) might be net48-only → defer with the `#if STEAM` guard, fix in 2.6 or its own micro-phase.
- Time-to-test multiplied by N projects; budget a session for shakedown.

**Forward-compat**: with net8 in place, future MonoGame upgrades (3.8.2+, 3.9, FNA migration) are mechanical. SDK-style csprojs (if adopted in step 2) make multi-targeting trivial later.

---

## 2.7 — SunBurn Replacement: Forward Renderer; Revert `ScreenManager` Environment Skip

**Status (2026-05-03)**: Executed as **Scope A** (data-carrier upgrade only). The full forward-renderer goal in this section was deliberately split — the real BasicEffect-backed renderer is now part of §2.8 where it has actual draw calls to drive. See "2.7 Scope A — what was actually done" subsection at the end of this section.

**Original Goal (Scope B, deferred to 2.8)**: Replace the `Ship_Game/Data/Mesh/SunBurnStubs.cs` no-op stubs with a working forward renderer based on MonoGame's `BasicEffect`. `ScreenManager.Environment` loads (or constructs) a real environment. 3D scene infrastructure ready for mesh draws in 2.8.

**Steps**:
1. **Architecture decision** (confirm with user before implementing): forward renderer using `BasicEffect`. 3 directional lights max (BasicEffect limit). No shadows. No deferred buffers.
2. Replace [Ship_Game/Data/Mesh/SunBurnStubs.cs](Ship_Game/Data/Mesh/SunBurnStubs.cs) namespace by namespace:
   - **`SynapseGaming.LightingSystem.Core`**:
     - `SceneInterface` becomes a real renderer: owns the GraphicsDeviceManager reference, manages `IObjectManager` + `IRenderManager`, has `BeginFrameRendering(SceneState, SceneEnvironment, deferred:false)` + `Submit(IRenderableEffect)` + `EndFrameRendering()`.
     - `SceneState` carries view + projection matrices; `BeginFrameRendering(ref Matrix view, ref Matrix proj, ...)` populates them.
     - `LightingSystemManager` owns the active set of lights (dir/point/ambient) and applies them to BasicEffect during render.
     - `SceneEnvironment` carries fog, ambient color; remove SunBurn-specific properties.
   - **`SynapseGaming.LightingSystem.Lights`**:
     - `DirectionalLight` / `PointLight` / `AmbientLight` (already stubbed) become real data carriers; the renderer applies them.
     - `LightManager : ILightManager, IManagerService` — add Submit/Remove/Clear that actually push to a list consumed by the renderer.
   - **`SynapseGaming.LightingSystem.Rendering`**:
     - `SceneObject` becomes a transform + mesh + Effect; `WorldBoundingBox` / `ObjectBoundingSphere` are real.
     - `RenderableMesh` carries vertex/index buffers and a primitive draw call that the renderer batches.
   - **`SynapseGaming.LightingSystem.Effects.Forward.LightingEffect`**:
     - Already inherits BasicEffect (Phase 1 stub). Add property forwarding: `DiffuseColor`, `Texture`, `LightingEnabled`, `EnableDefaultLighting`.
3. Restore [Ship_Game/GameScreens/ScreenManager.cs](Ship_Game/GameScreens/ScreenManager.cs) `LoadContent`:
   - Either rebake `Content/example/scene_environment.xnb` *without* the SunBurn `SceneEnvironmentReader_Pro` (use a plain XNA Object reader on a pure-data class), or
   - Fully bypass the XNB load and construct `Environment = new SceneEnvironment { ... }` from code (default ambient, fog, etc.). This is preferred — the asset isn't truly needed when the SunBurn pipeline is gone.
   - Either way, remove the Phase-1 `// TODO Phase 2: ...` comment.
4. Wire `ScreenManager.SceneInter` and `LightSysManager` to the new implementations. Verify the existing call sites (`SceneInter.CreateDefaultManagers(useDeferredRendering:false, usePostProcessing:true)` — note: pass `false` for postProcessing now since we don't have it).
5. Boot game; verify no crashes during ScreenManager init or LoadContent. Main menu should still render (no regression from 2.4).

**Forward-compat hooks for Phase 3**:
- **Pass-based renderer structure**. Even with forward-only rendering in Phase 2, organize `SceneInterface.EndFrameRendering` as a sequence of named passes (`ForwardPass.Execute()`) rather than one inline draw block. Phase 3 will insert `GBufferPass` / `LightingPass` / `ShadowPass` / `PostProcessPass` between BeginFrame and EndFrame — a pass-based architecture absorbs them as additions, not rewrites.
- **Render scene to a `RenderTarget2D`, then blit to backbuffer**. Don't draw directly to backbuffer. Phase 2 cost: ~20 lines (allocate full-size RT in 2.7, blit at EndFrame). Phase 3 benefit: bloom/tone-mapping/HDR all drop into the gap between "scene RT" and "backbuffer blit" without touching scene render code. **Single highest-leverage hook in this phase.**
- **`LightingEffect` stays the public call-site type**. Internally it's BasicEffect-backed in Phase 2; Phase 3 may swap to a custom MGFX effect with shadow map sampling. Keep the inheritance shallow (`LightingEffect : BasicEffect`) — but no call site outside 2.7 should reference `BasicEffect` directly. Mesh code asks for a `LightingEffect`, gets a `LightingEffect`.
- **Preserve `SceneObject.UpdateAnimation(float)` API even as a no-op**. Phase 1 stubs already have it. Keep it through the SunBurnStubs.cs replacement so Phase 3's skinned-animation work (if it happens) is an implementation, not an interface addition.
- **`SceneInterface.CreateDefaultManagers(useDeferredRendering, usePostProcessing)` API surface stays**. Both flags are `false` for Phase 2's call site, but route the boolean through to a strategy selector — don't hardcode forward. Phase 3 flips one flag.

**Tests added**:
- `UnitTests/Graphics/LightManagerTests.cs`:
  - `SubmitDirectionalLight_PropagatesToBasicEffect`: submit a `DirectionalLight` with known direction/color, render with `LightingEffect`, assert `BasicEffect.DirectionalLight0.Direction` matches the submitted value. Pure data-flow assertion, no GPU readback. Catches subtle bugs in the new light → effect parameter binding (the highest-risk area of 2.7).
  - `MultipleDirectionalLights_RespectsMaxOf3`: submit 4 lights, assert only first 3 reach BasicEffect (BasicEffect's hard limit) and the 4th is gracefully dropped/logged.
- `UnitTests/Graphics/SceneInterfaceTests.cs`:
  - `BeginEndFrame_NoSubmits_NoExceptions`: full lifecycle with zero scene objects.
  - `BeginEndFrame_OneObject_DrawCallCountMatches`: submit a stub SceneObject; assert exactly one draw call issued. Use a counting `GraphicsDevice` wrapper or a draw-call observer.

**Verification**:
- `Ship_Game/Data/Mesh/SunBurnStubs.cs` either deleted or shrunk to genuine no-op interfaces only.
- `ScreenManager.LoadContent` no longer has the `// TODO Phase 2` skip comment.
- 3D infrastructure ready: `SceneInter.BeginFrameRendering(...)` runs; `LightingSystemManager` returns the active lights.
- No regressions in 2.4's main-menu milestone.
- New LightManager + SceneInterface tests pass.

**Rollback**: `git revert HEAD`. SunBurn stubs return; ScreenManager env skip returns.

**Risk**: **High**. Largest architectural change in Phase 2. Surface area touches every 3D rendering call site. Mitigation: tackle namespace by namespace, validate after each (does the build still link? does the game still boot to main menu?). Defer the surrounding 2.8 mesh-draw work if 2.7 alone takes more than expected.

### 2.7 Scope A — what was actually done (2026-05-03)

The original §2.7 plan above was scope-split during execution. The honest read on §2.7 vs §2.8: until §2.8 issues actual `device.DrawIndexedPrimitives` calls, there's no observable difference between a "real" SceneInterface/LightingEffect/RenderManager and the no-op stubs. Building a renderer with no draws is wasted speculation. So §2.7 was reduced to the *minimum infrastructure* needed for §2.8 to start consuming, and the renderer itself moved into §2.8.

**What Scope A delivered** (commit pending):

1. ✅ **`ScreenManager.LoadContent`**: removed the `// TODO Phase 2:` skip comment. The `new SceneEnvironment()` construction is now the **chosen permanent solution** (option (b) from the original plan step 3 — "construct from code rather than load XNB"). The old comment's intent was preserved as descriptive prose explaining why the XNB path was abandoned (`SceneEnvironmentReader_Pro` is gone with the SunBurn purge).

2. ✅ **`SceneEnvironment` made real** in [Ship_Game/Data/Mesh/SunBurnStubs.cs](Ship_Game/Data/Mesh/SunBurnStubs.cs): added `AmbientLightColor` (default `0.2, 0.2, 0.2`), `FogEnabled`, `FogColor`, `FogStart` (1000), `FogEnd` (10000). 2.8's BasicEffect parameter binding will read these.

3. ✅ **`LightManager` made real**: backing `List<ILight>` + `IReadOnlyList<ILight> ActiveLights` accessor. `Submit(ILight)` / `Remove(ILight)` / `Clear()` actually mutate. The `Submit(LightRig)` overload stays a no-op because `LightRig` itself is a data-less stub class — nothing to extract until the LightRig content pipeline is revisited (defer to Phase 3).

4. ✅ **`ObjectManager` made real**: backing `List<ISceneObject>` + `IReadOnlyList<ISceneObject> ActiveObjects` accessor. Same pattern as LightManager. 2.8's render loop iterates this list.

5. ✅ **`SceneObject` storage made real**: separate backing lists for `Add(RenderableMesh)` and `Add(ModelMesh, Effect)` calls. `HasMeshes` now returns based on actual count instead of constant `false`. Public read-only accessors (`RenderableMeshes`, `AddedModelMeshes`) for §2.8 to iterate.

**Verification**:
- ✅ Build clean: `dotnet build StarDrive.csproj -c Debug -p:Platform=x64` returned 0 errors (3579 pre-existing CA1416 Color warnings, all unrelated).
- Boot smoke (pending user verification): main menu still renders end-to-end (no behavior change because no consumers are reading the new storage yet — that's §2.8).

**What was deferred to §2.8** (originally planned in this section):
- Pass-based renderer structure with named passes (`ForwardPass.Execute()`)
- Render-to-`RenderTarget2D` then blit to backbuffer (the "highest-leverage hook")
- `LightManager` → `BasicEffect.DirectionalLight0/1/2` parameter binding
- `LightingEffect : BaseMaterialEffect` as the public call-site type with diffuse/texture/lighting forwarding
- `SceneInterface.BeginFrameRendering`/`EndFrameRendering` actually doing work
- `RenderManager.Render()` iterating `ActiveObjects` and issuing draws

**Why this split is the right call**: §2.7 ends at a clean stopping point (build green, no boot regression, infrastructure ready) without sinking days into a renderer that nothing exercises. §2.8 will pick up StaticMesh.Draw + the renderer in one cohesive change where each line of new code is justified by an observable draw on screen.

---

## 2.8 — Mesh Rendering: `StaticMesh` + `RenderableMesh` Restoration

**Status (2026-05-02)**: Closed in three executed slices: **2.8 pre-hardening** (`b2b431537`) hardened the MGFX pipeline + render-loop NRE guards; **2.8 A+B+B4** (`310b79ef0`, `bb3a76ea1`) brought up the forward renderer scaffolding, `RenderManager` loop, and legacy `StaticMesh.Draw` overload dispatch; **2.8.C** (`c8752a311` + 4 hotfixes through `1873d3458`) un-stubbed the OBJ raw-mesh runtime path so planet bodies render with correct sun-direction lighting. The 276 XNB ship/hull/projectile/station/effect Models are a **separate sub-phase 2.8.D, deferred to Phase 3** — stubbed at `GameContentManager.LoadStaticMesh` (returns minimum-viable `StaticMesh(name, unitBounds)`); ships move and fight via the 2D module-overlay tab. Resolution paths in `project_phase2_xnb_model_drift.md`.

**Original Goal**: 3D meshes draw on the screen via the new forward renderer. Ship hulls render in the Ship Design viewport. Planet bodies render in the Universe screen (lit but unshadowed).

**Steps**:
1. Restore [Ship_Game/Data/Mesh/StaticMesh.cs](Ship_Game/Data/Mesh/StaticMesh.cs):
   - `Draw(GraphicsDevice device, in Matrix world, in Matrix view, in Matrix projection)` actually issues `device.DrawIndexedPrimitives` — currently a no-op.
   - Replace SunBurn `BaseMaterialEffect` with the new `LightingEffect : BasicEffect`.
   - Update `CreateSceneObject` to wire the new SceneObject/RenderableMesh from 2.7.
2. Restore [Ship_Game/Universe/SolarBodies/PlanetRenderer.cs](Ship_Game/Universe/SolarBodies/PlanetRenderer.cs) — switch fully to MonoGame's `DirectionalLight` (already aliased in §1.9). Remove `BasicDirectionalLight` references if any remain.
3. Verify model loading for ship hulls and planet meshes (depends on 2.3 model rebake; if deferred, fall back to OBJ via `RawContentLoader`).
4. Boot game; click MainMenu → "Ship Designer". Verify:
   - 3D viewport shows a ship hull
   - Mouse rotation orbits the camera
   - At least one light source illuminates the model (BasicEffect default lighting OK)
5. From MainMenu, attempt "New Game" → race selection → universe load. Universe rendering is a stretch goal (depends on starfield/skybox shaders and planet shaders — defer to Phase 3 if heavy).

**Forward-compat hooks for Phase 3**:
- **`StaticMesh.Draw` takes the effect as a parameter**, not constructed internally. Signature `Draw(GraphicsDevice, in Matrix world, in Matrix view, in Matrix projection, LightingEffect effect)`. Phase 3 will inject a shadow-aware effect or a normal-mapped variant by changing the caller, not the mesh code.
- **Material data on `RenderableMesh` is property-bag friendly**. Diffuse texture is the only field used in Phase 2 — but reserve space (or use a `MaterialOverrides` dict) for normal maps, specular maps, emissive textures. Phase 3 normal-mapping is then an additive change.

**Tests added**:
- `UnitTests/Graphics/ForwardRendererTests.cs`:
  - `RenderUnitCube_ProducesNonClearPixels`: render a hard-coded unit cube to a 64×64 RT with a fixed view/proj/light; read back pixels; assert at least N% are non-clear-color. The "did anything render at all" smoke. Catches degenerate VertexBuffer/IndexBuffer ctors, missing world matrix, broken effect parameter binding — the cheap-but-decisive correctness signal for the new pipeline.
  - `RenderShipHull_FromKnownAsset` (skip if asset rebake in 2.3 didn't cover models): load one rebaked hull mesh, render to RT, assert non-empty silhouette + lit pixels.

**Verification**:
- ShipDesignScreen renders at least one hull.
- No NRE in StaticMesh.Draw or RenderableMesh.
- Lighting is visible (model not pure-flat-color silhouette).
- ForwardRenderer tests pass.

**Rollback**: `git revert HEAD`. Stubs return; nothing renders, but the menu still works.

**Risk**: Medium–High. Depends on 2.7 + 2.3 model rebake. Likely iteration on shader-uniform binding (BasicEffect's parameter expectations vs. SunBurn's `LightingEffect` parameter set).

---

## 2.9 — Particle System + Ship-Design 3D Viewport

**Status (2026-05-02)**: Reduced to **§2.9.A** and shipped (`a6402f716`). Particle audit + `ParticleManagerTests.Reload_PopulatesAllNamedTemplates` regression net pinning all 27 named IParticle templates. Engine trails + weapon-fire particles + explosions verified working in live game (Universe + Combat). The "Ship Design 3D viewport renders fully (mesh + particles)" half is **XNB-gated** — particles work, but ship hulls don't render in 3D until 2.8.D lands. Ships still move/fight via the 2D module-overlay tab.

**Original Goal**: Particle effects (engine trails, weapon fire, impacts) render. The Ship Design viewport renders fully (mesh + particles). This is the cosmetic but highly-visible final touch.

**Steps**:
1. Verify [Ship_Game/Graphics/Particles/ParticleVertexBuffer.cs](Ship_Game/Graphics/Particles/ParticleVertexBuffer.cs) — already API-fixed in §1.8. Test that a `ParticleEmitter` actually emits + draws.
2. Verify [Ship_Game/Graphics/Particles/ParticleEffect.cs](Ship_Game/Graphics/Particles/ParticleEffect.cs) loads its `.fx` (already in 2.2's MGFX scope). If not in 2.2's inventory, add it.
3. Boot to Ship Designer, click on a hull, place an engine module, verify engine trail particles emit when the model is in motion (preview rotation).
4. Boot to Combat Screen test (if reachable without a save game), verify weapon-fire particles render.
5. Audit `Beam.cs` (one of the heaviest API touches in §1.8) — verify beam rendering works post-shader-restore.

**Forward-compat hooks for Phase 3**:
- **Particle render targets carry depth**. When 2.7's "render scene to RT" hook is in place, ensure the scene RT is allocated with `DepthFormat.Depth24Stencil8` (or `Depth16` for parity with current settings). Particles draw into the same RT with depth test enabled. Phase 3 soft particles (depth-aware fade) will sample this depth buffer — if the Phase 2 RT is depth-less, soft particles need a refactor to depth ownership instead of a shader change.

**Verification**:
- Particles visible in Ship Design.
- No NRE in ParticleEmitter.Draw / ParticleEffect.
- Engine trails follow the hull model.

**Rollback**: `git revert HEAD`. Particles return to the inert state.

**Risk**: Medium. Particle system was the most-touched code path in §1.8; high chance of latent bugs in the API migration that only manifest at runtime.

---

## 2.10 — FBX Mesh Import Re-enable; Cleanup; Phase 2 Sign-off

**Status (2026-05-02)**: **Sign-off done; FBX SDK 2018→2020 ABI fix DEFERRED to Phase 3.** [PHASE2_RESULTS.md](PHASE2_RESULTS.md) covers steps 7+ (sub-phase completion table, build matrix, runtime functionality matrix, carryover). Step 1 (FBX re-enable) is the one piece deferred — `NANOMESH_NO_FBX=1` retained on `Debug|x64` and `Release|x64`; 9 asteroid `.fbx` meshes stay unloaded; resolution path in `project_phase2_backlog_fbx.md`. Steps 2 (Color.TransparentBlack sweep) and 3 (remaining `// TODO Phase 2:` markers) handed to Phase 3 cleanup pass.

**Original Goal**: Final cleanup pass. FBX mesh import re-enabled in x64. Color.TransparentBlack swept to Color.Transparent. Phase 2 success gate passes. Tag and document.

**Steps**:
1. Re-enable FBX import in SDNative x64:
   - Apply the FBX SDK 2018→2020 `FbxArray` template ABI fix (per `project_phase2_backlog_fbx.md` memory).
   - Update `MeshImporter.ImportStaticMesh` to call into SDNative's FBX path instead of the Phase-1 stub returning null.
   - Verify with a test import of one .fbx asset.
   - **Test added**: `UnitTests/Data/MeshImporterTests.cs`:
     - `ImportFbx_KnownMesh_NonZeroVertexCount`: import a small reference .fbx checked into the test fixtures; assert vertex count, index count, and bounding box match expected values. Guards the §1.4 FBX SDK ABI fix from future regression — same risk class as the original 2018→2020 drift documented in `project_phase2_backlog_fbx.md`.
2. Sweep `Color.TransparentBlack` → `Color.Transparent` across ~40 call sites (mostly in `Ship_Game/UI/`, `Ship_Game/Menu*.cs`). Use a regex replace.
3. Audit all remaining `// TODO Phase 2:` markers. Either resolve or push to Phase 3 with a `// TODO Phase 3:` retag.
4. Run unit tests: `UnitTests/SDUnitTests`. Audit failures. Many will be content-loading-related; document, fix where trivial, push others to Phase 3 backlog.
5. Build all 5 configs × x64. Confirm 0 errors, no platform/processorArchitecture warnings, ideally 0 deprecation warnings.
6. Run game ≥ 5 minutes: navigate MainMenu → ShipDesigner → back → Race Design → MainMenu → exit via Esc. No crashes, no exceptions in `blackbox.log`.
7. Document Phase 2 outcome in `PHASE2_RESULTS.md` (mirror PHASE1_RESULTS.md structure):
   - Sub-phase completion table
   - Build matrix outcomes
   - Runtime functionality matrix (what works, what's stubbed, what's broken)
   - Phase 3 backlog
8. Update memory:
   - Add `project_phase3_backlog.md` with deferred items
   - Archive `project_phase2_backlog_runtime.md` (mark as resolved) or update its status notes
9. `git tag phase2-complete`.
10. Open PR `migration/phase2-rendering-content` → `migration/monogame_migration`.

**Verification**: All Phase 2 success gate criteria met. Unit tests build (passing rate documented). Game runs ≥ 5 minutes interactively without crash.

**Rollback**: Per-sub-phase revert preferred. `git reset --hard phase2-start` if a deep rollback is needed.

**Risk**: Medium. FBX re-enable is the big variable; everything else is mechanical cleanup.

---

## Critical Files (Quick Reference)

### Phase 1 leftovers (revert during Phase 2)
- [SDGraphics/Shaders/Shader.cs](SDGraphics/Shaders/Shader.cs) — null FromFile (revert 2.2)
- [SDGraphics/Sprites/SpriteRenderer.cs](SDGraphics/Sprites/SpriteRenderer.cs) — null tolerance (revert 2.4)
- [Ship_Game/GameScreens/ScreenManager.cs](Ship_Game/GameScreens/ScreenManager.cs) — Environment skip (revert 2.7)
- [Ship_Game/GameScreens/ScreenMediaPlayer.cs](Ship_Game/GameScreens/ScreenMediaPlayer.cs) — try/catch (revert 2.5)

### Stubs to flesh out
- [Ship_Game/Data/Mesh/SunBurnStubs.cs](Ship_Game/Data/Mesh/SunBurnStubs.cs) — entire SunBurn replacement (2.7)
- [Ship_Game/Data/Mesh/StaticMesh.cs](Ship_Game/Data/Mesh/StaticMesh.cs) — Draw is no-op (2.8)
- [Ship_Game/Data/Mesh/MeshImporter.cs](Ship_Game/Data/Mesh/MeshImporter.cs) — returns null (2.10)
- [Ship_Game/Data/Mesh/MeshExporter.cs](Ship_Game/Data/Mesh/MeshExporter.cs) — returns false (2.10)
- [Ship_Game/Data/GameContentManager.cs](Ship_Game/Data/GameContentManager.cs) — LoadEffect throws (2.2)

### Content pipeline
- `game/Content/Content.mgcb` (new in 2.3) — MGCB project manifest
- `game/Content/Effects/*.fx` — HLSL sources (2.2)
- `game/Content/Fonts/*.spritefont` — font sources, may need synthesis (2.3)
- [Ship_Game/Data/RawContentLoader.cs](Ship_Game/Data/RawContentLoader.cs) — secondary content path; validate against MonoGame GraphicsDevice

### Renderer surface
- [Ship_Game/Graphics/DeferredRenderer.cs](Ship_Game/Graphics/DeferredRenderer.cs) — Phase 3
- [Ship_Game/Graphics/BloomComponent.cs](Ship_Game/Graphics/BloomComponent.cs) — Phase 3
- [Ship_Game/Graphics/Particles/](Ship_Game/Graphics/Particles/) — particle pipeline (2.9)
- [Ship_Game/Beam.cs](Ship_Game/Beam.cs) — heavy API touches in §1.8; verify in 2.9

### External
- Steamworks SDK x64 (2.6) — `steam_api64.dll`
- MonoGame.Effect.Compiler (2.2) — MGFX tool
- MonoGame.Content.Builder.Task (2.3) — already added in §1.7

---

## Verification Strategy (End-to-End)

After each sub-phase:
1. `git status` — confirm only intended files modified.
2. Build solution: `msbuild StarDrive.sln /p:Configuration=Debug /p:Platform=x64`. Capture log.
3. Quick boot test: launch `game/StarDrive.exe`, observe blackbox.log behavior, exit. Document the new "deepest reachable point" in `phase2-progress.log`.
4. Commit with descriptive message referencing sub-phase number.

Final Phase 2 verification (after 2.10):
1. Clean build of `Debug|x64`, `Release|x64`, `Deploy|x64`. Zero errors. Ideally zero deprecation warnings.
2. Run `game/StarDrive.exe` from a clean shell.
3. Confirm 64-bit process via Task Manager.
4. **Splash plays or skips gracefully** → MainMenu visible with text.
5. **Navigate**: MainMenu → ShipDesign → back → RaceDesign → back → exit via Esc.
6. **3D viewport**: ShipDesign hull renders; engine trails visible on a hulled engine.
7. Confirm clean exit code 0; `blackbox.log` shows no fatal exceptions.
8. Run `UnitTests/SDUnitTests` — capture pass/fail rates; document in PHASE2_RESULTS.md.
9. Tag `phase2-complete`.

---

## Open Items / Phase 3 Preview

Items intentionally deferred to Phase 3 (Polish / Advanced Rendering):

### Rendering
- **DeferredRenderer port to MonoGame `RenderTarget2D`** — current Phase 2 uses forward rendering only.
- **Shadow maps** — BasicEffect has no shadow support. Need custom shaders + cascaded shadow map pipeline.
- **BloomComponent + post-processing** — disabled in Phase 2.
- **HDR / tone mapping** — not present in original SunBurn build, optional Phase 3 enhancement.
- **Skybox / starfield** — Universe screen depth and parallax stars.
- **Atmospheric scattering** — Planet shaders.
- **Beam / weapon FX** — beyond simple particles; lasers, tractor beams, explosion shockwaves.

### Content
- **`RawContentLoader` validation** against MonoGame `GraphicsDevice` — cross-checks the non-XNB content path.
- **Mod content routing** — verify mod assets still override vanilla after the MGCB rebake.
- **Save/load round-trip** with the rebaked content baseline.

### Animation
- **Skinned animation replacement** — XNAnimation/SgMotion equivalent. Only needed if a use case surfaces (none in Ship_Game/**, just in the test fixture).

### Testing
- **Full SDUnitTests pass rate** — Phase 2 may leave content-dependent tests failing. Phase 3 audits and fixes or marks as expected-fail.
- **Performance profiling** — frame time, draw call count, GC pressure on the new pipeline.

### Cleanup carryovers
- **God-class refactors** — `Fleet.cs` (2,882 LOC), `Empire.cs` (2,778), `ColonyScreen.cs` (2,017) are listed in ARCHITECTURE.md §8 as tech debt. Out of migration scope; track separately.
- **`.NET Framework 4.8 → modern .NET`** — explicit Phase 1 decision was to defer to "Phase 4+". Still deferred.

---

*This document should be updated as Phase 2 sub-phases complete; mirror the structure of [migration-plan-phase1.md](migration-plan-phase1.md).*

---

## Deferred Final Step — Steam SDK x64 (Steamworks.NET)

**Lowest priority of the entire migration.** Originally scoped as §2.6, deferred on 2026-05-03 because the current graceful-disabled state (Phase 1 stub in [SteamManager.Initialize](Ship_Game/Utils/SteamManager.cs) returning `false`) is good enough to ship the migration through Phase 2 → 3 → 4. Revive at the very end, after every other migration goal is met.

**Goal when revived**: `SteamAPI.Init()` returns true on a logged-in Steam client; achievements, stats, cloud-saves, and overlay work; no `0x8007000B BadImageFormat` in `blackbox.log`.

**Chosen approach: Full Steamworks.NET migration** (NOT rebuilding GARSteamManager — there's no source in the repo for the original native wrapper, and Steamworks.NET is the canonical C# binding).

**Why Steamworks.NET wins over the alternatives**:
- Native x64 binaries (`steam_api64.dll`) shipped with the package
- Maintained, MIT-licensed, ~10k stars, widely used in Unity/MonoGame projects
- Direct equivalents for every API GARSteamManager exposes (and many more)
- NuGet install — no SDK download/extract dance
- Tiny external call-site surface in this codebase (6 methods used outside `SteamManager.cs`: `Initialize`, `IsInitialized`, `RequestStats`, `AchievementUnlocked`, `ActivateWebOverlay`, `Shutdown` — see grep results from 2026-05-03 session)

**Steps when revived**:

1. **NuGet add** `Steamworks.NET` to `Ship_Game.csproj` (latest stable; verify the version targets net8.0 or netstandard2.x).
2. **Drop the x86 binaries** from `game/`:
   - `game/steam_api.dll` (105KB, x86 — confirmed PE header `Intel i386` on 2026-05-03)
   - `game/GARSteamManager.dll` (x86, no source)
   The Steamworks.NET package will deploy `steam_api64.dll` automatically via its `.targets` file.
3. **Confirm `game/steam_appid.txt`** still contains `220680` (StarDrive's Steam app ID — present as of 2026-05-03). Required for non-launcher debugging; ignored when launched via Steam client.
4. **Rewrite [Ship_Game/Utils/SteamManager.cs](Ship_Game/Utils/SteamManager.cs)** internals — keep the public API identical (callers stay untouched). Mapping:
   - `SteamInitialize()` → `SteamAPI.Init()`
   - `SteamShutdown()` → `SteamAPI.Shutdown()`
   - `RequestCurrentStats()` → `SteamUserStats.RequestCurrentStats()`
   - `SaveAllStatAndAchievementChanges()` → `SteamUserStats.StoreStats()`
   - `SetAchievement(name)` → `SteamUserStats.SetAchievement(name)`
   - `GetAchievement(name, ref achieved)` → `SteamUserStats.GetAchievement(name, out achieved)`
   - `SetStatINT/FLOAT(name, val)` → `SteamUserStats.SetStat(name, val)`
   - `GetStatINT/FLOAT(name)` → `SteamUserStats.GetStat(name, out val)`
   - `GetSteamID()` → `SteamUser.GetSteamID().m_SteamID`
   - `GetSteamName()` → `SteamFriends.GetPersonaName()`
   - `IsOverlayEnabled()` → `SteamUtils.IsOverlayEnabled()`
   - `ActivateOverlayWebPage(url)` → `SteamFriends.ActivateGameOverlayToWebPage(url)`
   - `ActivateOverlay*` (Achievements/Community/Friends/etc.) → `SteamFriends.ActivateGameOverlay("achievements"|"community"|...)`
   - `FileExists/GetFileSize/GetFileOnRemoteStorage/SaveFileOnRemoteStorage` → `SteamRemoteStorage.FileExists/GetFileSize/FileRead/FileWrite`
5. **Add `SteamAPI.RunCallbacks()` to the main game loop** — Steamworks.NET requires periodic callback dispatch (~60Hz is fine; once per `Update` tick is standard).
6. **Re-enable `Initialize()`**: drop the Phase-1 stub body (`IsInitialized = false; Log.Info("disabled")`); replace with `IsInitialized = SteamAPI.Init();` plus a try/catch around `SteamAPI.RestartAppIfNecessary(220680)` for the launcher-relaunch dance.
7. **Verify on a Steam-running dev machine**:
   - `SteamAPI.Init()` returns true
   - `SteamFriends.GetPersonaName()` returns the local user's display name
   - `AchievementUnlocked("Thanks")` actually fires (visible in Steam client → Achievements view)
   - Shift+Tab opens Steam overlay in-game
   - `ActivateWebOverlay("https://steamcommunity.com")` opens the overlay browser, not the system default
8. **Verify graceful disable when Steam isn't running**: `SteamAPI.Init()` returns false; every public method short-circuits; no NRE; no crash.
9. **Update [.gitignore](.gitignore)** to add the Steamworks.NET deployed runtime artifacts (`game/steam_api64.dll`, `game/Steamworks.NET.dll`, etc.) under the existing "MonoGame / SharpDX runtime DLLs" comment block.
10. **Update memory**: mark `project_phase2_backlog_runtime.md` priority #4 as ✅ DONE with commit reference; remove the deferred-final-step pointer from this section.

**Verification**:
- `blackbox.log` shows `SteamAPI.Init() = true` (or graceful false with no exception).
- Test achievement unlocks visible in Steam client.
- Cloud-save round-trip works (write file via SteamRemoteStorage; verify in Steam → User Data on the cloud side).
- All `SteamManager` external callers (`StarDriveGame.cs`, `Log.cs`) unchanged in source — proves the API-surface preservation.

**Rollback**: `git revert HEAD`. Returns to Phase 1's graceful-disabled state. Zero risk to other migration work since no caller path changed.

**Risk**: Low–Medium when actually executed. Steamworks.NET is mechanical; the only real variables are (a) RunCallbacks loop integration and (b) confirming the deployed `steam_api64.dll` lands in `game/` correctly across `Debug|x64` / `Release|x64` / `Deploy|x64` configs.

**Estimated effort**: 1–2 focused hours plus a Steam-client boot test.
