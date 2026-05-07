# Specular Override Table

Corrected `SpecularFactor` values for ships whose original FBX export captured an under-spec'd value. Applied 2026-05-08 via the temp `MeshExporter.SpecularOverrides` table on the `legacy/mesh_exporter_ca_patch` branch (commit `c8c97f35e`).

Inheritance rule: for each affected ship, take the **median Specular of vanilla blackbox ships sharing the same diffuse texture cluster**. Where the texture cluster has no vanilla equivalent (Yamamoto, TypeXIX), pin to the survey-wide median ~0.18.

Surveyed by [`UnitTests/Graphics/FbxMaterialSurveyTests.cs`](../UnitTests/Graphics/FbxMaterialSurveyTests.cs) → output `c:\tmp\fbx-specular-survey.csv`.

## Applied corrections

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

## Re-applying

If these FBXs ever need to be re-generated from XNB sources, the override table on `legacy/mesh_exporter_ca_patch` (commit `c8c97f35e`) reproduces these values automatically — `StarDrive.exe --export-meshes=fbx` writes to `game/MeshExport/Model/Ships/` with each override logged in yellow `[SpecularOverride]`. Copy from `game/MeshExport/Model/Ships/...` over `game/Content/Model/Ships/...` on the migration branch.

Note: the legacy x86 build clobbers `game/SDNative.dll` with a 32-bit binary; rebuild SDNative-x64 (`MSBuild SDNative/SDNative.vcxproj -p:Platform=x64`) before running migration tests / the game after each export cycle.
