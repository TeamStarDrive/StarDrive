# Mesh Re-Export Runbook

This runbook explains how to re-export ship and mesh content for BlackBoxPlus.
The migration branch (`migration/phase4-x64-monogame`) is **loader-only** — its
`MeshExporter.cs` is a static-mesh stub adapted for MonoGame 3.8.1.303 and
cannot drive a full re-export. The XNA 3.1 + 32-bit exporter on
`legacy/mesh_exporter_xna31` is the canonical re-export toolchain.

## When to use which legacy branch

| Branch | Purpose | Should I use it? |
|---|---|---|
| `legacy/mesh_exporter_xna31` | Clean general-purpose exporter. Pre-release content + any external mod that needs to round-trip its XNB models through the exporter. | Default for any re-export work. |
| `legacy/mesh_exporter_ca_patch` | Local-only override stack. Stacks per-ship `SpecularOverrides` on top of `xna31` to correct under-spec'd Combined Arms / blackbox ships during `§4.6.B(b)`. | Only when re-exporting our blackbox/CA corpus and you want the override table applied. |

`legacy/mesh_exporter_ca_patch` is a **local-only** branch by design — it is
never pushed to `origin`. The `xna31` branch is what external contributors
fetch. If you need to re-export your own mod content, use `xna31`; the
override table on `ca_patch` is irrelevant to your data.

## Prerequisites

- Visual Studio 2022 with the C++ desktop workload + MSVC toolset (for SDNative x86)
- .NET Framework 4.8 SDK (the legacy runtime; pre-installed on Windows 10/11)
- A working copy with submodules initialized recursively
  (`git submodule update --init --recursive` after switching branches)

## Re-exporting all ships (clean toolchain)

```powershell
# 1. Switch to the clean legacy branch
git checkout legacy/mesh_exporter_xna31
git submodule update --init --recursive

# 2. Build SDNative x86 (Debug or Release; Debug is faster to rebuild during iteration)
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    SDNative\SDNative.vcxproj -p:Configuration=Debug -p:Platform=x86

# 3. Build StarDrive.csproj (legacy supports x86 only — there is no x64 platform on this branch)
dotnet build StarDrive.csproj -c Debug -p:Platform=x86

# 4. Run the exporter
.\game\StarDrive.exe --export-meshes=fbx
```

The exporter walks `game/Content/Model/SpaceObjects/` plus the broader
`Effects/`, `mod models/`, and `Model/` trees that the legacy
`RawContentLoader.ExportAllXnbMeshes` enables. Output lands under
`game/MeshExport/` mirroring the input layout. Both `.fbx` and `.obj` are
emitted; `.dds` textures and `.mtl` material refs are written alongside.

## Re-exporting our blackbox/CA corpus (with spec overrides)

```powershell
git checkout legacy/mesh_exporter_ca_patch
git submodule update --init --recursive
# build + run as above
```

`MeshExporter.cs` on `ca_patch` carries two override tables —
`VanillaOverrides` (19 entries under `Model/Ships/`) and
`CombinedArmsOverrides` (98 entries under `mod models/`) — and routes via the
`mod models` path segment. Each applied override logs in yellow:

```
[SpecularOverride] LightCruiser mat=mat0 → 0.0938 (was 0.0750)
[SpecularOverride/CA] shipyard mat=mat0 → 0.2188 (was 0.0097)
```

The override-table data is mirrored in `x64Migration/specular-overrides.md`
on the migration branch for human reference. The encoded source-of-truth is
the `ca_patch` branch's `MeshExporter.cs`.

## Copying the corpus back to migration

```powershell
git checkout migration/phase4-x64-monogame
# Mirror new .fbx / .obj / .dds / .mtl into game/Content/Model/...
# (existing layout is preserved by the exporter; copy-with-overwrite is safe)
```

The migration branch loads the resulting FBXs through the §3.10 import path
(`SDMesh` + `NanoMesh` 64-bit). `UnitTests/Graphics/FbxMaterialSurveyTests`
will re-survey the freshly exported corpus on the next build — re-run it to
confirm the new values reached the FBX.

## How to extend the override table

If a CA-tree ship surfaces with a regressed material value, add an entry to
`CombinedArmsOverrides` in `Ship_Game/Data/Mesh/MeshExporter.cs` on
`legacy/mesh_exporter_ca_patch`, re-run the export, copy the FBX into the
migration branch, and update `x64Migration/specular-overrides.md` to reflect
the new entry.

If the regression also affects external (non-CA) mods using the same
diffuse-texture cluster, the *general* fix belongs on
`legacy/mesh_exporter_xna31` — but a per-ship override does not. Keep
`xna31` clean for external contributors.

## How to keep the branches healthy

- Both legacy branches must continue to build under VS2022's C++ toolset and
  .NET Framework 4.8 SDK. If a future SDK release removes 32-bit toolchain
  support, that's a §4.7 reopener — re-evaluate option (3) (resurrect
  exporter on migration).
- General-purpose exporter improvements made on `xna31` should be
  fast-forwarded into `ca_patch` so the override stack stays atop the
  current general fixes.
- General-purpose improvements should also be hand-ported to migration's
  `MeshExporter.cs` / `SdMeshGroup.cpp` so the migration source tree
  stays a faithful mirror of the export pipeline. The migration build is
  WaE-gated on `Release|x64`, so any port must compile clean.
