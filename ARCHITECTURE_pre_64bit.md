# StarDrive BlackBoxPlus — Architecture Reference

> **Pre-migration snapshot (2026-04-28); the current reference is [ARCHITECTURE.md](ARCHITECTURE.md).**

> **Purpose**: Living architectural reference for the BlackBoxPlus revival project.  
> **Scope**: Current state analysis + migration roadmap for XNA 3.1 + SunBurn → MonoGame (64-bit).  
> **Last Updated**: 2026-04-28

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Solution Structure](#2-solution-structure)
3. [Entry Points & Startup](#3-entry-points--startup)
4. [Game Loop](#4-game-loop)
5. [Systems Reference](#5-systems-reference)
   - [Rendering](#51-rendering)
   - [UI](#52-ui)
   - [Audio](#53-audio)
   - [Input](#54-input)
   - [AI](#55-ai)
   - [Networking](#56-networking)
   - [Save / Load](#57-save--load)
   - [Modding](#58-modding)
6. [Native C++ Integration (SDNative)](#6-native-c-integration-sdnative)
7. [Third-Party Libraries](#7-third-party-libraries)
8. [Technical Debt & Code Smells](#8-technical-debt--code-smells)
9. [Migration Roadmap: XNA + SunBurn → MonoGame](#9-migration-roadmap-xna--sunburn--monogame)

---

## 1. Project Overview

StarDrive was a space strategy game released in 2013. The code was decompiled with the original developer's permission and significantly reworked by the BlackBoxPlus team:

- Many gameplay features added and redesigned
- Performance-critical sections moved to native C++ (SDNative)
- Original XNA 3.1 + SunBurn (32-bit) stack retained — **target of the current migration**

The immediate goal is to replace XNA 3.1 + SunBurn with **MonoGame** and rebuild SDNative as a **64-bit DLL**, removing the 32-bit memory ceiling.

---

## 2. Solution Structure

**Solution file**: `StarDrive.sln`  
**Runtime**: .NET Framework 4.8  
**Platform**: x86 (32-bit) with LargeAddressAware — **to be changed to x64**

### Projects

| Project | Type | Purpose |
|---|---|---|
| `StarDrive` | C# Exe | Main game executable |
| `SDNative` | C++/CLI (vcxproj) | Native mesh I/O, image processing, spatial structures |
| `Microsoft.Xna.Framework.Game` | C# Lib | Custom XNA 3.1 Game class wrapper |
| `SDSunBurn` | C# Lib | Decompiled SynapseGaming SunBurn Pro (lighting engine) |
| `SDGraphics` | C# Lib | Graphics math: Vector2/3/4, Matrix, rendering primitives |
| `SDUtils` | C# Lib | Generic utilities: Array, Map, collections (no XNA dependency) |
| `SDUnitTests` | C# Test | .NET unit tests |
| `SDNativeTests` | C++ Test | Native unit tests |
| `SDInstaller` | WiX | Installer |

### Top-Level Folder Map

```
BlackBoxPlus/
├── Ship_Game/          # All main game code (~1,278 .cs files)
│   ├── AI/             # Empire AI, ship AI, pathfinding, threat matrix
│   ├── Audio/          # NAudio-based audio system
│   ├── Commands/       # Military goals & commands
│   ├── Data/           # Content loading, serialization, mesh/texture pipeline
│   ├── Debug/          # Debug screens and visualization
│   ├── Empires/        # Empire components
│   ├── Espionage/      # Spy network
│   ├── Fleets/         # Fleet management
│   ├── GameScreens/    # Screens, ScreenManager, Program.cs, StarDriveGame.cs
│   ├── Gameplay/       # Weapons, asteroids, projectiles, gameplay systems
│   ├── Graphics/       # SunBurn integration, particles, deferred renderer, bloom
│   ├── Input/          # InputState, key bindings
│   ├── PathFinder/     # A* pathfinding
│   ├── Ships/          # Ship data, modules, designs
│   ├── Spatial/        # Quadtree spatial indexing
│   ├── SpriteSystem/   # Sprite batching, texture atlases
│   ├── StoryAndEvents/ # Event system, notifications
│   ├── Threading/      # Custom thread pool / parallel task system
│   ├── UI/             # UI framework: layout, effects, components
│   └── Universe/       # Universe generation, solar systems, planets
├── SDGraphics/         # Graphics math library
├── SDUtils/            # Generic utilities library
├── SDNative/           # Native C++ DLL source
│   ├── SdMesh/         # Mesh data structures
│   ├── spatial/        # Collision / spatial structures
│   ├── 3rdparty/       # FBX SDK, libpng
│   └── NanoMesh/       # Mesh loading/parsing
├── SynapseGaming-SunBurn-Pro/  # Decompiled SunBurn source
├── Microsoft.Xna.Framework.Game/  # Custom XNA wrapper
├── UnitTests/          # Test projects
├── game/               # Build output: binaries + Content/
│   ├── Content/        # Game assets (textures, models, data files)
│   └── Mods/           # Mod directories
└── packages/           # NuGet (WiX installer only)
```

---

## 3. Entry Points & Startup

**Main entry point**: `Ship_Game/GameScreens/Program.cs` → `Program.Main()` (line 191)

### Startup Sequence

```
Program.Main()
  GlobalStats.LoadConfig()       # Must run before any logging
  Log.Initialize()               # Sentry error reporting
  ParseMainArgs()                # CLI flags (--mod, --export-textures, etc.)
  new StarDriveGame()
  game.Run()                     # Enters XNA game loop
```

### Game Class Hierarchy

```
StarDriveGame   [Ship_Game/GameScreens/StarDriveGame.cs]
  └─ GameBase   [Ship_Game/GameScreens/GameBase.cs]
       └─ Game  [Microsoft.Xna.Framework.Game/Game.cs]  ← custom XNA wrapper
```

### StarDriveGame.Initialize() (line 82–98)

1. `Instance = this` (singleton)
2. `Window.Title = "StarDrive BlackBox"`
3. `ResourceManager.InitContentDir()`
4. `ScreenManager = new(this, Graphics)`
5. `InitializeAudio()`
6. `ApplyGraphics(GraphicsSettings.FromGlobalStats())`
7. `base.Initialize()`

### CLI Arguments

`--mod="<path>"`, `--export-textures`, `--export-meshes=obj/fbx`, `--generate-hulls`, `--generate-ships`, `--fix-roles`, `--run-localizer=[0-2]`, `--resource-debug`, `--asset-debug`, `--console`, `--continue`

---

## 4. Game Loop

**Implementation**: `Microsoft.Xna.Framework.Game/Game.cs → Tick()` (line 214–283)

### Fixed Timestep (default)

- Target: **60 FPS** (TargetTickInterval = 1/60 ≈ 16.67 ms)
- Accumulates elapsed time; runs one or more fixed Update steps when elapsed ≥ target
- Capped at 100 ms max accumulated to prevent spiral-of-death
- Single `DrawFrame()` per tick regardless of update count

### Per-Frame Pipeline

```
Tick()
  ├─ if inactive → Sleep(20ms)
  ├─ Accumulate elapsed time
  ├─ while accumulated >= target: Update(targetInterval)
  └─ DrawFrame()
       ├─ BeginDraw()      # GraphicsDeviceManager prepares device
       ├─ Draw()           # ScreenManager.Draw()
       └─ EndDraw()        # Present to display
```

### StarDriveGame.Update() (line 139)

```
GameAudio.Update()
UpdateGame(deltaTime)          # inherited from GameBase
if ScreenManager.NoScreens → Exit()
```

### Multithreading

- **Main thread only** for rendering (validated via `GameBase.MainThreadId`)
- Screens can be added from background threads via `SafeQueue<GameScreen>` → applied next frame
- `Ship_Game/Threading/Parallel.cs` — custom thread pool for background loading tasks

---

## 5. Systems Reference

### 5.1 Rendering

**Pipeline**: SunBurn (3D lighting/shadows) + XNA SpriteBatch (2D UI) hybrid

#### Key Classes

| Class | File | Role |
|---|---|---|
| `ScreenManager` | `GameScreens/ScreenManager.cs` | Owns LightingSystemManager, SpriteBatch, SceneInterface |
| `GameBase` | `GameScreens/GameBase.cs` | GraphicsDeviceManager, applies shadow/anisotropy/SunBurn preferences |
| `DeferredRenderer` | `Graphics/DeferredRenderer.cs` | Deferred 3D rendering |
| `BloomComponent` | `Graphics/BloomComponent.cs` | Post-process bloom |
| `ParticleManager` | `Graphics/Particles/ParticleManager.cs` | Particle effects with batched vertex buffers |
| `SpriteRenderer` | `SDGraphics/Sprites/SpriteRenderer.cs` | Custom 2D batched sprite rendering |
| `TextureAtlas` | `SpriteSystem/TextureAtlas.cs` | Auto-packed texture atlases |
| `StaticMesh` | `Data/Mesh/StaticMesh.cs` | 3D mesh rendering via SDNative + SunBurn Effects |

#### SunBurn Integration

SunBurn namespaces used throughout codebase (409 files import `Microsoft.Xna.Framework`):

- `SynapseGaming.LightingSystem.Core` — core lighting management
- `SynapseGaming.LightingSystem.Lights` — light definitions
- `SynapseGaming.LightingSystem.Rendering` — render integration
- `SynapseGaming.LightingSystem.Effects.Forward` — forward rendering effects
- `SynapseGaming.LightingSystem.Shadows` — shadow maps

**Critical SunBurn callsites**:
- `ScreenManager`: `LightingSystemManager`, `SceneInterface.ApplyPreferences()`
- `StaticMesh`: SunBurn Effects for per-mesh rendering
- `DeferredRenderer`: `SceneInter.CreateDefaultManagers(useDeferredRendering, usePostProcessing)`

#### 2D Sprite Pipeline

`SpriteRenderer` → `DynamicSpriteBatcher` → `SpriteShader` → GPU

---

### 5.2 UI

**Architecture**: Stack-based screen manager with layered component hierarchy

#### Screen Manager (`GameScreens/ScreenManager.cs`)

- `GameScreens[]` array — active screens
- `PendingScreens` — thread-safe addition queue
- Each frame: Update all screens → Draw all screens → dispatch input to top screen

#### Base Classes

| Class | File | Notes |
|---|---|---|
| `GameScreen` | `GameScreens/GameScreen.cs` | Base for all screens (~940 lines). Implements IDisposable. |
| `MultiLayerDrawContainer` | `UI/MultiLayerDrawContainer.cs` | Layered rendering, draw-order management |

#### Notable Screens

| Screen | Lines | Purpose |
|---|---|---|
| `UniverseScreen` | — | Star map / strategic view |
| `ColonyScreen` | 2,017 | Planet management |
| `ShipDesignScreen` | — | Ship design |
| `DiplomacyScreen` | — | Diplomacy / trade |
| `CombatScreen` | — | Real-time combat |
| `GameLoadingScreen` | — | Main menu / loading |
| `InfiltrationScreen` | — | Espionage operations |

#### UI Layout

- `LayoutExporter` / `LayoutParser` — XML-based UI layout definitions
- `RelPos` / `RelSize` — relative positioning
- `UIEffect` / `AnimationCurve` — animation and easing

---

### 5.3 Audio

**Backend**: NAudio (Windows audio, no FMOD)

| Class | File | Role |
|---|---|---|
| `GameAudio` | `Audio/GameAudio.cs` | Central singleton; Update() called every frame |
| `AudioHandle` | `Audio/AudioHandle.cs` | Handle to a live audio instance |
| `NAAudioPlaybackEngine` | `Audio/NAudio/NAudioPlaybackEngine.cs` | NAudio backend |
| `CachedSoundEffect` | `Audio/NAudio/CachedSoundEffect.cs` | In-memory audio cache |

Supports WAV, OGG, MP3. Volume via `MusicVolume` / `EffectsVolume` (0–100).  
**No XNA audio dependency** — clean migration path.

---

### 5.4 Input

| Class | File | Role |
|---|---|---|
| `InputState` | `Input/InputState.cs` | Keyboard, mouse, gamepad state; key bindings |
| `InputState_DoubleClick` | `Input/InputState_DoubleClick.cs` | Double-click detection |
| `InputState_Holding` | `Input/InputState_Holding.cs` | Long-press tracking |
| `UniverseKeys` | `Input/UniverseKeys.cs` | Universe screen hotkeys |

**Flow**: `ScreenManager.input` updated each frame → top `GameScreen.HandleInput(input)` → propagates down

---

### 5.5 AI

#### Empire AI (`Ship_Game/AI/EmpireAI/`)

| Class | Role |
|---|---|
| `EmpireAI` | Top-level empire decision-making |
| `ThreatMatrix` / `ThreatCluster` | Threat assessment and enemy clustering |
| `MilitaryPlanner` / `WarPlanner` | War strategy and goal generation |
| `DiplomaticPlanner` | Trade and relationship management |
| `ResearchPlanner` | Technology selection |
| `EconomicPlanner` | Production and budget planning |
| `ShipBuilder` | Construction planning |
| `DefensiveCoordinator` | Military defense coordination |

#### Ship AI (`Ship_Game/AI/ShipAI/`)

| Class | Lines | Role |
|---|---|---|
| `ShipAI` | 915 | Individual ship behavior (combat, movement, trade, goals) |
| `CombatMovement` | — | Combat maneuvers: Evade, BroadSides, OrbitTarget, Artillery |
| `ShipAIPlan` | — | AI behavior plan abstraction |

#### Pathfinding (`Ship_Game/PathFinder/`)

- `Astar.cs` — A* implementation
- `Node` / `NodeVector` — graph nodes

#### Other AI

- `DroneAI` — drone swarm behavior
- `MissileAI` — smart missile homing
- `SystemCommander` — system-level decisions

---

### 5.6 Networking

**Status: Not present.** The game is single-player only. References to "spy network" and "projection network" in code are game-design concepts, not networking infrastructure.

---

### 5.7 Save / Load

**Format**: Custom binary serializer (not XML, not JSON)

| Class | File | Role |
|---|---|---|
| `ResourceManager` | `Data/ResourceManager.cs` (2,191 lines) | Content pipeline: loads XML definitions, manages texture/mesh caches |
| `BinarySerializerReader/Writer` | `Data/Binary/` | Custom binary I/O engine |
| `ObjectStateMap` | `Data/Binary/ObjectStateMap.cs` | Object graph tracking during serialization |
| `GameContentManager` | `Data/GameContentManager.cs` | XNA ContentManager wrapper with mod routing |
| `RawContentLoader` | `Data/RawContentLoader.cs` | Loads PNG, FBX, OBJ (bypasses XNB pipeline) |

**Save location**: `%APPDATA%/StarDrive BlackBox/Saved Games/`

#### Content Pipeline (3 layers)

```
Request for asset
  └─ GameContentManager (mod-aware routing)
       ├─ If mod override exists → load from mod directory
       ├─ RawContentLoader (PNG/FBX/OBJ)
       └─ XNA ContentManager (XNB compiled assets)
```

---

### 5.8 Modding

| Class | File | Role |
|---|---|---|
| `ModManager` | `Ship_Game/ModManager.cs` | Mod discovery and loading |
| `ModEntry` | `Ship_Game/ModEntry.cs` | Mod metadata |
| `ModInformation` | `Ship_Game/ModInformation.cs` | Mod info display |

Mods override vanilla content via `GlobalStats.ModPath`. `GameContentManager` routes asset requests to mod directory first. `ResourceManager` merges vanilla + mod data.

**Moddable content**: Ships (XML → .design), Hulls (XML → .hull), Technologies, Buildings, Races, Textures, Models, Data files.

---

## 6. Native C++ Integration (SDNative)

**Binary**: `game/SDNative.dll`  
**Source**: `SDNative/` (vcxproj, C++20, VS2022 toolset v143)  
**Current platform**: Win32 (32-bit) — **must be rebuilt as x64 for migration**

### Components

| Module | Location | Purpose |
|---|---|---|
| Mesh system | `SDNative/SdMesh/` | Mesh data structures, materials, skeletal animation |
| FBX import | `SDNative/3rdparty/fbxsdk/` | Model loading via Autodesk FBX SDK |
| Spatial | `SDNative/spatial/` | Collision detection, cell-based structures |
| NanoMesh | `SDNative/NanoMesh/` | Mesh parsing/processing |
| Ship serializer | `SDNative/ShipDataSerializer.h/.cpp` | Binary ship data I/O |

### P/Invoke Interface

**Key interop file**: `Ship_Game/Data/Mesh/MeshInterface.cs`

```csharp
[DllImport("SDNative.dll")] protected static extern unsafe
    SdMesh* SDMeshOpen([MarshalAs(UnmanagedType.LPWStr)] string fileName);

[DllImport("SDNative.dll")] protected static extern unsafe
    void SDMeshClose(SdMesh* mesh);

[DllImport("SDNative.dll")] protected static extern unsafe
    SdMeshGroup* SDMeshGetGroup(SdMesh* mesh, int groupId);
```

Marshaled structs use `LayoutKind.Sequential, Pack=4` with raw `byte*`, `ushort*`, `SdVertexElement*` pointers. `AllowUnsafeBlocks = true` required.

---

## 7. Third-Party Libraries

| Library | Version | Status | Migration |
|---|---|---|---|
| Microsoft.Xna.Framework | 3.1.0.0 (32-bit) | Active — shipping DLL | **Replace with MonoGame** |
| XNAnimation | 0.7.0.0 (32-bit) | Active — animation | Replace or port to MonoGame |
| SynapseGaming SunBurn Pro | Unknown (decompiled) | Active — lighting/shadows | **Replace with MonoGame pipeline** |
| NAudio | Current | Active — audio backend | Keep — no XNA dependency |
| FBX SDK | 3rdparty | Active (via SDNative) | Keep — rebuild for x64 |
| libpng16 | 3rdparty | Active (via SDNative) | Keep — rebuild for x64 |
| System.Text.Json | 7.0.0.2 | Active | Keep |
| IsExternalInit | 1.0.3 | Active | Keep |

**No existing MonoGame references** found in the codebase.

---

## 8. Technical Debt & Code Smells

### God Classes

| File | Lines | Issue |
|---|---|---|
| `GameText.cs` | 5,893 | Localization strings — acceptable as generated |
| `Fleet.cs` | 2,882 | Needs decomposition into focused components |
| `Empire.cs` | 2,778 | Mixed data/logic, needs component split |
| `ResourceManager.cs` | 2,191 | Content pipeline + caching + mod handling — overloaded |
| `ColonyScreen.cs` | 2,017 | UI + game logic mixed |
| `Ship.cs` | 1,908 | Ship entity, rendering, behavior all in one |
| `Relationship.cs` | 1,834 | Diplomatic state + AI in one class |
| `Planet.cs` | 1,676 | Data + rendering + simulation |
| `DxtReader.cs` | 1,620 | Large but focused — acceptable |
| `ShipModule.cs` | 1,611 | Module data + simulation — borderline |

### Tight Coupling

- **SunBurn**: Imported via `SynapseGaming.*` namespaces throughout `Ship_Game/Graphics/` and `GameScreens/`; `LightingSystemManager` owned by `ScreenManager`
- **XNA**: 409 files import `Microsoft.Xna.Framework` — broad blast radius for migration
- **Static singletons**: `GlobalStats`, `ResourceManager` static collections, `GameBase.Base`

### 32-Bit Assumptions

- All `.csproj` files target `x86`
- `SDNative.dll` is Win32 only
- Unsafe struct layouts assume 4-byte pointers (`Pack=4`)
- LargeAddressAware flag is a workaround, not a solution

### Unsafe Code

- `MeshInterface.cs` — extensive raw pointer manipulation for vertex/index data
- `ImageUtils.cs` — P/Invoke for image processing
- Vertex buffer management — direct `byte*` pointer arithmetic

### Hard Migration Areas (SunBurn)

1. `LightingSystemManager` in `ScreenManager` — core of 3D render loop
2. `StaticMesh` — SunBurn Effects for every mesh draw call
3. `DeferredRenderer` — `SceneInterface.CreateDefaultManagers()`
4. Shadow system — `SynapseGaming.LightingSystem.Shadows` used throughout

---

## 9. Migration Roadmap: XNA + SunBurn → MonoGame

### Goals

- Remove 32-bit constraint (x86 → AnyCPU/x64)
- Replace XNA 3.1 with MonoGame
- Replace SunBurn with MonoGame-compatible rendering
- Rebuild SDNative.dll as 64-bit

### Effort Estimate

| Area | % Effort | Key Risk |
|---|---|---|
| SunBurn replacement (rendering pipeline) | ~40% | No 1:1 MonoGame equivalent; custom shaders needed |
| SDNative 64-bit rebuild | ~25% | FBX SDK x64, pointer size changes in marshaling |
| Content pipeline refactor | ~15% | XNB → MonoGame pipeline; mod routing |
| XNA API surface replacement | ~15% | Framework wrapper, device management, ContentManager |
| Test coverage | ~5% | Existing UnitTests baseline |

### Easy Wins (low coupling to XNA/SunBurn)

- `SDUtils` — zero XNA dependency, no changes needed
- `Audio` — NAudio backend, no XNA dependency
- `Input` — thin wrapper, straightforward swap
- `UI` — custom framework, adaptable
- `SDGraphics` math — can align with MonoGame math types

### Blockers

1. **SunBurn**: No MonoGame equivalent. Options:
   - Port SunBurn shaders to MGFX (MonoGame Effect format)
   - Replace with a custom deferred renderer using MonoGame's Effect pipeline
   - Evaluate MonoGame.Extended or similar community libraries for post-processing

2. **SDNative (32-bit C++)**: Must rebuild as x64:
   - Change vcxproj platform from Win32 to x64
   - Rebuild FBX SDK for x64 (Autodesk provides x64 SDK)
   - Update all `Pack=4` struct layouts to `Pack=8` or let the runtime decide
   - Re-validate all P/Invoke signatures

3. **XNA ContentManager / XNB files**: MonoGame uses MGCB (MonoGame Content Builder):
   - Existing XNB files may be compatible or need recompilation via MGCB
   - `RawContentLoader` (PNG/FBX direct loading) is already a good pattern — expand it

4. **Microsoft.Xna.Framework.Game custom wrapper**: Replace entirely with MonoGame's `Game` class

### Suggested Migration Order

```
Phase 1: Foundation
  1a. Change platform target to x64 / AnyCPU
  1b. Rebuild SDNative.dll as x64
  1c. Replace Microsoft.Xna.Framework.Game with MonoGame Game class
  1d. Verify game starts with MonoGame, even if rendering is broken

Phase 2: Rendering Core
  2a. Remove SunBurn dependency from ScreenManager
  2b. Implement basic MonoGame SpriteBatch 2D rendering (UI first)
  2c. Replace SunBurn mesh Effects with custom MGFX shaders
  2d. Port or rewrite DeferredRenderer using MonoGame RenderTarget2D

Phase 3: Content Pipeline
  3a. Migrate XNB assets through MGCB
  3b. Verify RawContentLoader works with MonoGame GraphicsDevice
  3c. Validate mod content routing

Phase 4: Polish & Optimization
  4a. Post-processing (bloom, tone mapping) via MonoGame RenderTarget pipeline
  4b. Particle system vertex buffer compatibility
  4c. Performance profiling and optimization
  4d. Full regression testing
```

---

*This document should be updated as migration phases complete.*
