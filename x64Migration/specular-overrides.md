# Specular Override Table

Corrected `SpecularFactor` values for ships whose original FBX export captured an under-spec'd value. Applied in two waves on 2026-05-08 via temp tables (`MeshExporter.VanillaOverrides` + `MeshExporter.CombinedArmsOverrides`) on the `legacy/mesh_exporter_ca_patch` branch.

Inheritance rule: for each affected ship, take the **median Specular of vanilla blackbox ships sharing the same diffuse texture cluster**. Where the texture cluster has no vanilla equivalent (Yamamoto, TypeXIX), pin to the survey-wide median ~0.18.

Surveys (migration branch) in [`UnitTests/Graphics/FbxMaterialSurveyTests.cs`](../UnitTests/Graphics/FbxMaterialSurveyTests.cs):

| Method | Output | Purpose |
|---|---|---|
| `DumpAllShipFbxMaterials`            | `c:\tmp\fbx-specular-survey.csv`        | Vanilla `game/Content/Model/Ships/**`            |
| `DumpCombinedArmsFbxMaterials`       | `c:\tmp\fbx-specular-survey-ca.csv`     | CA `game/Mods/Combined Arms/**`                  |
| `ProposeCombinedArmsSpecularOverrides` | `c:\tmp\fbx-ca-proposed-overrides.csv` | Joins CA → vanilla cluster medians + faction fallback |

## Wave 1 — vanilla blackbox tree

| Ship FBX | Texture cluster | Was → Now |
|---|---|---|
| `Cordrazine/Cordrazine_Station.fbx`             | ship16            | 0.0469 → **0.0938** |
| `Draylok/Draylok_Station.fbx`                   | ship18            | 0.0438 → **0.0625** |
| `Kulrathi/Kulrathi_Station.fbx`                 | ship12            | 0.0500 → **0.1875** |
| `Kulrathi/Kuma Naka.fbx`                        | ship12            | 0.0500 → **0.1875** |
| `Kulrathi/Kuma Oki.fbx`                         | ship12            | 0.0500 → **0.1875** |
| `Kulrathi/Kuma Sukoshi.fbx`                     | ship12            | 0.0500 → **0.1875** |
| `Kulrathi/Yamamoto.fbx`                         | SciFi_Ship_Escort (CA-only) | 0.0156 → **0.1875** |
| `Opteris/OpterisStation.fbx`                    | ship19            | 0.0156 → **0.0938** |
| `Pollops/Pollops_Station.fbx`                   | ship15            | 0.0188 → **0.2188** |
| `Ralyeh/Ralyeh_Station.fbx`                     | ship17            | 0.0156 → **0.2188** |
| `Remnant/SharedTextures/AncientFrigate.fbx`     | ship09            | 0.0097 → **0.1875** |
| `Remnant/SharedTextures/Behemoth.fbx`           | ship09            | 0.0098 → **0.1875** |
| `Remnant/SharedTextures/RemnantPortal.fbx`      | ship09            | 0.0098 → **0.1875** |
| `Terran/SharedTextures/LightCruiser.fbx`        | ship10 (Battleship-equivalent) | 0.0156 → **0.0938** |
| `Vulfen/TypeWI.fbx`                             | ship13            | 0.0313 → **0.2188** |
| `Vulfen/TypeWII.fbx`                            | ship13            | 0.0313 → **0.2188** |
| `Vulfen/TypeWIII.fbx`                           | ship13            | 0.0313 → **0.2188** |
| `Vulfen/TypeXIX.fbx`                            | ship14_d2 (CA-only variant) | 0.0116 → **0.1875** |
| `Vulfen/Vulfar_Station.fbx`                     | ship14            | 0.1250 → **0.2188** |

## Vanilla cluster reference

The reference values these inherit from. From `c:\tmp\fbx-specular-survey.csv` filtered to vanilla ships only — most clusters are uniform across all sharing ships.

