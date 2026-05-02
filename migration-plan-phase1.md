# Phase 1 Migration Plan — x86/XNA → x64/MonoGame Foundation

## Context

The existing [ARCHITECTURE.md](ARCHITECTURE.md) outlines a 4-phase migration roadmap from XNA 3.1 + SunBurn (32-bit) to MonoGame (64-bit). Phase 1 ("Foundation") was sketched as four bullets:

> 1a. Change platform target to x64 / AnyCPU
> 1b. Rebuild SDNative.dll as x64
> 1c. Replace Microsoft.Xna.Framework.Game with MonoGame Game class
> 1d. Verify game starts with MonoGame, even if rendering is broken

This document expands those bullets into 10 sequenced, individually-committable sub-phases. Each sub-phase has a clear goal, ordered steps, verification criteria, rollback path, and risk rating.

The intended outcome of Phase 1 is a **64-bit MonoGame StarDrive process that boots, ticks the game loop, accepts input, and exits cleanly**. 3D rendering and SunBurn-driven scene rendering are **explicitly broken / stubbed** in Phase 1 — that's Phase 2's job.

## Confirmed Strategic Decisions

| Decision | Choice | Rationale |
|---|---|---|
| MonoGame variant | **WindowsDX** (D3D11) | Closest to XNA 3.1 / D3D9 semantics. Keeps HLSL `.fx` workflow viable for Phase 2. |
| SDSunBurn handling | **Exclude entirely from solution** | Stub all SunBurn call sites with TODO Phase 2 markers. Cleanest cut. |
| .NET target framework | **Stay on .NET Framework 4.8** | Avoid stacking two large migrations. Defer .NET modernization to Phase 4+. |
| FBX SDK | **Vendor FBX SDK 2020 x64** | Preserve mesh import capability; fix any API drift in SDNative/SdMesh/. |
| Custom XNA wrapper | **Delete `Microsoft.Xna.Framework.Game` project entirely** | 4181 LOC of decompiled XNA. MonoGame ships all of it. Keeping as shim adds maintenance burden. |
| Pack=4 fix | **Remove `Pack=4` from C# pointer-containing structs** | Default natural alignment matches C++ side. Pragma pack on C++ side would freeze a broken layout. |
| Output directory | **Keep `game/`** | Avoid breaking Content/, Mods/, installers, log paths. |
| XNAnimation | **Drop entirely in Phase 1** | Skeletal animation isn't used in `Ship_Game/**`; only in test fixture. Replace later if needed. |
| MonoGame version | **3.8.1.303** (latest stable) | Battle-tested, supports `net48`. |

## Phase 1 Success Gate

The user runs `game/StarDrive.exe`. The process:

1. Loads as a 64-bit process (Task Manager — no `*32` suffix).
2. Opens a window at the configured resolution.
3. Game loop ticks for ≥30 seconds without crash, AV, `BadImageFormatException`, missing-DLL error, or unhandled `NullReferenceException`.
4. Keyboard input registers (Esc closes the window cleanly via `Game.Exit()`).
5. Window may be black, may show a stub overlay, or may show partial menu — all acceptable.
6. `blackbox.log` is created. Process exits with code 0. No native heap corruption on shutdown.

**Anti-goals**: 3D rendering working, main menu fully functional, SunBurn scenes loading, audio working, content loading working. All those are Phase 2+.

---

## Sub-phase Index

| # | Title | Risk |
|---|---|---|
| 1.1 | Baseline checkpoint and migration branch | Low |
| 1.2 | Solution / C# project platform reconfig (x86 → x64) | Low–Medium |
| 1.3 | Add x64 configuration to SDNative.vcxproj | Medium |
| 1.4 | SDNative third-party x64 dependencies (FBX SDK 2020, libpng, zlib) | **High** |
| 1.5 | Remove `Pack=4` from pointer-containing C# structs | Low |
| 1.6 | Remove XNA 3.1 references; delete custom wrapper project | Medium |
| 1.7 | Add MonoGame.Framework.WindowsDX NuGet packages | Low–Medium |
| 1.8 | Code fixes for MonoGame API drift | Medium |
| 1.9 | Exclude SDSunBurn; stub call sites | Medium–High |
| 1.10 | Final cleanup, runtime verification, Phase 1 sign-off | Low–Medium |

Each sub-phase ends with a commit and is rollback-able via `git revert <sha>` or `git reset --hard <tag>`.

