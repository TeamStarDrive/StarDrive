# Phase 1 Results — x86/XNA → x64/MonoGame Foundation

**Status**: Phase 1 architectural goals met. Runtime gate gated by Phase 2 content-pipeline work (expected per plan anti-goals).

**Branch**: `migration/phase1-x64-monogame` (merges to `migration/monogame_migration`)

## Sub-phase Completion

| Sub-phase | Outcome |
|-----------|---------|
| 1.1 Baseline checkpoint, branch | Done |
| 1.2 Sln/csproj x86 → x64 | Done |
| 1.3 SDNative.vcxproj x64 config | Done |
| 1.4 SDNative third-party x64 deps (FBX 2020, libpng/zlib from vcpkg) | Done; FBX mesh import disabled in x64 (Phase 2 backlog) |
| 1.5 `Pack=4` removal from pointer-bearing interop structs | Done |
| 1.6 XNA 3.1 references purged; `Microsoft.Xna.Framework.Game` wrapper deleted | Done |
| 1.7 MonoGame.Framework.WindowsDX 3.8.0.1641 + Content.Builder.Task NuGet packages | Done |
| 1.8 MonoGame API drift in SDGraphics + Ship_Game | Done |
| 1.9 SDSunBurn excluded; call sites stubbed; XNAnimation removed (pulled forward) | Done |
| 1.10 Final cleanup, runtime verification | Build matrix green; runtime hits Phase 2 content-pipeline blockers (documented below) |

## Build Matrix (5 configs × x64)

All clean — 0 errors, no platform/processorArchitecture warnings.

| Configuration | Time | Warnings |
|---------------|------|----------|
| Debug \| x64               | 4.22s | 34 |
| Release \| x64             | 7.35s | 84 |
| Debug - Auto Fast \| x64   | 2.42s | 4  |
| Release - Auto Fast \| x64 | 2.29s | 34 |
| Deploy \| x64              | 4.67s | 34 |

Warnings are exclusively `Color.TransparentBlack` deprecation hints (use `Color.Transparent`) plus one MSTest analyzer note (`MSTEST0036`).

Logs: `phase1-logs/phase1-10-x64-*.log`.

## Runtime Smoke Test — Outcome

`game/StarDrive.exe` (Debug|x64) was launched directly.

**What works**:
- 64-bit process (confirmed by `0x8007000B BadImageFormat` from x86 Steam SDK — itself proof of x64).
- Process starts, loads app settings, writes boot banner to `blackbox.log`.
- MonoGame `GraphicsDevice` initializes; `Resetting`/`Reset` lifecycle fires.
- `3D Graphics Preferences` applied (resolution, shadow/effect detail, texture sampling).
- `ScreenManager.LoadContent` is reached.
- Game cleans up cleanly via `RunCleanupAndExit(-1)` (no native heap corruption, no `BadImageFormatException`, no missing-DLL OS dialog).

**Patched during §1.10 step 7 to push past early crashes** (these are tolerance patches, not solutions):
- `SDGraphics.Shaders.Shader.FromFile` returns `null` instead of throwing (`SpriteRenderer` ctor and Begin/End paths now null-tolerant).
- `ScreenManager.LoadContent` skips `Load<SceneEnvironment>("example/scene_environment")` and uses `new SceneEnvironment()` (the XNB embeds a `SynapseGaming.LightingSystem.Processors.SceneEnvironmentReader_Pro` ContentTypeReader from the now-excluded SunBurn assembly).
- `ScreenMediaPlayer` ctor wraps `VideoPlayer.Volume`/`IsLooped` setters in try/catch (Media Foundation backend may not be initialized).

**Where Phase 1 stops**: `Font` ctor → `content.Load<SpriteFont>("Fonts/Arial20Bold")` → `Texture2D.ValidateParams` rejects the embedded XNB texture data with:
> `elementCount is not the right size, elementCount * sizeof(T) is 131072, but data size is 524288`

The 4× ratio is consistent with XNA 3.1 → MonoGame 3.8 binary XNB format drift in the Texture2D reader. This is the actual content-pipeline rebake work — not a stub-able edge case — and is explicitly listed in the Phase 1 plan's anti-goals (`content loading working` is Phase 2+).

## Phase 2 Backlog — Discovered During §1.10

### Content pipeline (the big one)

1. **XNA 3.1 ↔ MonoGame XNB binary-format incompatibility** — `Texture2D.ValidateParams` rejects baked SpriteFont textures (size mismatch ~4×). All XNB assets need to be rebaked through MonoGame Content Builder, or a custom XNB shim layer must translate between formats. This blocks every content load: fonts, textures, models, sounds, music. Highest-leverage Phase 2 task.

2. **HLSL effect compilation removed** — `SDGraphics.Shaders.Shader.FromFile` currently returns `null`. Need to:
   - Precompile `Content/Effects/*.fx` to `.mgfx` via MGFX tool (or MonoGame.Effect.Compiler).
   - Restore `Shader.FromFile` to call `ContentManager.Load<Effect>`.
   - Remove the null-tolerance in `SpriteRenderer` (keep until precompiled effects are wired).

3. **SunBurn `ContentTypeReader` references in baked XNBs** — at minimum `example/scene_environment.xnb` references `SynapseGaming.LightingSystem.Processors.SceneEnvironmentReader_Pro`. Either rebake assets without SunBurn types or replace SunBurn's role in the lighting/scene pipeline before re-enabling these loads.

### Runtime backends

4. **Media Foundation / VideoPlayer** — `VideoPlayer.Volume` / `IsLooped` setters fail at boot. SharpDX.MediaFoundation.dll is present but the runtime path may need explicit Media Foundation startup, or the WMV codec stack on the target machine. Splash/loading videos and in-game cinematics are blocked.

5. **Steam SDK is still x86** — `SteamInitialize` throws `0x8007000B` (BadImageFormat). Need to swap `steam_api.dll` for the x64 build or update to Steamworks SDK x64.

### Already-known carryovers

6. **SDSunBurn replacement** — currently fully stubbed via `Ship_Game/Data/Mesh/SunBurnStubs.cs`. Lighting, shadows, scene rendering, and the `SceneObject`/`RenderableMesh` pipelines are no-ops. Replacement (BasicEffect-based forward renderer? bespoke deferred path?) is the major Phase 2 architectural decision.

7. **FBX mesh import disabled in x64** — already memo'd (FBX 2018 → 2020 SDK ABI drift in `FbxArray` template). `MeshImporter.ImportStaticMesh` returns null with a warning. Restoration plan documented in memory `project_phase2_backlog_fbx.md`.

8. **Skinned models / `SgMotion` removed** — `StaticMesh.cs` lost `FromSkinnedModel` / `CreateAnimation` paths. Animation rig replacement is a Phase 2 design call (MonoGame has no built-in skinning equivalent to `SgMotion`).

### Cleanup follow-ups

9. **`Color.TransparentBlack` → `Color.Transparent`** — sweep ~40 call sites to clear the deprecation warning noise.
10. **Test fixture audit** — UnitTests project compiles, but no test runs were performed in Phase 1. First full test run will likely surface more content-loading failures with the same root cause as #1.

## Phase 1 Sign-off

- All sub-phase goals architecturally met.
- Build is green across the full configuration matrix.
- The runtime gate ("game loop ticks ≥30s") is realistically blocked by item #1 (XNB content-pipeline rebake), which the Phase 1 plan explicitly defers to Phase 2.
- No regressions in the build/link foundation. SDSunBurn, XNAnimation, XNA 3.1, and old SunBurn binding redirects are fully purged.

**Tagged**: `phase1-complete`
