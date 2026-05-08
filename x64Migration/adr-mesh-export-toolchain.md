# ADR: Mesh-Export Toolchain — Hybrid (legacy primary + migration mirror)

**Status**: Accepted (2026-05-08)
**Phase**: §4.7
**Supersedes**: pending decision pinned in `project_phase4_legacy_mesh_export_sync.md`

## Context

The full mesh export pipeline (`MeshExporter` + `SdMeshGroup` write side) was
written against XNA 3.1's `Model` / `SkinnedModel` / `SgMotion` /
`SunBurn.LightingSystem.Effects` types. None of those types survive on the
migration branch (`migration/phase4-x64-monogame`, net8 + MonoGame 3.8.1.303 +
x64). The migration branch's `MeshExporter.cs` is a **static-mesh-only stub**
adapted in §3.4 step 1; skinned and animated paths were intentionally
deferred. Only the legacy XNA 3.1 + 32-bit toolchain can drive a full corpus
re-export today.

During Phase 4 the legacy export side accumulated five fixes, all on legacy
branches:

| Commit | Subject | Branch | Type |
|---|---|---|---|
| `f964b6df7` | MeshExporter: fix scale + cross-folder texture refs | `xna31` | General fix |
| `5c3a218be` | SdMeshGroup: drop XNA→NanoMesh axis swap on export | `xna31` | General fix |
| `c8c97f35e` | MeshExporter: temp Specular override for CA-derived ship re-export | `ca_patch` | Local override |
| `08879b835` | MeshExporter: add Combined Arms override table + scope export to mod models | `ca_patch` | Local override |
| `dfb730278` | MeshExporter: fix CA path detection for ships at mod-models root | `ca_patch` | Local override |

Phase 3.4 step 5 (`Xna31VertexDeclarationReader` + Model-level XNB reading) is
incomplete — the prerequisite for "option 3, resurrect exporter on migration"
remains unmet.

## Decision

Hybrid of the plan's option (1) and option (2):

1. **`legacy/mesh_exporter_xna31` is the authoritative external re-export
   toolchain.** Stays clean — no project-specific overrides. The two general
   fixes (`f964b6df7`, `5c3a218be`) already live there and are pushed to
   `origin`. Any external mod author re-exporting their content uses this
   branch.

2. **`legacy/mesh_exporter_ca_patch` is the local-only override stack.**
   Stacks the three CA / blackbox specular-override commits on top of
   `xna31`. Never pushed to `origin`. Used only when re-exporting our
   blackbox/CA corpus.

3. **The migration branch mirrors the general fixes** by hand-port (since
   the structural shape of `MeshExporter.cs` differs from legacy and a
   verbatim cherry-pick would not compile). The mirror keeps the migration
   source tree a truthful representation of the export pipeline state. The
   port is gated by the WaE Release|x64 build.

4. **The migration branch deliberately does NOT mirror the CA-specific
   override commits.** Per-ship override insertion is a re-export-time
   workaround for our project's content; it has no runtime role on
   migration and would only confuse readers.

5. **A re-export runbook** (`x64Migration/re-export.md`) captures the legacy
   workflow so future re-exports do not require institutional knowledge.

## Why not option (1) alone?

Option (1) (status quo) would leave the migration source tree drifted from
the export pipeline indefinitely. The drift was already 2 commits at the
start of Phase 4 and grew to 5 during §4.2 / §4.6.B; future divergence
would compound. The hand-port keeps general fixes paired across branches.

## Why not option (3) (resurrect on migration)?

`Xna31VertexDeclarationReader` is partial; skinned-model export needs
significant work to bridge MonoGame's `Model` API to NanoMesh's writer.
The export cadence (a handful of times per phase) does not justify that
investment. Re-evaluate if (a) the legacy branch ever stops building, or
(b) external mod authors need a single-toolchain workflow.

## Why not pure option (2) (cherry-pick verbatim)?

The migration `MeshExporter.cs` is structurally different (Phase 3.4 step
1 dropped `using SgMotion;` / `using SunBurn.LightingSystem.Effects;`).
A verbatim cherry-pick produces a 500-line full-file conflict, and a
"--strategy theirs" resolution would replace the migration's adapted code
with legacy's XNA-only shape and break the WaE-gated build. Hand-port is
the safe path.

## Consequences

- External contributors can clone, switch to `legacy/mesh_exporter_xna31`,
  and re-export their mod content. They never see our project-specific
  overrides.
- We re-export our blackbox/CA corpus on `legacy/mesh_exporter_ca_patch`
  locally; the resulting FBXs land on migration.
- Every general-purpose exporter fix needs **two** edits: one on
  `legacy/mesh_exporter_xna31` and a hand-port on migration. The runbook
  flags this.
- `legacy/mesh_exporter_ca_patch` must be fast-forwarded onto each new
  `xna31` tip when general fixes land, so its override stack stays atop
  the current general fixes.
- If `legacy/mesh_exporter_xna31` ever stops building under future toolchain
  releases (e.g. VS2022 dropping x86 desktop support), that is a §4.7
  reopener.

## Implementation

- `legacy/mesh_exporter_xna31` reset to `origin/legacy/mesh_exporter_xna31`
  tip (`7b5fef051`) — confirmed clean.
- `legacy/mesh_exporter_ca_patch` retained at `dfb730278` as the local
  override stack.
- Migration commit on `migration/phase4-x64-monogame`: surgical port of
  the two general fixes — `MeshExporter.cs` parent-bone walk +
  `MakeRelativePath`, `SdMeshGroup.cpp` axis convention, plus the
  `RawContentLoader` null-check and `GameContentManager` log-level demotion.
- `x64Migration/re-export.md` runbook for the export workflow.