---

## 1.1 — Baseline Checkpoint and Migration Branch

**Goal**: Tagged starting point. Current x86/XNA build provably working before any change.

**Steps**:
1. `git status` clean. Stash or commit any in-progress work.
2. `git checkout -b migration/phase1-x64-monogame`.
3. `git tag pre-migration-x86`.
4. Build solution as `Debug|x86` in VS2022 (toolset v143). Capture build log.
5. Launch `game/StarDrive.exe`, confirm current expected state (e.g., main menu reaches). Capture screenshot/log.

**Verification**: Build succeeds 0 errors. Game runs at known-good state.

**Rollback**: `git checkout main && git branch -D migration/phase1-x64-monogame`.

---

## 1.2 — Solution / C# Project Platform Reconfig (x86 → x64)

**Goal**: Solution exposes x64 platforms. All C# projects target x64. SDNative still Win32 (next sub-phase). Build will be partially broken — intentional.

**Steps**:
1. In [StarDrive.sln](StarDrive.sln), replace every `|x86` with `|x64` in:
   - `SolutionConfigurationPlatforms` section (lines 48–54)
   - `ProjectConfigurationPlatforms` section (lines 55–139)
   - Keep all 5 config names: `Debug`, `Debug - Auto Fast`, `Deploy`, `Release`, `Release - Auto Fast`.
2. In [StarDrive.csproj](StarDrive.csproj), for each of the 5 platform-conditional `<PropertyGroup>` blocks (Debug|x86, Release|x86, Deploy|x86, Debug-AutoFast|x86, Release-AutoFast|x86 at lines 38–118):
   - Change condition platform `x86` → `x64`
   - `<PlatformTarget>x86</PlatformTarget>` → `<PlatformTarget>x64</PlatformTarget>`
   - `<Prefer32Bit>true</Prefer32Bit>` → `<Prefer32Bit>false</Prefer32Bit>` (or remove)
   - Remove `<LargeAddressAware>true</LargeAddressAware>` (line 36) — meaningless on x64
   - Default `<Platform>` line 7: `x86` → `x64`
3. Repeat platform/Prefer32Bit/LargeAddressAware swap in:
   - [SDGraphics/SDGraphics.csproj](SDGraphics/SDGraphics.csproj)
   - [SDUtils/SDUtils.csproj](SDUtils/SDUtils.csproj)
   - [SynapseGaming-SunBurn-Pro/SDSunBurn.csproj](SynapseGaming-SunBurn-Pro/SDSunBurn.csproj) (excluded in 1.9, but stay consistent)
   - [UnitTests/SDUnitTests.csproj](UnitTests/SDUnitTests.csproj)
   - [Microsoft.Xna.Framework.Game/Microsoft.Xna.Framework.Game.csproj](Microsoft.Xna.Framework.Game/Microsoft.Xna.Framework.Game.csproj) (deleted in 1.6, but keep solution loadable now)
4. Do NOT remove XNA references yet — that's 1.6. Keep this commit narrow.

**Verification**:
- Solution loads in VS2022 without errors.
- `Debug|x64` platform appears in config dropdown.
- Grep `*.csproj` for `Prefer32Bit>true` and `LargeAddressAware>true` — should be 0 hits.
- C# projects compile (XNA references resolve at compile time even with `processorArchitecture=x86` HintPath).

**Rollback**: `git revert HEAD`.

---

## 1.3 — Add x64 Configuration to SDNative.vcxproj

**Goal**: SDNative compiles for x64. Linking fails on third-party libs (1.4 fixes).

**Steps**:
1. In [SDNative/SDNative.vcxproj](SDNative/SDNative.vcxproj):
   - Duplicate `<ProjectConfiguration Include="Debug|Win32">` and `Release|Win32` blocks (lines 4–11) to add `Debug|x64` and `Release|x64` versions.
   - Duplicate every `<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|Win32'">` and equivalent `Release|Win32` blocks (lines 20–33) for x64.
   - Duplicate `<ItemDefinitionGroup>` blocks with `<ClCompile>`/`<Link>` settings for x64.
   - Replace hardcoded `Win32` references in `<OutDir>` / `<IntDir>` with `$(Platform)` macro where present.
