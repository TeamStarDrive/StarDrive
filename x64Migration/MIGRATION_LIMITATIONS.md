# Migration Limitations

Architectural differences between the original XNA 3.1 + SunBurn pipeline and
the migrated MonoGame 3.8.1 forward pipeline that produce visible (or
visible-in-edge-cases) deltas relative to the legacy build. Each entry below
documents a known cap or behavior change carried by the migration so the
end-of-project audit has a single place to consult — and so future
contributors don't waste cycles diagnosing them as bugs.

Entries follow a consistent shape:
- **Limit** — what is constrained relative to the legacy build.
- **Why** — the architectural reason (engine delta, hardware target, etc.).
- **Impact** — when a player would actually notice.
- **Path to remove** — the work that'd be required to lift it.

---

## Dynamic projectile / explosion lights capped at 2 simultaneous

Phase 4.6 §2.

**Limit.** Only the 2 dynamic point lights closest to the camera (XY distance)
tint nearby ship hulls at any moment. Pre-migration SunBurn ran an unbounded
queue (`MaxDynamicLightSources = 100` in `app.config`).

**Why.** SunBurn was a **deferred** renderer — every mesh wrote a G-buffer
once, then each submitted light drew its bounding volume into a lighting
accumulator. Cost was O(visible_lights × screen_pixels_in_volume), no
per-shader slot cap; small projectile lights paid only for their tiny
on-screen footprint.

The migrated pipeline is **forward**: lighting math runs inside each mesh's
pixel shader, so every light has to be pre-bound as shader constants. Targeting
`ps_4_0_level_9_3` (FL9.3 — pre-2008-class GPU compatibility) caps the PS
const float pool at 32 vec4 registers. That budget is mostly consumed by the
3 sun PointLight slots, 3 directional lights, ambient, World/View/Projection,
LightViewProjection, ShadowParams, fog, and material/map flags. Even after
packing the 5 PointLight slots into `PositionAndRadius` + `DiffuseAndEnabled`
float4 pairs and dropping their specular contribution, only 2 dynamic slots
fit.

**Impact.** Bolt **sprites** still draw for every projectile (they're textures,
not light-dependent), so all bolts remain visible regardless. Only the
*hull-tinting glow* is limited. In a busy fleet engagement with 6+ bolts in
flight near the camera, the closest 2 will tint nearby hulls and the others
won't. Bloom on the bright sprites carries enough of the visible signal that
the eye still reads "lots of glowing bolts."

**Path to remove.** In ascending order of work:
1. **4 slots:** drop a sun PointLight (probably `OverSaturationKey` —
   small-radius near-sun oversaturator) and reuse the freed registers.
2. **4 slots, no sun loss:** also pack the 3 directional lights tighter
   (currently 9 registers — 3 vec3 fields × 3 — could pack to ~6).
3. **8+ slots:** bump `ps_4_0_level_9_3` → `ps_4_0`, getting the full DX10
   const buffer (~4096 registers) but losing FL9.3 hardware compat.
4. **Unbounded:** clustered / Forward+ shading — bin lights into screen-space
   tiles, dispatch a per-tile light list. Right answer if the count ever
   needs to scale into the hundreds.

---

## Point-light specular contribution dropped

Phase 4.6 §2 — same register-budget fix as the dynamic-light cap above.

**Limit.** Sun-anchor PointLights (Key / LocalFill / OverSaturationKey) and
dynamic lights (projectile glow, explosion flash, shield impact) no longer
contribute specular highlights. Only the 3 DirectionalLight slots do.

**Why.** Each PointLight slot used to declare `float3 SpecularColor` —
1 const float register per slot. Dropping it freed 3 registers from the sun
slots and let dynamic slots fit at all. Sun PointLights are large area-light
proxies (Radius ≥ 1000), not glint sources of geometric significance, so
their specular was already a faint diffuse-boost (`q.DiffuseColor * 0.15` per
the binder) rather than a true highlight.

