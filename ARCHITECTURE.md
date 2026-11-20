# StarDrive BlackBoxPlus — Architecture Reference

> **Purpose**: Architecture reference for engineers and modders who are new to this codebase.
> **Scope**: The code as it is on the Jupiter 1.60 line (64-bit, .NET 8, MonoGame). The pre-migration
> snapshot (x86, XNA 3.1, SunBurn) is kept in [ARCHITECTURE_pre_64bit.md](ARCHITECTURE_pre_64bit.md).
> **Last updated**: 2026-11-18
> **Paths**: game-code paths are relative to `Ship_Game/`; a project's own row or section uses paths inside
> that project; content files such as `Effects/X.fx` are relative to `game/Content/`; repo-level files and
> folders (`StarDrive.sln`, `SDNative/`, `UnitTests/`, ...) are named from the repository root.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Solution Structure](#2-solution-structure)
3. [Entry Points and Startup](#3-entry-points-and-startup)
4. [Game Loop and Threads](#4-game-loop-and-threads)
5. [Systems Reference](#5-systems-reference)
   - [5.1 Rendering](#51-rendering)
   - [5.2 Content Loading and Mod Routing](#52-content-loading-and-mod-routing)
   - [5.3 UI](#53-ui)
   - [5.4 Localization and the In-Game Codex](#54-localization-and-the-in-game-codex)
   - [5.5 Audio and Video](#55-audio-and-video)
   - [5.6 Input](#56-input)
   - [5.7 Saves and Settings](#57-saves-and-settings)
   - [5.8 Modding](#58-modding)
   - [5.9 AI Layers](#59-ai-layers)
   - [5.10 Networking, Updates, Logging and Error Reporting](#510-networking-updates-logging-and-error-reporting)
6. [Native Code (SDNative)](#6-native-code-sdnative)
7. [Third-Party Libraries and Tools](#7-third-party-libraries-and-tools)
8. [Build, Test and Release](#8-build-test-and-release)
9. [Technical Debt](#9-technical-debt)
10. [Migration Outcome](#10-migration-outcome)

---

## 1. Project Overview

StarDrive is a space 4X game released in 2013. BlackBox is a community rewrite of the decompiled game,
made with the original developer's permission (see `README.md`).

- The game is a 64-bit Windows program on .NET 8 (`net8.0-windows`, `x64`).
- MonoGame (WindowsDX, DirectX 11 backend) provides the window, device, content and math types.
  MonoGame keeps the `Microsoft.Xna.Framework` namespace, so that name in a `using` line means MonoGame.
- A native C++ DLL, `SDNative.dll`, handles mesh import, PNG I/O, spatial queries and a few hot paths.
- The 3D renderer is a forward renderer written for this project. It still uses SunBurn's type and
  namespace names (`SynapseGaming.LightingSystem.*`) so older call sites compile (section 5.1).
- The game is single-player. The only network traffic is update checks and downloads (GitHub, or a mod's
  Bitbucket page) and error reports (Sentry).
- The current release line is codenamed Jupiter (1.60). The Mars line (1.51) lives on the `mars-1.51` branch.

---

## 2. Solution Structure

**Solution file**: `StarDrive.sln`
**Runtime**: .NET 8 (`net8.0-windows`) for all C# game projects; `SDAnalyzers` targets `netstandard2.0`
**Platform**: every solution configuration is `x64`

### Projects

| Project | File | Type | Purpose |
|---|---|---|---|
| `StarDrive` | `StarDrive.csproj` | C# exe (`WinExe`; `Exe` in `Debug` and `Debug - Auto`) | The game. Root namespace `Ship_Game`, output `game\StarDrive.exe` |
| `SDGraphics` | `SDGraphics/SDGraphics.csproj` | C# library | Math types (`Vector2/3/4`, `Vector2d/3d`, `Matrix`, `RectF`, `Range`), sprite batching (`Sprites/`), `Shaders/Shader.cs`, input enums |
| `SDUtils` | `SDUtils/SDUtils.csproj` | C# library | Collections (`Array`, `Map`, `BitArray`), `Dir`, `Mem`, string and file helpers. No MonoGame reference |
| `SDAnalyzers` | `SDAnalyzers/SDAnalyzers.csproj` | Roslyn analyzer | Rule `SD0001`: fails the build on non-premultiplied `new Color(..., a < 255)` literals |
| `SDNative` | `SDNative/SDNative.vcxproj` | C++ DLL | Mesh I/O (NanoMesh + FBX SDK), image I/O, spatial structures, ship design writer |
| `SDUnitTests` | `UnitTests/SDUnitTests.csproj` | MSTest library | C# unit and integration tests; references `StarDrive.csproj` |
| `SDNativeTests` | `SDNative/SDNativeTests/SDNativeTests.vcxproj` | C++ console app | Native spatial tests with an ImGui debug window |
| `SDInstaller` | `Deploy/SDInstaller.wixproj` | WiX | MSI installer. In the solution but not built by any configuration |

Each C# project except `SDAnalyzers` lists its source files explicitly (`EnableDefaultCompileItems` is false). A new `.cs` file
needs a `<Compile Include="..." />` entry. `StarDrive.csproj` and `SDUnitTests.csproj` alias
`Range` to `SDGraphics.Range` globally, so `System.Range` slice syntax is not used.

### Top-Level Folder Map

```
BlackBoxPlus/
├── Ship_Game/              # Game code, about 900 .cs files (as of 2026-11-18)
│   ├── AI/                 # Empire AI, Goal base class, military tasks, threat matrix, ship AI, combat tactics
│   ├── Audio/              # GameAudio and the NAudio backend
│   ├── Codex/              # In-game Codex screen, entries, tooltip hooks
│   ├── Commands/Goals/     # Concrete Goal state machines (build, colonize, defend, pirates, remnants, ...)
│   ├── Data/               # ResourceManager, GameContentManager, serializers, YAML, mesh/texture I/O, GameText
│   ├── Debug/              # Debug overlay screen and its pages
│   ├── Empires/ Espionage/ Fleets/ Troops/
│   ├── GameScreens/        # Program, StarDriveGame, GameBase, ScreenManager, GameScreen and all screens
│   ├── Gameplay/           # Weapons, projectiles, relationships, asteroids
│   ├── Graphics/           # Post-process, shadow map, particles, render targets and states
│   ├── Input/              # InputState, key handling
│   ├── PathFinder/         # Unused A* code (see section 9)
│   ├── Ships/              # Ship (partial files), ShipModule, ShipDesign, ShipHull, legacy parsers
│   ├── Spatial/            # SpatialManager, NativeSpatial bridge, managed Qtree
│   ├── SpriteSystem/       # Texture atlases and atlas cache
│   ├── StoryAndEvents/     # Events and notifications
│   ├── Threading/          # Parallel: custom thread pool
│   ├── Tools/Localization/ # Localization tool behind the --run-localizer CLI flag
│   ├── UI/                 # UI element framework and YAML layouts
│   ├── Universe/           # UniverseScreen (partial files), UniverseState, solar systems, planets
│   └── Utils/              # Log, SafeQueue, PerfTimer, FrameTimeLogger, SteamManager stub, ...
├── SDGraphics/  SDUtils/  SDAnalyzers/
├── SDNative/               # Native DLL source; NanoMesh/ and ReCpp/ are git submodules
├── UnitTests/              # SDUnitTests.csproj
├── Deploy/                 # MakeInstaller.py, NSIS scripts, WiX project, vendored NSIS and 7-Zip
├── x64Migration/           # Migration plans, results, limitations, re-export runbook
├── docs/                   # Design notes and plans (for example docs/codex-plan.md)
├── .github/workflows/      # patch-build.yml, notify-release.yml
├── Properties/             # AssemblyInfo.cs: game version
└── game/                   # Build output and the game's working directory
    ├── Content/            # Vanilla content
    ├── Mods/               # ExampleMod only; Combined Arms and Star Trek are separate repositories
    └── LegacyMesh/         # Archived XNA 3.1 model XNBs; not loaded by the game
```

### Build Configurations

| Configuration | Defines | Notes |
|---|---|---|
| `Debug` | `DEBUG;TRACE` | Console subsystem, so the log console shows |
| `Release` | (optimized) | Warnings are errors (`StarDrive`, `SDGraphics`, `SDUtils`, `SDUnitTests`). This is what CI builds |
| `Debug - Auto` / `Release - Auto` | `AUTO` | The AI plays for the player (`GlobalStats.RestrictAIPlayerInteraction` returns true) at normal speed |
| `Debug - Auto Fast` / `Release - Auto Fast` | `AUTOFAST` | As `AUTO`, and the sim speed ramps up automatically (`GlobalStats.AutoRampGameSpeed`) |
| `Deploy` | (optimized) | After build, runs `Deploy/MakeInstaller.py --major --type=nsis` (target `MakeMajorInstaller`) |

C++ projects map every solution configuration to their own `Debug|x64` or `Release|x64`.

---

## 3. Entry Points and Startup

**Main entry point**: `Ship_Game/GameScreens/Program.cs` → `Program.Main()`

```
Program.Main()
  culture = InvariantCulture (all threads)
  EnsureNativeDependenciesLoadable()   # NativeLibrary.Load("SDNative.dll"); message box + exit if blocked
  GlobalStats.LoadConfig()             # must run before any Log call
  Log.Initialize(enableSentry: true)   # blackbox.log, async log thread, Sentry
  ParseMainArgs(args)                  # CLI flags below
  new StarDriveGame()                  # GC latency mode, AppData folders, AutoPatcher cleanup, IsFixedTimeStep
  game.OnInitialize = RunInitializationTasks   # CLI tools (export textures/meshes, localizer)
  game.Run()                           # MonoGame game loop
```

`Main` also catches the `EntryPointNotFoundException` that MonoGame throws on Windows 10 builds older
than 1803 and shows a "Windows version too old" message.

### Game Class Hierarchy

```
StarDriveGame   [Ship_Game/GameScreens/StarDriveGame.cs]
  └─ GameBase   [Ship_Game/GameScreens/GameBase.cs]   # GraphicsDeviceManager, window modes, ScreenManager
       └─ Microsoft.Xna.Framework.Game                 # MonoGame's Game class
```

`GameDummy` (`Ship_Game/GameScreens/GameDummy.cs`) is a second `GameBase` subclass used by the tests.

### StarDriveGame.Initialize() and LoadContent()

1. `ResourceManager.InitContentDir()`, `ScreenManager = new(this, Graphics)`, `InitializeAudio()`
   (reads `game/Content/Audio/AudioConfig.yaml`).
2. `ApplyGraphics(GraphicsSettings.FromGlobalStats())`: windowed, borderless or fullscreen, with a
   borderless fallback when DXGI refuses fullscreen. Fullscreen is a borderless window covering the
   display, with a backbuffer of the display's size. A lower resolution is drawn into
   `GameBase.ScreenTarget`, and each frame that picture is stretched over the display, keeping its shape.
   `GameBase.ScreenWidth`/`ScreenHeight` are the size the game draws at. The mouse is mapped with
   `WindowToScreen` and `ScreenToWindow`.
3. `ProbeVideoBackend()` sets `GlobalStats.VideoDisabled` if Media Foundation is missing.
4. `OnInitialize()` runs CLI tasks and can exit before the window is used.
5. `LoadContent()` adds `GameLoadingScreen`. It plays the splash video while `ResourceManager.LoadItAll`
   runs on a pool thread, then switches to `MainMenuScreen`.

### CLI Arguments

`--help`, `--mod="<name>"`, `--export-textures`, `--export-meshes=obj|fbx|fbx+obj`, `--generate-hulls`,
`--generate-ships`, `--fix-roles`, `--run-localizer=[0-2]`, `--merge-translations`, `--resource-debug`,
`--asset-debug`, `--console`, `--continue`. `--apply-patch=<version>` is internal: the AutoPatcher passes
it when it relaunches the game elevated.

---

## 4. Game Loop and Threads

### Main Loop

`StarDriveGame` sets `IsFixedTimeStep = true` and does not change `TargetElapsedTime`, so MonoGame's
default fixed step applies. Each tick:

```
StarDriveGame.Update()
  GameAudio.Update()
  GameBase.UpdateGame()            # skipped while the device is lost or the game is crashing
    ScreenManager.Update()
      hot-load checks, input.Update(), add pending screens, F1 opens the Codex
      HandleInput: the topmost screen (section 5.6)
      Update: screens back to front
  exit when no screens remain
StarDriveGame.Draw()
  DrawToScreen()                   # binds GameBase.ScreenTarget, or the backbuffer when there is none
  ScreenManager.Draw()             # visible screens back to front, then tooltips and cursor
  StretchScreenTarget()            # draws ScreenTarget over the display, if there is one
  SetRenderTarget(null)            # always, so Present never runs with a render target bound
```

### Threads

| Thread | Created by | Runs |
|---|---|---|
| `Main Thread` (UI thread) | Process start; named in `Program.Main` | MonoGame loop: input, screen `Update`, all drawing. `GameBase.MainThreadId` |
| `Universe.SimThread` | `UniverseScreen.CreateUniverseSimThread()` | The universe simulation (`UniverseSimulationLoop`) |
| Pool workers | `Ship_Game/Threading/Parallel.cs` | `Parallel.For` / `Parallel.Run` work: ship, projectile and AI updates, loading, saving, downloads |
| `AsyncLogWriter` | `Log.Initialize` | Writes `blackbox.log` |

`Parallel` is a custom pool that grows on demand. `Parallel.For` splits a range over at most
`Parallel.MaxParallelism` workers: the physical core count (`GetPhysicalCPUCoreCount` in `SDNative.dll`),
capped by the user setting `GlobalStats.MaxParallelism` (0 or less means no cap). If a worker thread
cannot start (low memory), the task runs inline instead.

### The Simulation Thread

`UniverseScreen` owns the simulation (`Universe/UniverseScreen/UniverseScreen.UpdateGame.cs`). The handshake with the UI thread:

1. `UniverseScreen.Draw` finishes, calls `AdvanceSimulationTargetTime` (real time × game speed, held back
   when the sim lags more than 1 second) and sets `DrawCompletedEvt`.
2. The sim thread wakes, then runs fixed steps until it reaches the target time, at most `30 × GameSpeed`
   steps per wake. A step is `1 / CurrentSimFPS` game seconds, shorter below 1x game speed.
   `CurrentSimFPS` is `GlobalStats.SimulationFramesPerSecond`, lowered in steps of 5 (not below 10) while
   turns run slow and raised back when they catch up (`AutoAdjustSimulationFrameRate`).
3. Meanwhile the UI thread presents the frame and starts the next one. **The two threads run at the same time.**

One step, `SingleSimulationStep`:

```
InvokePendingSimThreadActions()        # work queued by the UI with RunOnSimThread
UpdateEmpires()                        # Empire.Update → EmpireAI (section 5.9)
UState.Objects.Update()                # UniverseObjectManager:
   UpdateLists → systems → ships (Parallel.For) → projectiles → spatial update
   → CollideAll → borders → sensors → ship AI (Parallel.For)
misc: explosions, bombs, shields, FTL, cargo shuttles, junk, derelicts
EndOfTurnUpdate()                      # military strengths, Dyson swarms
```

While the game is paused or saving, no turns advance: the sim thread drains queued actions and refreshes
object visibility only.

### Rules for Crossing Threads

The first six rules are in `.claude/skills/self-review-before-commit/stardrive-checklist.md`; the details
added to them and the last two rules describe the code.

- **UI changes game state only through `UniverseScreen.RunOnSimThread(action)`.** Use
  `UniverseScreen.IsOnSimThread` to decide whether to queue or run inline.
- **Sim code never touches UI objects** (scroll list entries, UI elements). Set a flag and let the UI act
  in its own `Update`, or queue the work with `GameScreen.RunOnNextFrame` (runs only while that screen is
  visible). `GameScreen.IsUIThread` tells you where you are.
- **UI code reads sim lists once per frame** (for example `fleet.Ships`, a planet's buildings) and must
  survive the list changing during the read. Do not assume two passes saw the same items.
- **Never remove an object the draw thread may be iterating.** Mark it dead (`Active = false`) and let the
  owning thread remove it in its own tick (`UniverseObjectManager.UpdateLists`).
- **No new locks or concurrent collections** without agreement from the maintainers. The house style is a
  null or `Active` check, or an atomic swap of an immutable object.
- **A non-popup screen over the universe stops it.** The universe is not drawn, so the sim thread waits.
  `RunOnSimThread` wakes it once only when the universe is hidden *and* paused; otherwise queued work waits
  until the covering screen closes.
- **Screens**: `ScreenManager.AddScreen` is thread-safe (queued); `AddScreenAndLoadContent` is main-thread only.
- **Debug drawing from any thread** goes through `GameScreen.Renderer` (`Graphics/DeferredRenderer.cs`),
  a locked queue of 2D primitives the screen draws later.

---

## 5. Systems Reference

### 5.1 Rendering

**Pipeline**: MonoGame `SpriteBatch` for 2D and UI, plus a custom forward renderer for 3D meshes.
2D drawing uses premultiplied alpha: PNGs are premultiplied on load and the `SD0001` analyzer guards
`Color` literals.

#### Key Classes

| Class | File | Role |
|---|---|---|
| `ScreenManager` | `GameScreens/ScreenManager.cs` | Owns `SpriteBatch`, `SpriteRenderer`, the `SceneInterface`, scene objects and lights |
| `SceneInterface`, `SceneObject`, `LightingEffect`, light types | `Data/Mesh/SunBurnStubs.cs` | The forward 3D renderer, under SunBurn's names. `SceneInterface.RenderScene` is the 3D pass |
| `LightingEffectBinder` | `Data/Mesh/LightingEffectBinder.cs` | Chooses lights per mesh and pushes them into effect slots |
| `SkinnedLightingEffect`, `BoneAnimationPlayer` | `Data/Mesh/` | Animated meshes |
| `StaticMesh` | `Data/Mesh/StaticMesh.cs` | A loaded mesh: buffers, materials, optional skin and animation |
| `MeshInterface`, `MeshImporter`, `MeshExporter` | `Data/Mesh/` | Bridge to `SDNative.dll` mesh functions |
| `BloomComponent`, `DistortionComponent` | `Graphics/` | Post-process bloom and shield-hit distortion |
| `ShadowMapComponent` | `Graphics/` | Shadow depth pass; not attached by any screen today (see below) |
| `ParticleManager` | `Graphics/Particles/` | Particles from `3DParticles/Particles.yaml` and `ParticleEffects.yaml` (hot-reloaded) |
| `RenderTargets`, `RenderStates`, `MemoryPressure` | `Graphics/` | Render target creation, cached blend/depth states, RAM/VRAM headroom probes (SharpDX DXGI) |
| `SpriteRenderer` | `SDGraphics/Sprites/SpriteRenderer.cs` | Batched sprite and quad drawing with `Effects/Simple.fx` |
| `TextureAtlas` | `SpriteSystem/TextureAtlas.cs` | Packs a texture folder into one atlas, cached on disk |

#### 3D Lighting

`LightingEffect` drives `Effects/MeshLighting.fx`; animated hulls use `SkinnedEffect.fx`. A change to hull
shading goes in both. The shader has three directional lights, three point lights anchored on the
system's sun, and eight dynamic point-light slots (`LightingEffectBinder.DynamicSlotCount`) for weapon and
explosion lights. Materials carry diffuse, emissive, specular, normal and tint maps.

The universe screen renders into `MainTarget`, then applies bloom (`GlobalStats.RenderBloom`), shield
distortion (`GlobalStats.RenderShieldDistortion`), fog of war (`BasicFogOfWar.fx` with ping-pong render
targets) and the empire border render target.

**Shadows**: `ShadowMapComponent`, `Shadow.fx` and the receiver path in `MeshLighting.fx` exist and are
tested, but `UniverseScreen` deliberately does not attach a shadow map (the comment in
`UniverseScreen.cs` explains the artifacts). Attaching one is a per-screen decision via
`ScreenManager.AttachShadowMap`.

#### Shaders: Sources and Builds

- HLSL sources (`.fx`) sit next to their compiled output in `game/Content/Effects/` and
  `game/Content/3DParticles/` (`ParticleEffect.fx`). The compiled files are committed. `Distort.fx` and
  `Distorters.fx` directly in `game/Content/` are old copies that nothing loads; shield distortion uses
  `Effects/Distort.fx`.
- **Nothing in the build compiles shaders.** `EnableMGCBItems` is false in every project that references
  MonoGame. After editing an
  `.fx`, compile it with MonoGame's `mgfxc` using `/Profile:DirectX_11` (as in
  `x64Migration/migration-plan-phase4.md`), with the tool version matching the MonoGame package.
- `MeshLighting.fx`, `SkinnedEffect.fx` and `Effects/Distort.fx` compile as `vs_4_0`/`ps_4_0`; the rest as
  `*_4_0_level_9_1`. The hardware floor is therefore Direct3D feature level 10.0.
- Two naming conventions: `Content.Load<Effect>("Effects/X")` prefers a sibling `X.mgfxo` over the old
  XNB; `GameContentManager.LoadEffect` / `LoadShader` and `SDGraphics.Shaders.Shader.FromFile` map `X.fx`
  to `X.mgfx`. `LoadEffect` looks for the `.mgfx` in the mod folder first. `LoadShader` looks for the
  `.fx` in the mod folder first and then loads the `.mgfx` beside it, so a mod overrides such a shader by
  shipping both files. `SpriteRenderer` loads `Content/Effects/Simple.mgfx` directly.
- Unit tests read the `.mgfxo` files from `game/Content` directly.
- `x64Migration/MIGRATION_LIMITATIONS.md` lists known renderer limits (single shadow caster, no PCF, no IBL).

#### Meshes

Ship, station, asteroid and projectile models live in `game/Content/Model/` as `.fbx` (some `.obj`).
`GameContentManager.LoadStaticMesh` takes a model name (an `.xnb` path, or no extension) and loads the
`.fbx` sibling, else the `.obj`. `RawContentLoader` → `MeshImporter` → `SDNative` (`SDMeshOpen`, NanoMesh
FBX/OBJ readers) → `StaticMesh`. Skinned FBX files load as a `StaticMesh` with a `BoneAnimationPlayer`. A
model with only an `.xnb` goes to MonoGame's `Model` reader and, if that fails, becomes an empty mesh with
a warning.

The in-tree `MeshExporter` exports static mesh data only. A full re-export of the original XNA 3.1 models
uses the `legacy/mesh_exporter_xna31` branch; see `x64Migration/re-export.md`,
`x64Migration/adr-mesh-export-toolchain.md` and `x64Migration/specular-overrides.md`.

#### Textures

| Format | Loader | Notes |
|---|---|---|
| PNG | `ImageUtils.LoadPng` → `SDNative` `LoadPNGImage` | Premultiplied on load unless asked otherwise |
| DDS | `ImageUtils.LoadDds` → C# `DxtReader` | Base level only, no mips. Model DXT1 maps stay compressed (`GameContentManager.LoadModelTexture`); everything else is decoded to RGBA |
| XNB | MonoGame `ContentManager`, with `Xna31Compat.Register` mapping XNA 3.1 texture readers to `Xna31Texture2DReader` | About 100 XNA 3.1 XNBs remain. The fonts in `game/Content/Fonts/` are XNB version 4 (XNA 3.1), not MGCB output |

UI textures are grouped into atlases per folder. `TextureAtlas` caches them under
`%APPDATA%\StarDrive\TextureCache` (or `TC-<mod name>` with a mod) and purges the cache when the version
changes. Prepacked `.atlas` + `.dds` files in content are used when present.

---

### 5.2 Content Loading and Mod Routing

| Class | File | Role |
|---|---|---|
| `ResourceManager` | `Data/ResourceManager.cs` | Loads all game data and graphics resources; file lookup helpers |
| `GameContentManager` | `Data/GameContentManager.cs` | MonoGame `ContentManager` subclass: asset cache, mod routing, effect/mesh/texture loading |
| `RawContentLoader` | `Data/RawContentLoader.cs` | Non-XNB assets: PNG, DDS, FBX, OBJ; `GetContentPath` |
| `YamlParser`, `YamlSerializer` | `Data/Yaml/`, `Data/YamlSerializer/` | Custom YAML parser; fills `[StarData]` objects |

**Load order**: `ResourceManager.LoadItAll(manager, mod)` → `LoadContent` (language, hulls, empires, troops,
weapons, modules, goods, roles, ship designs, buildings, random items, dialogs, techs, events, artifacts,
planet types, ...) → `LoadGraphicsResources` (atlases, nebulae, stars, flags, junk, asteroids, projectiles,
suns, ...). If a mod fails to load, the game shows an error, clears the mod and loads vanilla.

**Routing** (mod first, then vanilla):

```
asset request "Textures/foo"
  GameContentManager.OpenStream / RawContentLoader.GetContentPath
    path starts with "Mods/"         → load that file directly
    GlobalStats.ModPath + path exists → load the mod file
    otherwise                         → load "Content/" + path
```

- `ResourceManager.GetModOrVanillaFile(path)`: one file, mod copy wins.
- `ResourceManager.GatherFilesUnified(dir, ext)`: union of vanilla and mod files. `xml`, `hull` and
  `design` files match by file name (so subfolders can differ); other files match by relative path.
- `ResourceManager.GatherFilesModOrVanilla(dir, ext)`: the mod's files if it has any, else vanilla.

**Data formats**: per-item data such as `Technology/`, `Buildings/`, `Weapons/`, `ShipModules/`, `Races/`
is XML (`XmlSerializer`). Hulls are `.hull`, ship designs `.design` (custom text; `ShipDesign.Parse`,
`ShipDesignWriter`). Global tables (`Globals.yaml`, `GameText.yaml`, `Codex.yaml`, `PlanetTypes.yaml`,
`Suns.yaml`, `Audio/AudioConfig.yaml`, ...) are YAML. `ScreenManager.AddHotLoadTarget` reloads watched
files while the game runs.

---

### 5.3 UI

**Architecture**: a stack of screens managed by `ScreenManager`; each screen is a tree of UI elements.

| Class | File | Notes |
|---|---|---|
| `ScreenManager` | `GameScreens/ScreenManager.cs` | Screen stack, pending-screen queue, input dispatch, scene objects, music |
| `GameScreen` | `GameScreens/GameScreen.cs` | Base screen; derives from `MultiLayerDrawContainer`. `IsPopup`, `RunOnNextFrame`, `Renderer` |
| `PopupWindow` | `PopupWindow.cs` | Base for popup screens (for example `CodexScreen`) |
| `UIElementV2`, `UIElementContainer`, `UIPanel`, `UIButton`, `UILabel`, `ScrollList` | `UI/` | Element framework |
| `LayoutParser`, `RelPos`, `RelSize` | `UI/` | YAML layouts (`game/Content/UI/MMenu.*.yaml`) and relative positioning |
| `UIEffect`, `AnimationCurve` | `UI/Effects/` | Element animation |

#### Notable Screens

| Screen | Purpose |
|---|---|
| `GameLoadingScreen`, `MainMenuScreen` | Startup splash and loading, main menu (`GameScreens/MainMenu/`) |
| `UniverseScreen` | Star map and space combat; owns the sim thread (`Universe/UniverseScreen/`, 14 partial files) |
| `ColonyScreen` | Planet management (`GameScreens/ColonyScreen/`, split into partial files) |
| `CombatScreen` | Ground combat on a planet (`GameScreens/CombatScreen/`) |
| `ShipDesignScreen`, `FleetDesignScreen` | Ship designer and fleet formations |
| `DiplomacyScreen`, `InfiltrationScreen`, `ResearchScreenNew` | Diplomacy, espionage, research |
| `RaceDesignScreen`, `GameSetupScreen`, `CreatingNewGameScreen` | New game setup (`GameScreens/NewGame/`) |
| `LoadSaveScreen`, `LoadUniverseScreen` | Save list and save loading |
| `CodexScreen` | In-game Codex (`Codex/CodexScreen.cs`) |
| `DebugInfoScreen` | Debug overlay with pages in `Debug/Page/` (AI, threat matrix, tasks, wars, spatial, ...) |

---

### 5.4 Localization and the In-Game Codex

#### GameText

- All player-facing text lives in `game/Content/GameText.yaml`. Each token has a NameId key, an `Id` and
  language columns `ENG`, `RUS`, `SPA`, `UKR`, `GER`, `PTB`, `POL` (enum `Language` in `GlobalStats.cs`).
  A missing translation falls back to English. Coverage differs a lot by language; check the file.
- `Ship_Game/Data/GameText.cs` is the `GameText` enum of token ids. Its header says it is generated, but
  the project rule is to add members by hand next to their id neighbors.
- `Localizer` (`Data/Localizer.cs`) loads the vanilla file, then the mod's `GameText.yaml`. A mod token with
  the same NameId replaces the vanilla text.
- `LocalizedText` converts implicitly from `string`, so a hardcoded English string compiles but is never
  translated. Do not do that.
- Id ranges: the file header reserves 1–99,999 for general text and 100,000–199,999 for Codex text.
  New UI tokens take free ids in 4000–8000 (never 18000–20000); check the vanilla file, the enum and every
  `game/Mods/*/GameText.yaml` first.
- Tools: `--run-localizer` and `--merge-translations` (`Ship_Game/Tools/Localization/`).

#### The Codex

- `game/Content/Codex.yaml` is the entry tree (`UID`, `Children`, `Hidden`, `VideoPath`, `Link`). A mod's
  `Codex.yaml` replaces the vanilla one.
- Entry text is in `GameText.yaml` with ids 100,000 and up. Names follow the UID: `Codex` + PascalCase(UID),
  plus `Short` and `Text` suffixes (`CodexEntry.ResolveDefaults`). No enum members.
- `game/Content/CodexHooks.yaml` maps tooltip NameIds to entry UIDs. Hooked tooltips show "Press F1 for
  details", and F1 (`InputState.CodexHelp`) opens that entry. Both files are re-read when the Codex opens.
- Content rules: the Codex describes the rules the code uses, so a change to a mechanic updates its entry
  and tooltips in the same change. It does not reveal AI decision rules. Plan: `docs/codex-plan.md`.

---

### 5.5 Audio and Video

**Audio backend**: NAudio. There is no XNA or MonoGame audio in use.

| Class | File | Role |
|---|---|---|
| `GameAudio` | `Audio/GameAudio.cs` | Static entry point; `Update()` every frame |
| `AudioConfig`, `AudioCategory` | `Audio/AudioConfig.cs` | Categories and cues from `game/Content/Audio/AudioConfig.yaml` |
| `AudioDevices` | `Audio/AudioDevices.cs` | Output device tracking and device-change notifications |
| `NAudioPlaybackEngine`, `NAudioSampleMixer` | `Audio/NAudio/` | Mixing and playback |
| `NAudioMFReader`, `CachedSoundEffect` | `Audio/NAudio/` | Media Foundation decoding; cached effects |

Game audio files are `.m4a`. Videos (`game/Content/Video/*.wmv`) play through MonoGame's `VideoPlayer`;
`ProbeVideoBackend` disables video when Media Foundation is missing.

---

### 5.6 Input

| Class | File | Role |
|---|---|---|
| `InputState` | `Input/InputState.cs` (+ `_DoubleClick`, `_Holding`) | Per-frame keyboard and mouse state, named actions (`CodexHelp`, speed keys, ...) |
| `IInputProvider`, `DefaultInputProvider` | `Input/InputState.cs` | Source of raw input; tests swap in `MockInputProvider` |
| `UniverseKeys` | `Input/UniverseKeys.cs` | Universe screen hotkeys |
| `InputBindings` | `Input/InputBindings.cs` | Configurable bindings; marked incomplete in its own comment |

**Flow**: `ScreenManager.Update` → `input.Update` → `HandleInput` on the topmost screen; while it stays
active, the screens below get none. If it closes itself during `HandleInput` and returns false, the screen
below also gets that frame's input. No screen gets input while the game window is out of focus.

---

### 5.7 Saves and Settings

**Format**: a custom binary serializer driven by attributes.

| Piece | File | Role |
|---|---|---|
| `[StarDataType]`, `[StarData]`, `[StarDataDeserialized]`, `[StarDataConstructor]` | `Data/Serialization/StarDataTypeAttribute.cs` | Opt-in markup for saved and YAML-loaded types |
| `BinarySerializer` + reader/writer | `Data/Binary/` | Save files |
| `SavedGame`, `HeaderData` | `SavedGame.cs` | Save and load entry points; `SaveGameVersion` |
| `UniverseScreen.Save*` | `Universe/UniverseScreen/UniverseScreen.Save.cs` | Manual saves and autosaves |

- A save is `BinarySerializer.SerializeMultiType(writer, { HeaderData, UniverseState })`.
- Saves start on the sim thread (`SaveDuringNextUpdate` → `CheckForPendingSaves`). `SaveAsync` serializes on
  a pool thread while `IsSaving` holds the simulation.
- Autosaves (`Autosave1..3`) happen after `GlobalStats.AutoSaveFreq` seconds of unpaused play.
- Fields match by **name**. Renaming a field loads old saves with the initializer. A field that no longer
  exists is skipped on read. Deleting a `[StarDataType]` type that old saves still hold breaks those saves.
- Enums are saved by numeric value (`EnumSerializer`). Reordering or renumbering a saved enum changes what
  old saves load; enums that allow gaps say so in their header comment.
- The writer skips a value equal to the **declared** default (`[StarData(DefaultValue = X)]`, else the
  type's zero), never the C# initializer. A new field whose "unset" value is not zero needs `DefaultValue`.
  An existing field that must keep a saved 0 needs `[StarData(NoDefaults = true)]`.
- Objects rebuilt on load (`[StarDataDeserialized]` methods, `Ship.InitializeStatus` with `fromSave`) must
  carry over the state the save just loaded.

**Versioning**: `SavedGame.SaveGameVersion` is 21. `LoadSaveScreen` lists only saves whose header version
equals it, so Mars (20) and Jupiter (21) saves are invisible to each other. Adding or removing fields
does not need a bump; a change that stops older saves loading does (ask the maintainers first). CI publishes
the number as a `save-version-N.txt` release asset, and `AutoUpdateChecker.UpdateBreaksSaves` warns players
before a patch that raises it.

**Files under `%APPDATA%\StarDrive\`** (`SDUtils/Dir.cs`, `Dir.StarDriveAppData`):

| Path | Contents |
|---|---|
| `Saved Games/`, `Saved Races/`, `Saved Setups/`, `Fleet Designs/`, `Saved Designs/`, `WIP/`, `Colony Blueprints/` | Player data |
| `StarDrive.user.config` | User settings (`GlobalStats.LoadConfig`); defaults come from `app.config` |
| `TextureCache/`, `TC-<mod>/` | Texture atlas cache |
| `Patches/`, `PendingPatch.json` | AutoPatcher downloads and the elevated-resume marker |

---

### 5.8 Modding

| Class | File | Role |
|---|---|---|
| `ModInformation` | `ModInformation.cs` | The `Mod:` block of a mod's `Globals.yaml` |
| `GamePlayGlobals` | `GamePlayGlobals.cs` | Gameplay settings from `Content/Globals.yaml` or the mod's `Globals.yaml` |
| `ModManager` | `ModManager.cs` | Mod selection screen |
| `ModEntry` | `ModEntry.cs` | One mod in the list; `CheckSupport` matches `SupportedBlackBoxVersions` |
| `AvailableModsScreen` | `GameScreens/MainMenu/AvailableModsScreen.cs` | Known mods and their GitHub releases |

- A mod is a folder `game/Mods/<Name>/` that mirrors `Content/`. It must not contain a `Content/`
  subfolder (`ResourceManager.InitContentDir` throws).
- `Globals.yaml` → `Mod:` has `Name`, `Description`, `Author`, `IconPath`, `Version`, `FormatVersion`,
  `SupportedBlackBoxVersions` (for example `"jupiter-1.60"`), the `UseVanilla*` flags (ships, techs, weapons,
  modules, buildings, races), `ClearVanillaTechs`, `ClearVanillaWeapons` and `DisableDefaultRaces`.
  `FormatVersion` must equal `ModEntry.FormatVersion` (1), or New Game and Load Game are refused.
- The mod's update page, `DownloadSite`, is a field of `Globals:` itself, next to `URL`, not of `Mod:`.
- `SupportedBlackBoxVersions` is matched against the version suffix CI writes into
  `AssemblyInformationalVersion` (for example `1.60.NNNNN jupiter-1.60`).
- Mods can replace textures (PNG, DDS), meshes (FBX, OBJ), compiled shaders, audio, YAML, XML, hulls and
  designs. A `.design` file records a `ModName`; a vanilla design (empty `ModName`) that uses modules the
  mod lacks is ignored with a warning.
- `--mod="<Name>"` starts with a mod; `--resource-debug` logs which files a mod supplies.
- Only `ExampleMod` is in this repository. Combined Arms and Star Trek: Shattered Alliance have their own
  repositories, so check them too before claiming content is unused.

---

### 5.9 AI Layers

Everything below runs on the sim thread; ship updates and ship AI fan out over `Parallel.For`.

| Layer | Where | What it does |
|---|---|---|
| Empire update | `Empire.cs` (+ `Empire_*.cs` partials) | `Empire.Update` calls `EmpireAI.Update` once per turn |
| Empire AI | `AI/EmpireAI/EmpireAI*.cs` (partial class `EmpireAI`) | Runs the planners: economic, expansion, research stations, mining, salvage, space roads, diplomacy, research, espionage, then military and war. Then evaluates every `Goal` |
| Goals | `AI/Goal.cs`, `Commands/Goals/*.cs` | Saved state machines: an array of step functions returning a `GoalStep` |
| Expansion and research | `AI/ExpansionAI/`, `AI/Research/` | `ExpansionPlanner`, `MiningOpsPlanner`, `ResearchStationPlanner`, `SalvageOpsPlanner`; `ChooseTech`, `ShipPicker` |
| Military tasks | `AI/Tasks/MilitaryTask*.cs`; list in `AI/EmpireAI/EmpireAI.RunMilitaryPlanner.cs` | Tasks such as `AssaultPlanet`, `DefendClaim`, `ClearAreaOfEnemies`, `StrikeForce` that request and hold fleets. War, defense and claim tasks keep their fleet when they end: a task finds its force in `AI/Tasks/MilitaryTask_KeptFleets.cs`, and `AI/EmpireAI/EmpireAI.KeptFleets.cs` runs the per-turn upkeep (cap, refits, splits, break-ups). `TaskType` order is saved, so do not reorder it |
| Wars | `AI/StrategyAI/` (`War`, `WarScore`, `WarState`, `FleetBuildRatios`) | War state and fleet composition |
| Threat and defense | `AI/ThreatMatrix*.cs`, `AI/ThreatCluster.cs`, `AI/DefensiveCoordinator.cs`, `AI/SystemCommander.cs` | Known enemy strength by area; system defense |
| Fleets | `Fleets/Fleet*.cs` (`Fleet : ShipGroup`) | Formation, movement, staging, combat orders, flanking; an idle kept fleet waits at a rally planet, and its refit and split steps are in `Fleets/Fleet.Idle.cs` |
| Ship AI | `AI/ShipAI/` (`ShipAI`, `ShipGoal` order queue, `CombatAI`), `AI/CombatTactics/`, `AI/ShipMovement/` | Orders, combat stances (`Artillery`, `BroadSides`, `OrbitTarget`, `Evade`, `HoldPosition`, `AttackRun`) |
| Routing | `AI/GravityWellRouter.cs` | Waypoints around gravity wells and hostile systems, computed when an order is given |
| Factions and others | `Pirates.cs`, `Remnants.cs`, `AI/DroneAI.cs`, `AI/MissileAI.cs` | Faction AI; drones and missiles |

---

### 5.10 Networking, Updates, Logging and Error Reporting

**Networking**: none for gameplay. HTTPS calls go to the GitHub API and release downloads
(`AutoUpdateChecker`, `AutoPatcher`) and to Sentry.

**Updates** (`GameScreens/MainMenu/`):

- `AutoUpdateChecker` reads `api.github.com/repos/<owner>/<repo>/releases` (built from `DownloadSite` in
  `Globals.yaml`). It picks the highest non-pre-release release (`TrySelectMaxVersionRelease`). The same
  major.minor is offered as an in-game patch (`AutoPatcher`); a higher one shows
  `MajorUpgradeAvailablePopup` with the page in `game/upgrade-url.txt`. A mod's `DownloadSite` is checked
  the same way, filtered to the game's major.minor line (Bitbucket download pages are also supported).
  Builds whose version does not parse fall back to `/releases/latest`.
- `AutoPatcher` downloads into `%APPDATA%\StarDrive\Patches\`, unzips, deletes stale files and copies the
  patch over the install. If the install is under "Program Files" and the process is not elevated, it writes
  `PendingPatch.json` and relaunches with `--apply-patch=<version>` as administrator.
- **Do not accept the patch in the repository's own `game\` folder**: it overwrites tracked content and
  binaries with the released copies.

**Logging** (`Utils/Log.cs`): `blackbox.log` (previous run in `blackbox.old.log`) in the game folder, or in
`%APPDATA%\StarDrive\` when the game is under "Program Files". `Log.Info` is compiled only in Debug.
`Log.ErrorDialog` shows the exception dialog and, for a non-zero exit code, ends the process. The log is
truncated after each successful autosave.

**Sentry**: `Log.Initialize` starts the Sentry SDK only for official version strings
(`major.minor.build`), so forks with extra version parts do not report. Release is the version number;
environment is Release, Test or Staging (debugger or Debug build). Events are sent only while the user
setting `GlobalStats.AutoErrorReport` is on, which also enables session tracking. Error events carry the
last 128 KB of the log. Nothing is reported when the game starts under a debugger. A repeated error is reported
once, then every 100th time (`Log.ShouldIgnoreErrorText`).

**Steam**: `Utils/SteamManager.cs` is a stub. `Initialize()` returns false and every call is a no-op
(`ActivateWebOverlay` just opens the browser). The x86 Steam libraries were dropped and Steamworks.NET is
not wired in; callers already check `SteamManager.IsInitialized`.

---

## 6. Native Code (SDNative)

**Binary**: `game/SDNative.dll` (plus `game/libfbxsdk.dll`, copied by the post-build step)
**Source**: `SDNative/SDNative.vcxproj` — C++20, toolset v143, Windows SDK 10.0
**Platform**: built as `x64` by the solution (the project file still also defines `Win32` configurations)

### Components

| Module | Location | Purpose |
|---|---|---|
| Mesh system | `SDNative/SdMesh/` (`SdMesh`, `SdMeshGroup`, `SdMaterial`, `SdAnimation`) | Mesh data, materials, tangent space, skinned bones and animation clips |
| NanoMesh (submodule) | `SDNative/NanoMesh/` | FBX, OBJ and text mesh readers/writers; pinned to a fork branch per `.gitmodules` |
| FBX SDK | `SDNative/3rdparty/fbxsdk/` | Autodesk FBX SDK 2020.3.7, linked as a DLL |
| Spatial | `SDNative/spatial/` (`Grid`, `GridL2`, `Qtree`, `Collision`, `Search`) | Broad-phase collision and nearby search |
| Images | `SDNative/util/image_utils.cpp`, `3rdparty/libpng`, `lodepng`, `soil2` | PNG load/save, DDS save, BGRA/RGBA conversion |
| Utilities | `SDNative/util/os_utils.cpp`, `util/math_utils.cpp` | `GetPhysicalCPUCoreCount`, rotation math |
| Ship data | `SDNative/ShipDataSerializer.cpp`, `util/byte_buffer.cpp` | Legacy ship parsing and the `.design` writer's buffer |
| ReCpp (submodule) | `SDNative/ReCpp/` | Utility library (files, paths, strings, tests) |

### P/Invoke Callers

| C# file | Native functions |
|---|---|
| `Data/Mesh/MeshInterface.cs` | `SDMeshOpen`, `SDMeshClose`, `SDMeshGetGroup`, `SDMeshGroupGetData`, `SDMeshSave`, bone and animation getters/setters |
| `Data/Texture/ImageUtils.cs` | `LoadPNGImage`, `SaveImageAsPNG`, `SaveImageAsDDS`, `ConvertBGRAtoRGBA` |
| `Spatial/NativeSpatial.cs` | `SpatialCreate`, `SpatialUpdate`, `SpatialCollideAll`, `SpatialFindNearby`, ... (`SpatialManager` uses the native `Qtree` by default) |
| `Ships/ShipDesignWriter.cs` | `ByteBuffer*` |
| `Ships/Legacy/LegacyShipData_LegacyParser.cs` | `CreateShipDataParser`, `DisposeShipDataParser` |
| `Threading/Parallel.cs` | `GetPhysicalCPUCoreCount` |
| `SDGraphics/RadMath.cs` | Rotation helpers (`RadiansToDirection`, `RotatePoint`, ...) |

Structs that hold pointers use default sequential layout; `Pack = 4` remains only on structs of floats and
ints (bone poses, key frames). `AllowUnsafeBlocks` is on in all C# game projects.

---

## 7. Third-Party Libraries and Tools

Versions are as read from the project files on 2026-11-18.

| Library / tool | Version | Source | Used for |
|---|---|---|---|
| MonoGame.Framework.WindowsDX | 3.8.1.303 | NuGet (`StarDrive`, `SDGraphics`, `SDUnitTests`) | Game loop, graphics, content, input, video |
| MonoGame.Content.Builder.Task | 3.8.1.303 | NuGet | Referenced, but the build pass is disabled (`EnableMGCBItems=false`). `game/Content/Content.mgcb` holds a font build recipe |
| SharpDX, SharpDX.DXGI, SharpDX.Direct3D11 | 4.0.1 | NuGet (`StarDrive`) | VRAM queries in `MemoryPressure` |
| NAudio | 2.2.1 | NuGet | Audio |
| Sentry | 3.41.4 | NuGet | Error reporting |
| LargeAddressAware | 1.0.6 | NuGet (`StarDrive`, `SDUnitTests`) | Left over from the x86 build (section 9) |
| Microsoft.CodeAnalysis.CSharp | 4.8.0 | NuGet (`SDAnalyzers`) | Analyzer SDK |
| MSTest.TestAdapter / MSTest.TestFramework | 3.11.1 | NuGet (`SDUnitTests`) | Tests |
| Microsoft.NET.Test.Sdk | 17.14.1 | NuGet (`SDUnitTests`) | Tests |
| Autodesk FBX SDK | 2020.3.7 | `SDNative/3rdparty/fbxsdk` | FBX import/export |
| libpng | 1.6.37 | `SDNative/3rdparty/libpng` | PNG |
| lodepng, soil2, stb, rapidxml, ImGui | not recorded | `SDNative/3rdparty/` | lodepng and soil2 are compiled into `SDNative`; ImGui only into `SDNativeTests` |
| NanoMesh, ReCpp | git submodules | `SDNative/` | Mesh I/O, utilities |
| NSIS, 7-Zip | vendored | `Deploy/NSIS/`, `Deploy/7-Zip/` | Installer and patch ZIPs |
| WiX | 3.14.1 | `Deploy/packages.config` | MSI project (not built by the solution) |

---

## 8. Build, Test and Release

### Building

- Requirements (`README.md`): Visual Studio 2022 with .NET desktop (.NET 8 SDK), Desktop C++ (MSVC v143)
  and the Windows 10 SDK. Clone with submodules.
- Build `StarDrive.sln` as `Release|x64` with Visual Studio's MSBuild before a PR, because only Release
  treats warnings as errors. CI runs:
  `msbuild StarDrive.sln /m /verbosity:minimal /restore /p:Configuration=Release /p:Platform=x64`.
- The game runs from `game\`, which is the output folder and the working directory.
- Version: `Properties/AssemblyInfo.cs` holds `1.60.00000` / `1.60.00000 jupiter-1.60` locally; CI stamps
  the build number into all four `AssemblyInfo.cs` files.

### Tests

- **C#**: `UnitTests/SDUnitTests.csproj` (MSTest; about 330 test classes and 2,900 `[TestMethod]`s as of
  2026-11-18). Commands from the review checklist:
  `dotnet build UnitTests/SDUnitTests.csproj`, then `dotnet test UnitTests/SDUnitTests.csproj --no-build`.
- Output goes to `UnitTests/bin/Debug/` for every configuration.
- `StarDriveTestContext` (`[AssemblyInitialize]`) changes to `game\`, creates a hidden `TestGameDummy`
  with a real graphics device, forces vanilla content (`GlobalStats.SetActiveModNoSave(null)`), turns off
  Sentry and player lockout, and loads test content (`ResourceManager.LoadContentForTesting`).
- Tests derive from `StarDriveTest` (`CreateUniverseAndPlayerEmpire`, ship spawning, ...).
  Test universes run no sim thread: `CreateUniverseAndPlayerEmpire` never calls `UniverseScreen.LoadContent`,
  where the thread starts; the sandbox helpers set `CreateSimThread = false`; `LoadSave` passes
  `startSimThread: false`. Tests step the simulation themselves.
- `[TestCategory("Performance")]` tests are excluded when `unittests.runsettings` is used (CI uses it).
- A new test must fail without the fix. The float `AssertEqual` overload takes the tolerance first.
- **Native**: `SDNativeTests` exercises the spatial structures (grid, quadtree, nearby search, collisions).

### Release

| Flow | Trigger | What happens |
|---|---|---|
| Patch | Push to `main`, or manual (`.github/workflows/patch-build.yml`) | Release\|x64 build, vstest run, `MakeInstaller.py --patch` (ZIP + NSIS), `save-version-N.txt`, draft GitHub release `jupiter-patch-1.60.NNNNN`, Discord notice |
| Publish | Publishing the draft (`notify-release.yml`) | Discord announcement unless marked pre-release. Published non-pre-release patches reach players through `AutoUpdateChecker` |
| Major | Local `Deploy` build | `MakeInstaller.py --major --type=nsis`; uploaded by hand |

The NSIS scripts (`Deploy/BlackBox-Jupiter.nsi`, `Deploy/BlackBox-Jupiter-Patch.nsi`, both including
`Deploy/BBInstaller.nsi`) default to `C:\Games\StarDrivePlus64` and record `HKLM\Software\StarDrivePlus64`.

**Branches**: work collects on a rolling `jupiter-1.60/fixes_NN` branch cut from `main` and merged back
into `main` with a merge commit. Community PRs target the current fixes branch, not `main` (see the
Development Cycle section of `README.md` and the review checklist). `mars-1.51` keeps the old line;
`legacy/mesh_exporter_xna31` keeps the XNA 3.1 mesh exporter.

---

## 9. Technical Debt

### Large Files (lines, as of 2026-11-18)

| File | Lines | Note |
|---|---|---|
| `Data/GameText.cs` | 6,456 | Enum of text ids |
| `Fleets/Fleet.cs` | 3,469 | Plus `Fleet.Idle.cs`, `Fleet.Flank.cs` |
| `Empire.cs` | 2,977 | Plus 12 `Empire_*.cs` partials |
| `Data/ResourceManager.cs` | 2,361 | Loading, caches, file routing, test helpers |
| `Ships/Ship.cs` | 2,217 | Plus 14 `Ship_*.cs` partials |
| `SDGraphics/Matrix.cs` | 2,186 | Math |
| `Gameplay/Relationship.cs` | 1,860 | Diplomatic state and AI |
| `Universe/SolarBodies/Planet/Planet.cs` | 1,798 | Plus 10 `Planet_*.cs` partials |
| `Ships/ShipModule.cs` | 1,726 | |
| `Data/Texture/DxtReader.cs` | 1,538 | Focused |
| `Data/Mesh/SunBurnStubs.cs` | 1,495 | The 3D renderer under SunBurn names |
| `Universe/UniverseScreen/UniverseScreen.HandleInput.cs` | 1,449 | |
| `Remnants.cs` | 1,444 | |

### Renderer and Content

- **SunBurn names live on.** The renderer sits in `SunBurnStubs.cs` under `SynapseGaming.LightingSystem.*`
  namespaces, which 39 files import. Some members are empty: `LightingSystemManager`,
  `SceneInterface.CreateDefaultManagers`, `AddManager`, `Update` and `ApplyPreferences`. The options path
  `GameBase.UpdateRendererPreferences` → `ScreenManager.UpdatePreferences` therefore ends in a no-op.
- Shadow mapping is built and tested but not attached anywhere.
- About 100 `.xnb` files remain in `game/Content` (effects, fonts, tutorials, video sidecars, loading
  screen and a few others). The XNA 3.1 readers (`Xna31Texture2DReader`, `Xna31VertexDeclarationReader`,
  `SunBurnReaderStubs`) stay for them. An XNB-only model loads as an empty mesh.
- `MeshInterface.cs` documents a dormant layout mismatch: the write-side `SdBonePose` (40 bytes) differs from
  the native `BonePose` (36 bytes).
- Shaders are compiled by hand; a stale `.mgfxo` is not caught by the build.

### Code Structure

- Static singletons: `GlobalStats`, `ResourceManager`, `ScreenManager.Instance`, `GameBase.Base`,
  `StarDriveGame.Instance`.
- Cross-thread access relies on conventions (section 4), not on types or locks.
- Dead or unfinished code: `PathFinder/Astar.cs` has no callers (`GravityWellRouter` replaced it);
  `InputBindings` is marked incomplete; `DeferredRenderer` is a debug-primitive queue whose name suggests
  a 3D renderer.
- Many comments still narrate migration phases (`Phase 2.8`, `§3.7`) or XNA behavior.

### Leftovers from the 32-bit Stack

- Tracked but unused: `Microsoft.Xna.Framework.dll`, `XnaNative.dll`, `XNAnimation.dll` at the repository
  root, and `SynapseGaming-SunBurn-Pro/` (the old `SDSunBurn` project, not in the solution).
- `UnitTests/` tracks XNA-era DLLs (`Microsoft.Xna.Framework.dll`, `XnaNative.dll`, `d3d9.dll`,
  `d3dx9_31.dll`, `X3DAudio1_6.dll`) and copies `X3DAudio1_6.dll` to its output; `game/X3DAudio1_6.dll`
  is tracked too. `UnitTests/TestGameDummy.cs.rej`, a patch reject file, is tracked.
- `LargeAddressAware` is still referenced although the process is 64-bit.
- `SDNative.vcxproj` and `SDNativeTests.vcxproj` keep `Win32` configurations; the solution maps
  `SDInstaller` to `x86`.
- The application icon is `Icons/Mars.ico`.

---

## 10. Migration Outcome

The x86 XNA 3.1 + SunBurn build was migrated to 64-bit MonoGame for the Jupiter 1.60 release. The plans,
per-phase results and known limits are in `x64Migration/` (`migration-plan-phase1.md` to `-phase5.md`,
`PHASE1_RESULTS.md` to `PHASE3_RESULTS.md`, `MIGRATION_LIMITATIONS.md`). The pre-migration architecture is
in [ARCHITECTURE_pre_64bit.md](ARCHITECTURE_pre_64bit.md).

| Area | Before (Mars 1.51) | Now (Jupiter 1.60) |
|---|---|---|
| Runtime and platform | .NET Framework 4.8, x86 with LargeAddressAware | .NET 8 (`net8.0-windows`), x64 |
| Framework | XNA 3.1 plus a custom `Microsoft.Xna.Framework.Game` project | MonoGame WindowsDX 3.8.1.303; `GameBase` derives from MonoGame's `Game` |
| 3D lighting | Decompiled SunBurn Pro (`SDSunBurn`) | Forward renderer in `SunBurnStubs.cs` + `LightingEffectBinder` + `MeshLighting.fx` / `SkinnedEffect.fx` |
| Skinned models | XNAnimation | `StaticMesh` + `BoneAnimationPlayer` + `SkinnedLightingEffect`, from FBX |
| Effects | XNA effect XNBs (D3DX bytecode) | Hand-ported HLSL compiled with `mgfxc` (`.mgfxo` / `.mgfx`) |
| Models | XNB models | FBX/OBJ through NanoMesh; old XNBs archived in `game/LegacyMesh/` |
| Remaining XNB textures and fonts | Read by XNA 3.1 | Still shipped; read through `Xna31Compat` readers on MonoGame |
| Native DLL | Win32 `SDNative.dll`, FBX SDK 2018 | x64 `SDNative.dll`, FBX SDK 2020.3.7 |
| Steam | x86 Steam libraries | `SteamManager` stub |
| CI | AppVeyor | GitHub Actions `patch-build.yml` |
| Saves | `SaveGameVersion` 20 | 21; Mars saves do not load |
| Install folder | `C:\Games\StarDrivePlus` | `C:\Games\StarDrivePlus64` |