2. Same operation in [SDNative/SDNativeTests/SDNativeTests.vcxproj](SDNative/SDNativeTests/SDNativeTests.vcxproj).
3. C++ source code itself needs no edit. `int` for sizes (`SlabAllocator.h`, `image_utils.cpp`) is acceptable for Phase 1.

**Verification**: VS Build of SDNative for `Debug|x64` reaches the link step. Compile succeeds. Linker emits `LNK1112` for FBX/libpng/zlib (machine type mismatch) — expected.

**Rollback**: `git revert HEAD`.

---

## 1.4 — SDNative x64 Third-Party Dependencies

**Goal**: SDNative.dll links and produces a valid x64 binary in `game/SDNative.dll`.

**Steps**:
1. **FBX SDK 2020 x64**:
   - Download Autodesk FBX SDK 2020.x for VS2022 from Autodesk developer portal.
   - Vendor `libfbxsdk.lib` and `libfbxsdk.dll` (x64) under `SDNative/3rdparty/fbxsdk/x64/`.
   - Update vcxproj `<AdditionalLibraryDirectories>` for `Debug|x64` and `Release|x64` to point at x64 lib path.
   - Update post-build copy step (line 84/109 in vcxproj) to copy x64 `libfbxsdk.dll` to `game/`.
2. **libpng + zlib x64**:
   - **Preferred**: use vcpkg (`vcpkg install libpng:x64-windows-static-md zlib:x64-windows-static-md`) and integrate via `vcpkg.targets`.
   - **Alternative**: build static libs manually with existing CMake scripts; place under `3rdparty/libpng/x64/` and `3rdparty/zlib/x64/`.
   - Confirm same MSVC runtime (`/MD` or `/MT`) as SDNative.
3. Verify FBX SDK 2020 API hasn't drifted in ways that break `SDNative/SdMesh/`. If it has:
   - **Escape hatch**: temporarily exclude FBX-using files from x64 build via `<ClCompile><ExcludedFromBuild Condition="'$(Platform)'=='x64'">true</ExcludedFromBuild></ClCompile>`. Mesh import broken in Phase 1; rest of SDNative ships.
   - Document API drift items as Phase 2 backlog in `PHASE1_RESULTS.md`.
4. Build, confirm `game/SDNative.dll` is x64: `dumpbin /headers SDNative.dll | findstr machine` should show `x64`.

**Verification**: Both Debug|x64 and Release|x64 of SDNative build clean. SDNativeTests.exe builds and runs.

**Rollback**: Tag `phase1.4-pre` before this commit. `git revert HEAD` covers tracked changes; vendored binaries also tracked. If FBX 2020 vendoring fails entirely, fall back to escape hatch.

**Critical risk**: HIGH. Most likely sub-phase to derail timeline. Mitigation: vcpkg for libpng/zlib (5 min) instead of manual builds; FBX SDK 2020 escape hatch documented.

---

## 1.5 — Remove `Pack=4` from Pointer-Containing C# Structs

**Goal**: C# ↔ C++ struct interop correct on x64. Independent commit; doesn't depend on MonoGame.

**Steps**:
1. [Ship_Game/Data/Mesh/MeshInterface.cs](Ship_Game/Data/Mesh/MeshInterface.cs): remove `Pack = 4` from:
   - `SdMesh` (line 33)
   - `SdMaterial` (line 46)
   - `SdMeshGroup` (line 119)
   - `SdVertexData` (line 73)
   - `SdVertexElement` (line 63)
   - Replace each with `[StructLayout(LayoutKind.Sequential)]`.
2. [Ship_Game/ExtensionMethods/SDNative.cs](Ship_Game/ExtensionMethods/SDNative.cs): remove `Pack = 4` from `CStrView` (line 10).
3. [Ship_Game/Ships/ShipData_LegacyParser.cs](Ship_Game/Ships/ShipData_LegacyParser.cs) and [Ship_Game/Ships/Legacy/LegacyShipData_LegacyParser.cs](Ship_Game/Ships/Legacy/LegacyShipData_LegacyParser.cs): audit each `[StructLayout]` attribute. Remove `Pack=4` from any struct containing pointers OR `CStrView`. Leave value-only structs alone.
4. [Ship_Game/Ships/ShipDesignWriter.cs](Ship_Game/Ships/ShipDesignWriter.cs): same audit.
5. **Leave [Ship_Game/Spatial/SpatialObj.cs](Ship_Game/Spatial/SpatialObj.cs) alone** — pointer-free, performance-sensitive, `Pack=4` is fine.
6. C++ side already uses natural alignment (verified: no `#pragma pack` in `ReCpp/src/rpp/strview.h` or `SDNative/SdMesh/*.h`).