**Impact.** Chrome / metallic ship panels near an in-system sun read slightly
flatter — directional Key + Global Fill carry the highlight read, but the
PointLight oversaturator no longer adds the extra glint. Visible on
high-spec_map hulls under bright suns; invisible elsewhere.

**Path to remove.** Bound up with the dynamic-light cap above. Any path that
expands shader register headroom (drop a sun slot, pack directionals,
upgrade to `ps_4_0`) lets PointLight specular come back. In particular
"option 2" of the dynamic-light path naturally restores it.

---

## Single shadow caster

Phase 3.8.B.

**Limit.** Only one light per frame casts shadows: the dominant directional
or sun-anchor PointLight, picked by [LightingEffectBinder.TryPickShadowDirection](Ship_Game/Data/Mesh/LightingEffectBinder.cs).
SunBurn's deferred path could cast shadows from any number of submitted
lights (each light volume optionally carried its own shadow render target).

**Why.** Shadow casting requires a depth pre-pass per shadow-casting light
— bind a render target, draw all casters from the light's POV, then sample
that depth in the lit pass via `LightViewProjection`. Each additional
caster costs a full caster-set draw + an additional shader sampler. The
migration ships with a single shadow `Texture2D` + `LightViewProjection`
matrix uniform on `LightingEffect`; multi-caster would need an array of
each plus a per-light loop in the PS, which doesn't fit the FL9.3 budget.