| Texture | Vanilla spec | Notes |
|---|---|---|
| ship04  | 0.2188 | uniform (3 ships)             |
| ship09  | 0.1562 / 0.1875 | split — picked 0.1875 for Remnants |
| ship10  | 0.0938 / 0.2188×3 / 0.5625 | varied — picked Battleship's 0.0938 for LightCruiser |
| ship11  | 0.1875 | uniform (6)                   |
| ship12  | 0.1875 | uniform (2)                   |
| ship13  | 0.2188 | uniform (3)                   |
| ship14  | 0.1875 / 0.2188 (×2) | nearly uniform — picked 0.2188 |
| ship15  | 0.2188 | uniform (9)                   |
| ship16  | 0.0938 (×7) / 0.1875 (×2) | dominant 0.0938 |
| ship17  | 0.2188 | uniform (9)                   |
| ship18  | 0.0625 | uniform (9)                   |
| ship19  | 0.0938 | uniform (8)                   |

## Wave 2 — Combined Arms mod tree

CA's OBJ→FBX exports went through the same broken `MeshExporter.cs:216` pipeline, plus most CA hulls reuse vanilla blackbox diffuse textures, so the cluster-median rule extends naturally. 101 entries in `MeshExporter.CombinedArmsOverrides`; 90 re-exported FBXs were copied into `game/Mods/Combined Arms/mod models/**` (committed on the CA branch's nested git, not in this repo's tree).

**Path-conditional dispatch.** `LightCruiser.fbx` exists in both trees with different cluster medians (vanilla ship10 → 0.0938, CA Terran fallback → 0.2188), so the override selection must distinguish vanilla vs CA exports by output path. The first attempt used `IndexOf("\\mod models\\")` substring matching and missed CA ships at the mod-models root (`shipyard.fbx`, `Station_Small.fbx`); fixed by walking path segments split on `\` and `/` for an exact `mod models` segment match.

**Exclusions** (CA but not overridden):
- Dauntless faction folder — distinct hull aesthetic, mod-only diffuse maps, owner opted out.
- `ts2_modi.fbx`, `ts3modE.fbx` — Dauntless-naming ships at mod-models root rather than under `Dauntless/`.

**Faction fallback** — for CA ships whose diffuse texture doesn't match any vanilla cluster directly but live under a known faction folder, inherit from that faction's primary vanilla cluster:

| Faction | Inherits from | Median |
|---|---|---|
| Cordrazine | ship16 | 0.0938 |
| Draylok    | ship18 | 0.0625 |
| Kulrathi   | ship12 | 0.1875 |
| Opteris    | ship19 | 0.0938 |
| Pollops    | ship15 | 0.2188 |
| Ralyeh     | ship17 | 0.2188 |
| Vulfar     | ship13 | 0.2188 |
| Vulfen     | ship13 | 0.2188 |
| Terran     | ship10 (cluster median, NOT outliers) | 0.2188 |

**File-specific faction override** (FBX whose faction isn't inferable from path):
- `shipyard.fbx`, `Station_Small.fbx` → Terran (0.2188)

Distribution after copy: 72 ships at 0.2188, 35 at 0.0938, 24 at 0.1875, 18 at 0.0625, plus Dauntless-residue 14 at 15.625 / 11 at 0.0156 (intentionally untouched).

## Re-applying

If these FBXs ever need to be re-generated from XNB sources, both override tables on `legacy/mesh_exporter_ca_patch` reproduce these values automatically. `StarDrive.exe --export-meshes=fbx` writes to:
- `game/MeshExport/Model/Ships/...` — vanilla outputs (Wave 1)
- `game/MeshExport/Mods/Combined Arms/mod models/...` — CA outputs (Wave 2)

Each override logged in yellow `[SpecularOverride]`. Copy:
- Wave 1 outputs → `game/Content/Model/Ships/...` (migration branch tree)
- Wave 2 outputs → `game/Mods/Combined Arms/mod models/...` (CA branch with FBXs)

The exporter's directory walk is scoped to `mod models` only on the legacy branch — `RawContentLoader.ExportAllXnbMeshes` has the other paths commented out — so a single export run covers CA without re-emitting vanilla.

Note: the legacy x86 build clobbers `game/SDNative.dll` with a 32-bit binary; rebuild SDNative-x64 (`MSBuild SDNative/SDNative.vcxproj -p:Platform=x64`) before running migration tests / the game after each export cycle.