**Verification**: SDNativeTests passes (round-trip serialization tests). Smoke test: `string → CStrView → string` round-trips correctly.

**Rollback**: `git revert HEAD`.

---

## 1.6 — Remove XNA 3.1 References; Delete Custom Wrapper Project

**Goal**: No references to XNA 3.1. `Microsoft.Xna.Framework.Game` project gone. Build broken until 1.7+1.8 — sequence carefully.

**Steps**:
1. In [StarDrive.csproj](StarDrive.csproj):
   - Remove `<Reference Include="Microsoft.Xna.Framework, Version=3.1.0.0, ..." processorArchitecture=x86 ...>` block (lines 154–158).
   - Remove `<ProjectReference Include="..\Microsoft.Xna.Framework.Game\Microsoft.Xna.Framework.Game.csproj" />`.
2. Repeat the XNA reference removal in [SDGraphics/SDGraphics.csproj](SDGraphics/SDGraphics.csproj) (line 38) and [UnitTests/SDUnitTests.csproj](UnitTests/SDUnitTests.csproj) (line 47).
3. In [StarDrive.sln](StarDrive.sln):
   - Remove `Project(...)` block for `Microsoft.Xna.Framework.Game` and its config mapping section.
   - Remove any `ProjectDependencies` references to GUID `{DD32A813-1393-421A-BAF8-455F02167BE3}`.
4. In [app.config](app.config), remove the `<dependentAssembly>` block for `Microsoft.Xna.Framework.Game` redirect (around line 54).
5. Delete `Microsoft.Xna.Framework.Game/` directory tree on disk.
6. Remove from `game/`: `Microsoft.Xna.Framework.dll`, `Microsoft.Xna.Framework.Game.dll`, `Microsoft.Xna.Framework.Game.pdb`, `Microsoft.Xna.Framework.RuntimeProfile`, `XnaNative.dll`, `d3d9.dll`, `d3dx9_31.dll`.
7. `git clean -fdx` on `obj/` and `bin/` trees to clear stale references.

**Verification**:
- Solution loads. `Microsoft.Xna.Framework.Game` no longer in solution explorer.
- Build fails consistently with "namespace `Microsoft.Xna.Framework` not found" errors. EXPECTED.
- `app.config` has no XNA Game redirect.

**Rollback**: `git revert HEAD` restores deleted files. Test rollback before proceeding to 1.7.

---

## 1.7 — Add MonoGame.Framework.WindowsDX NuGet Packages

**Goal**: MonoGame referenced. Build now reports MonoGame API-drift errors instead of "missing namespace" errors.

**Steps**:
1. In [StarDrive.csproj](StarDrive.csproj), add (using modern `<PackageReference>` style):
   ```xml
   <PackageReference Include="MonoGame.Framework.WindowsDX" Version="3.8.1.303" />
   <PackageReference Include="MonoGame.Content.Builder.Task" Version="3.8.1.303" />
   ```
2. Repeat in [SDGraphics/SDGraphics.csproj](SDGraphics/SDGraphics.csproj) (it has implicit converters to/from XNA `Vector2/3`/`Matrix`).
3. Repeat in [UnitTests/SDUnitTests.csproj](UnitTests/SDUnitTests.csproj).
4. Do NOT add to `SDSunBurn.csproj` — excluded next sub-phase.
5. NuGet restore: `dotnet restore` or VS-driven restore.
6. Confirm `MonoGame.Framework.dll` ends up in `bin/` for managed projects.
7. Keep target framework at `net48` (from line 10 of StarDrive.csproj). MonoGame 3.8.1.303 supports `net48`.

**Verification**:
- NuGet restore succeeds.
- `MonoGame.Framework.WindowsDX` 3.8.1.x in `packages/` or `~/.nuget/packages`.
- Build error count finite (hundreds, not thousands).
- All errors trace to MonoGame API drift, not missing references.

**Rollback**: `git revert HEAD`.

---

## 1.8 — Code Fixes for MonoGame API Drift

**Goal**: All `Ship_Game/**`, `SDGraphics/**`, `StarDrive` code compiles against MonoGame. SDSunBurn still has errors (excluded next).