**Impact.** A scene illuminated by both a directional sun and a near-sun
oversaturator only shows shadows from the directional. Multi-source
shadowing (a planet eclipsed by another planet, a station shaded by both
sun and a nearby ship's PointLight) doesn't render.

**Path to remove.** Add a second shadow RT + LightViewProjection slot,
extend the depth pre-pass to iterate the top-N casters, and add a second
sampler + matrix to the PS. Mostly mechanical; scope-limited by the same
shader-register cap as the dynamic lights.

---

## Hard 1-tap shadow edges (no PCF)

Phase 3.8.B (deferred soft-shadow pass to §3.8.C, which never landed).

**Limit.** Shadow comparison samples a single texel from the shadow map
(`MinFilter = Point` — see [MeshLighting.fx:78](game/Content/Effects/MeshLighting.fx#L78)) and
returns hard 0/1 occlusion. SunBurn's deferred shadows used multi-tap PCF
for soft edges.

**Why.** §3.8.B was the MVP — depth pre-pass + 1-tap sampling — to get any
shadow at all. PCF (3×3 or 5×5 weighted samples around the projected UV)
was scoped for §3.8.C and not picked up before §4 began.

**Impact.** Shadow edges appear slightly aliased / stair-stepped, especially
on shallow grazing surfaces (planet terminators, hull panels lit at low N·L).
No penumbra / soft falloff.

**Path to remove.** Add a 5- or 9-tap PCF kernel to `SampleShadowFactor` in
`MeshLighting.fx` + `SkinnedEffect.fx`. Constant cost; no register impact.
Could land at any time without architectural change.

---

## No HDR tone-mapped composite

Phase 4.6 §10.

**Limit.** Final image is LDR throughout. Bloom is a 4-pass extract /
gaussian / combine post-process, but the underlying scene buffer never
goes through an HDR tone curve — the lit pass writes 8-bit color directly.
SunBurn's deferred composite included a tone-mapping pass that attenuated
saturated channels into a perceptually balanced LDR output.

**Why.** Bloom + LDR forward was the cheap path to get something
shippable. A real HDR composite would need a half-float scene RT, a
luminance-extraction pass for adaptive exposure, and a tone curve
(Reinhard / ACES / similar) before the bloom combine. Bigger scope than
§4.6 budget.

**Impact.** This is the **root cause** of the §4.6 #10 Universe-screen
green wash. The Clouds.fx PS routes macro noise to the green channel; the
cyan filter zeros R; without a tone curve to attenuate over-bright
channels back into the displayable range, the unbounded green saturated
the screen. Workaround: dimmed the cyan filter from 100% → ~19%
([Background.cs](Ship_Game/Universe/Background.cs)). Symptom resurfaces wherever a
shader writes a channel value > 1.0 expecting a tone curve to pull it back.

**Path to remove.** Sizable: half-float MainTarget, luminance reduction
chain, tone-mapping shader before the bloom combine, then re-tune every
emissive / additive draw against the new dynamic range. Likely a phase of
its own.

---

## 3 fixed PointLights per system anchor

Phase 2.8.

**Limit.** Each star system contributes exactly 3 PointLights to the scene:
Key (full radius scene light), LocalFill (full radius white fill), and
OverSaturationKey (small radius sun-pixel oversaturator). Hardcoded in
[UniverseScreen.cs:271-273](Ship_Game/Universe/UniverseScreen/UniverseScreen.cs#L271-L273). SunBurn allowed any
number of lights per system, configured via content (`.lightRig` or runtime
composition).

**Why.** Forward shader has 3 PointLight slots. The binder fills them all
from the closest system's lights ([LightingEffectBinder.cs:124-159](Ship_Game/Data/Mesh/LightingEffectBinder.cs#L124-L159));
extra lights would silently drop. The 3 slots are a deliberate match of the
SunBurn-era convention so the lighting reads identically to the original
when the layout matches.

**Impact.** Modders or content authors can't add a fourth light per system
(e.g., a binary-star Secondary). The 3 hardcoded lights are also chosen
globally per active system — multi-system shots (deep-space camera with
two systems in frame) only light from the one closest in XY.

**Path to remove.** Bound to the same shader-register expansion as the
dynamic-light cap. Free 1+ slot from the directional / fog / packed
material state and the binder can fill more sun lights. Or per-mesh light
binning (closer to clustered shading).

---

## No `LightRig` content pipeline

Phase 4.5.B.

**Limit.** SunBurn's `.lightRig` content type — which carried per-scene
light configurations (positions, radii, intensities, sub-fixtures for
stations / hangars / etc.) — is a dataless stub class in the migration
([SunBurnStubs.cs](Ship_Game/Data/Mesh/SunBurnStubs.cs)). Its content reader returns an
empty `LightRig` and `LightManager.Submit(LightRig)` is a no-op. The
load+catch path was deleted entirely in §4.5.B because it was always
functionally a no-op.

**Why.** SunBurn's `.lightRig` files are XNA 3.1 XNB blobs whose internal
type graph references SunBurn-Pro classes that the migration replaced with
stubs. Decoding them at runtime would need a full re-implementation of the
SunBurn LightRig reader, plus the data structures behind it. Out of scope
for the migration's "preserve gameplay-visible state" goal.

**Impact.** Stations, hangars, planet surfaces, and any scene that relied
on content-authored light setups now use only the global Universe lighting
+ 3 sun-anchor PointLights. Custom mod content that ships `.lightRig`
files silently does nothing. In practice the affected scenes (e.g., the
ship designer's lit hangar) are visually plausible because the global
scene lighting fills in, but the original deliberate per-scene mood is
gone.

**Path to remove.** Reverse-engineer the SunBurn `.lightRig` XNB schema
(decompile reader from `SynapseGaming-SunBurn-Pro.dll`), implement a
runtime reader that emits `PointLight` / `DirectionalLight` instances into
the migration's `LightManager`. Mostly read-side parser work; the
downstream lighting infra already accepts the resulting light list.

---

## XNA 3.1 model XNB runtime decode unsupported

Phase 3.4.

**Limit.** The runtime cannot decode XNA 3.1-built model XNBs. The entire
ship / station / weapon / projectile mesh corpus was re-exported offline as
FBX (Phase 3.4 onward) and is loaded via `NanoMesh` / `MeshInterface`.
SunBurn-era builds loaded the original XNB files directly through the XNA
content pipeline.

**Why.** XNA 3.1 vertex declarations encode element format + offset in a
binary format that MonoGame's `ContentTypeReader<VertexDeclaration>` doesn't
implement (the schema changed at XNA 4 and MonoGame followed the newer
shape). A partial runtime reader (`Xna31VertexDeclarationReader`) was
prototyped in Phase 2.7.A but never reached production-grade — Phase 3.4
chose the offline FBX export pipeline as the more sustainable path.
Decompile-of-the-prototype lives on `legacy/mesh_exporter_xna31`.

**Impact.** Modders adding new ships have to ship FBX (or OBJ → FBX
through the migration's `mesh_exporter` legacy branch), not the original
XNB workflow that StarDrive shipped with. Mods that distributed XNB-only
content silently fail to load the affected models.

**Path to remove.** Finish the `Xna31VertexDeclarationReader` partial
implementation, or document the FBX pipeline as the supported path. The
former is a meaningful amount of binary reverse-engineering; the latter
is the more pragmatic path and is already in place.

---

## Custom shaders constrained to `ps_4_0_level_9_3`

Phase 2.6.A onward.

**Limit.** All migration `.fx` shaders compile under MGFX 3.8.1 to
`ps_4_0_level_9_3` / `vs_4_0_level_9_3`. PS const float pool capped at 32
vec4 registers; no compute shaders; no Shader Model 5 features (UAVs,
tessellation, structured buffers); 64-instruction loops max. SunBurn-era
XNA `Effect` ran on whatever XNA shipped (typically SM3.0 with looser
register limits and richer parameter binding).

**Why.** FL9.3 is the lowest viable feature level for DX11 forward
rendering, and the project chose it to retain pre-2008-class GPU
compatibility (the StarDrive minimum spec). Bumping to `ps_4_0` gets the
full DX10 const buffer (~4096 registers) and SM4 features but drops
support for FL9.x hardware.

**Impact.** Several migration features had to work around the register cap
— this doc's first two entries (dynamic light cap, point-light specular
drop) are direct consequences. `MeshLighting.fx`'s shadow path uses 1-tap
sampling instead of PCF for the same reason. New visual features
(volumetric fog, screen-space reflections, advanced post-processing) would
need the bigger register / instruction budget.

**Path to remove.** Bump shader profile to `ps_4_0` (DX10 / FL10.0
hardware). Likely fine on any system from ~2008 forward; loses
compatibility with older integrated chipsets that still claimed FL9.3.
Project decision rather than code work.

---

## No environment / IBL / cubemap-based ambient

Phase 2.8 / Phase 3.7.

**Limit.** Ambient lighting on meshes is a flat `AmbientLightColor` vec3
scaled by the diffuse texture. SunBurn supported environment cubemaps for
image-based ambient (cockpit lit by surrounding starfield, hull subtly
tinted by nearby nebula colors).

**Why.** Cubemap-based IBL needs a per-scene environment cubemap RT, a
roughness-mipped pre-filter pass, and a cubemap sampler in the lit shader
for diffuse-and-specular IBL terms. None of that infra was ported — the
migration's `LightingEffectBinder` collapses scene-environment lighting
into a single `AmbientLightColor` vec3 ([LightingEffectBinder.cs:42](Ship_Game/Data/Mesh/LightingEffectBinder.cs#L42)).

**Impact.** Hulls in unusual environments (nebula clouds, near a colored
sun) read with a uniform ambient cast instead of picking up the
surrounding color. Subtle but a recognizable "this looks like StarDrive"
delta on close-up screenshots — the original game read more grounded in
its scene because the environment subtly tinted everything.

**Path to remove.** Implement a per-scene cubemap render (or
artist-authored texture cube), pre-filter for roughness mips, and add a
`SampleCubeLOD`-based ambient term to the PS. Bigger scope than a single
phase; likely belongs alongside any HDR composite work.