This is the largest sub-phase by code volume. Mostly mechanical search-and-replace. Consider splitting into 1.8a / 1.8b / 1.8c commits if patches get unwieldy.

**Steps** (ordered smallest blast radius first):

1. **`FrameworkDispatcher.Update()` removal**: only call site was in deleted wrapper. Grep `Ship_Game/**/*.cs` to confirm zero remaining.
2. **GamerServices/Guide**: confirmed unused; no action.
3. **`MultiSampleType` enum → `int`**: in [Ship_Game/GameScreens/GameBase.cs](Ship_Game/GameScreens/GameBase.cs) lines 77, 82, 86, 87. Replace `MultiSampleType.None` → `0`. Remove `MultiSampleQuality` references.
4. **`DisplayMode.Format` removed**: in [Ship_Game/GameScreens/MainMenu/OptionsScreen.cs](Ship_Game/GameScreens/MainMenu/OptionsScreen.cs) line 306 and [Ship_Game/Graphics/RenderTargets.cs](Ship_Game/Graphics/RenderTargets.cs) line 24. Replace with `SurfaceFormat.Color`. Remove format selector from OptionsScreen UI.
5. **`GraphicsAdapter.CheckDeviceFormat / CheckDeviceMultiSampleType / CheckDepthStencilMatch` removed**: in [Ship_Game/Graphics/RenderTargets.cs](Ship_Game/Graphics/RenderTargets.cs) and [GameBase.cs](Ship_Game/GameScreens/GameBase.cs) line 78. Stub each with `return true;` + `// TODO Phase 2: capability check`.
6. **`PreparingDeviceSettings` event** in [GameBase.cs](Ship_Game/GameScreens/GameBase.cs) line 69: delete the event subscription and `PrepareDeviceSettings` method (lines 72–91). Move the four important lines into the GameBase constructor: `Graphics.PreferMultiSampling = true; Graphics.GraphicsProfile = GraphicsProfile.HiDef; Graphics.ApplyChanges();`
7. **`ShaderProfile` enum + `MinimumPixelShaderProfile` / `MinimumVertexShaderProfile`** in [GameBase.cs](Ship_Game/GameScreens/GameBase.cs) lines 63–64: replace with `Graphics.GraphicsProfile = GraphicsProfile.HiDef;`.
8. **`DeviceType.Hardware`** in [GameBase.cs](Ship_Game/GameScreens/GameBase.cs) line 78: removed in MonoGame. Drops out when stubbing step 5.
9. **`DepthStencilBuffer` class** in [Ship_Game/Graphics/BloomComponent.cs](Ship_Game/Graphics/BloomComponent.cs) (lines 38, 47):
   - Replace `new DepthStencilBuffer(device, w, h, DepthFormat.Depth24Stencil8, MultiSampleType.None, 0)` with `new RenderTarget2D(device, w, h, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8)`.
   - Phase 1 simplest path: stub Bloom entirely (`Bloom = null`) and let it be off. Phase 2 will restore bloom properly.
10. **SDGraphics implicit converters**: [SDGraphics/Vector2.cs](SDGraphics/Vector2.cs), `Vector3.cs`, `Matrix.cs` define `implicit operator XnaVector2`. The `Microsoft.Xna.Framework.Vector2` namespace exists in MonoGame too — these should still resolve. No code edits expected.
11. **Other Tier-2 errors** (`SpriteBatch.Begin(SaveStateMode)` removed, etc.): address iteratively as build errors surface. Allow 30–60 min for `build → fix top error → repeat` loop.
12. **[Ship_Game/Data/GameContentManager.cs](Ship_Game/Data/GameContentManager.cs)**: heavy SunBurn imports. Stub SunBurn-specific content readers; throw `NotImplementedException` from any custom `ContentTypeReader<T>` for SunBurn types, with `// TODO Phase 2` markers.

**Verification**: `Ship_Game`, `SDGraphics`, `SDUtils`, `StarDrive` all build clean. `SDSunBurn` still fails (next sub-phase).

**Rollback**: `git revert HEAD` (or per-commit if split).

---

## 1.9 — Exclude SDSunBurn; Stub Call Sites

**Goal**: Full solution compiles. SunBurn-coupled rendering paths are stubs with `// TODO Phase 2` markers.

**Steps**:
1. In [StarDrive.sln](StarDrive.sln), remove `Project(...)` block for `SDSunBurn` and its config mappings.
2. In [StarDrive.csproj](StarDrive.csproj), remove `<ProjectReference Include="..\SynapseGaming-SunBurn-Pro\SDSunBurn.csproj" />`.
3. In [app.config](app.config), remove `SynapseGaming-SunBurn-Pro` `<dependentAssembly>` block.
4. Keep `SynapseGaming-SunBurn-Pro/` directory on disk — Phase 2 may resurrect a small subset.
5. Stub all SunBurn references in [Ship_Game/GameScreens/ScreenManager.cs](Ship_Game/GameScreens/ScreenManager.cs) (~14 references):
   - `SceneInterface SceneInter` → `object SceneInter = null;`
   - `LightingSystemManager LightSysManager;` → null
   - `SceneEnvironment Environment;` → null
   - `SceneState GameSceneState;` → null
   - Constructor (lines 73–77): comment out, log "Phase 1: SunBurn disabled"
   - `SceneInter.ApplyPreferences(...)` (line 110): no-op + TODO
   - `SceneInter.RenderManager.Render()` (line 322): no-op + TODO
   - Remove `using SynapseGaming.LightingSystem.*;` lines (11–13)
6. Stub `LightingSystemPreferences Preferences;` in [GameBase.cs](Ship_Game/GameScreens/GameBase.cs); remove `using SynapseGaming.LightingSystem.Core;`.
7. **Triage 30 files with `using SynapseGaming`**:
   - Heavy users (`UniverseScreen.cs`, `Ship.cs`, `SolarSystemBody.cs`, `Mesh*.cs`, `SceneObj.cs`, `SceneInstance.cs`): stub SunBurn calls inline with TODO markers.
   - Light users: remove `using SynapseGaming` and let unresolved-type errors guide each fix.
   - Create [Ship_Game/Data/Mesh/SunBurnStubs.cs](Ship_Game/Data/Mesh/SunBurnStubs.cs) with minimal stub types: `public class SceneObject {}`, `public interface IRenderableMesh {}`, etc. Each stub ≤ 5 lines, marked `// TODO Phase 2: replace with MonoGame-native equivalent`.
8. **Mesh import path** ([Ship_Game/Data/Mesh/MeshImporter.cs](Ship_Game/Data/Mesh/MeshImporter.cs), `MeshExporter.cs`, `StaticMesh.cs`): stub to compile. Return null/empty results at runtime with log "Phase 1 stub: mesh import disabled".
9. Defensive null guards: every stubbed call site gets `if (SceneInter == null) { Log.Info("Phase 1: SunBurn stubbed, skipping"); return; }`. Removed in Phase 2.
10. Remove `SDSunBurn.dll` and `.pdb` from `game/`.

**Verification**: Full solution build, all 5 configurations × `x64`, 0 errors. `StarDrive.exe` produced in `game/`.

**Rollback**: `git revert HEAD`. SunBurn project files NOT deleted on disk, just unhidden by revert.

**Risk**: Volume of stub work + risk of `NullReferenceException` at runtime when stubbed `null SceneInter` is dereferenced. Mitigation: defensive null guards everywhere, removed in Phase 2.

---

## 1.10 — Final Cleanup, Runtime Verification, Phase 1 Sign-Off

**Goal**: Phase 1 success gate passes.

**Steps**:
1. In [StarDrive.csproj](StarDrive.csproj), remove `<Reference Include="XNAnimation" ... processorArchitecture=x86>` (lines 149–153).
2. In [UnitTests/StarDriveTestContext.cs](UnitTests/StarDriveTestContext.cs), remove XNAnimation reference / using.
3. Delete `game/XNAnimation.dll`.
4. Audit `game/` directory:
   - **Should NOT contain**: `Microsoft.Xna.Framework.dll`, `Microsoft.Xna.Framework.Game.dll`, `XnaNative.dll`, `d3d9.dll`, `d3dx9_31.dll`, `XNAnimation.dll`, `SDSunBurn.dll`, `DistortionPipeline.dll`.
   - **Should contain**: `MonoGame.Framework.dll` and its native deps (D3DCompiler, etc.) — restored by NuGet via MonoGame's MSBuild targets.
5. Audit [app.config](app.config): no SunBurn or XNA Framework Game binding redirects. Keep System.* redirects (Memory, Buffers, etc.).
6. Build all 5 configurations × `x64`. Confirm no platform/processorArchitecture warnings.
7. Run `game/StarDrive.exe`:
   - Process is x64 in Task Manager (no `*32` suffix).
   - Window opens.
   - `blackbox.log` is created with boot messages.
   - Game loop runs (verify by adding temporary `Log.Info($"Frame {FrameId}")` in `GameBase.Update()`; remove before final commit).
   - Press Esc → window closes, exit code 0.
   - No unhandled exception popups, no `BadImageFormatException`, no missing-DLL OS dialog.
8. Black/garbled menu is acceptable — Phase 1 success gate.
9. Document outcome in `PHASE1_RESULTS.md`: what works, what's broken, Phase 2 backlog items discovered.
10. `git tag phase1-complete`.

**Verification**: All success gate criteria met. Game runs ≥ 30 seconds, exits cleanly.

**Rollback**: `git reset --hard pre-migration-x86` returns to original state. Per-sub-phase revert preferred.

---

## Critical Files (Quick Reference)

### Configuration
- [StarDrive.sln](StarDrive.sln) — solution platforms
- [StarDrive.csproj](StarDrive.csproj) — main project, XNA refs, platform target
- [app.config](app.config) — binding redirects
- [SDNative/SDNative.vcxproj](SDNative/SDNative.vcxproj) — native build config

### Game core (touched in 1.8, 1.9)
- [Ship_Game/GameScreens/GameBase.cs](Ship_Game/GameScreens/GameBase.cs) — XNA wrapper subclass, device settings
- [Ship_Game/GameScreens/StarDriveGame.cs](Ship_Game/GameScreens/StarDriveGame.cs) — main game class
- [Ship_Game/GameScreens/ScreenManager.cs](Ship_Game/GameScreens/ScreenManager.cs) — SunBurn-heavy
- [Ship_Game/Graphics/RenderTargets.cs](Ship_Game/Graphics/RenderTargets.cs) — XNA capability checks
- [Ship_Game/Graphics/BloomComponent.cs](Ship_Game/Graphics/BloomComponent.cs) — DepthStencilBuffer

### Interop (touched in 1.5)
- [Ship_Game/Data/Mesh/MeshInterface.cs](Ship_Game/Data/Mesh/MeshInterface.cs) — 5 structs to fix
- [Ship_Game/ExtensionMethods/SDNative.cs](Ship_Game/ExtensionMethods/SDNative.cs) — `CStrView`
- [Ship_Game/Ships/ShipData_LegacyParser.cs](Ship_Game/Ships/ShipData_LegacyParser.cs)

### To delete
- [Microsoft.Xna.Framework.Game/](Microsoft.Xna.Framework.Game/) — entire wrapper project (1.6)

### To exclude (keep on disk)
- [SynapseGaming-SunBurn-Pro/](SynapseGaming-SunBurn-Pro/) — excluded from solution in 1.9

---

## Verification Strategy (End-to-End)

After each sub-phase:
1. `git status` — confirm only intended files modified.
2. Build solution: `msbuild StarDrive.sln /p:Configuration=Debug /p:Platform=x64`.
3. Commit with descriptive message referencing sub-phase number.

Final Phase 1 verification (after 1.10):
1. Clean build of `Debug|x64` and `Release|x64`. Zero errors.
2. Run `game/StarDrive.exe` from a clean shell.
3. Confirm 64-bit process via Task Manager.
4. Confirm window opens, ticks for ≥ 30 seconds.
5. Confirm Esc exits cleanly with code 0.
6. Inspect `blackbox.log` — main loop entries present, no fatal exceptions.
7. Run SDNativeTests.exe (if it builds — exempted by 1.4 escape hatch is acceptable).
8. Tag `phase1-complete`.

---

## Open Items / Phase 2 Preview

Items intentionally deferred to Phase 2:
- 3D rendering pipeline (replace SunBurn with custom MGFX shaders or community MonoGame deferred renderer)
- Mesh rendering via MonoGame `Effect` instead of SunBurn Effects
- DeferredRenderer rewrite using `RenderTarget2D`
- Bloom + post-processing pipeline restoration
- Skeletal animation (XNAnimation replacement) if needed
- XNB content compatibility audit (some XNB files may need MGCB recompilation)
- `RawContentLoader` validation against MonoGame `GraphicsDevice`
- Particle system vertex buffer compatibility check
- Mod content routing validation