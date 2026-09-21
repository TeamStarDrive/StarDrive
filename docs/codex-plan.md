# Codex Implementation Plan

Status: planned (2026-05-21). Owner: Gilad. Branch: TBD (suggest `feature/codex`).

This plan replaces the existing "BlackBox Info" wiki (HelpTopics_*.xml) with a structured, localized, navigable in-game **Codex** that game systems can deep-link into from tooltips. The plan is staged so each phase is independently shippable.

---

## Why

- Existing `HelpTopics_*.xml` has inline strings per language file → translation drift, fake-translated files (5 of 6 non-English files just contain English content).
- A bug in [InGameWiki.cs:75-86](../Ship_Game/StoryAndEvents/InGameWiki.cs#L75-L86) (`HashSet.Add` returning false on duplicate category names) silently drops every entry beyond the first in any category. Vanilla "BlackBox" category has 7 topics; only "Updates" is visible.
- Flat `Category` string — no nested categories possible.
- No way for in-game tooltips to deep-link into the wiki for context-sensitive help.
- No styling in body text — flat color, single font.

Target state: yaml-driven Codex with localized text via GameText tokens, nested categories, tooltip hooks for context-sensitive deep links (F1), styled markup (color/bold/url) in body text.

---

## Locked design decisions

These are non-negotiable for the implementation; revisit only with explicit user sign-off.

| Decision | Choice | Notes |
|----------|--------|-------|
| Nesting model | Explicit `Children:` list in yaml | Arbitrary depth supported. Cleaner authoring than path-based UIDs. |
| Markup syntax | Named tags: `<color=Caption>...</color>`, `<b>...</b>`, `<url=https://...>...</url>` | Unity-rich-text style, self-documenting. |
| Codex hotkey | **F1** | Universal "help" convention. Requires moving the existing `FTLOverlay` (currently F1) → F3. |
| Token ID range | **100000+** reserved for codex content strings | Vanilla GameText currently maxes at ~18,284. 100000 leaves >80,000 IDs of headroom for non-codex growth and provides a clear visual separator. |
| Storage format | New `Codex.yaml`, all text in `GameText.yaml` | Single source of truth for translations. Codex.yaml is language-independent. |
| Phase order | 1 → 2 → 4 → 3 → 5 | Style (4) before tooltip hooks (3) so authored entries are already rich-text when first wired up. |
| Window size | 1100 × 750 (was 750 × 600) | Tunable in CodexScreen ctor. |
| Class rename | `InGameWiki` → `CodexScreen` | Including all references, the GameText token "StardriveHelp2" → new "CodexTitle". |

---

## Phase 1 — Foundation (yaml + localization migration)

**Goal**: replace HelpTopics XML with `Codex.yaml`; pull all displayable strings into `GameText.yaml`; fix the multi-entry-per-category bug; preserve Russian translations.

**Estimate**: 3–4 hours.

### New schema

`game/Content/Codex.yaml`:

```yaml
# Codex content structure. Strings live in GameText.yaml; this file
# is pure structure + token references.
- UID: blackbox
  TitleId: CodexBlackBox
  Children:
    - UID: blackbox_updates
      TitleId: CodexBlackBoxUpdates
      ShortDescId: CodexBlackBoxUpdatesShort
      TextId: CodexBlackBoxUpdatesText
    - UID: blackbox_releases
      TitleId: CodexBlackBoxReleases
      Children:
        - UID: blackbox_test_dl
          TitleId: CodexBlackBoxTestDl
          ShortDescId: CodexBlackBoxTestDlShort
          TextId: CodexBlackBoxTestDlText
          Link: "https://github.com/TeamStarDrive/CombinedArms/releases"
- UID: combat
  TitleId: CodexCombat
  Children:
    - ...
```

### New data classes

Create `Ship_Game/Codex/CodexEntry.cs`:

```csharp
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Codex
{
    [StarDataType]
    public sealed class CodexEntry
    {
        [StarData] public string UID;
        [StarData] public string TitleId;       // GameText NameId
        [StarData] public string ShortDescId;   // GameText NameId
        [StarData] public string TextId;        // GameText NameId
        [StarData] public string Link;          // external URL (legacy field; prefer <url> tag in body)
        [StarData] public string VideoPath;
        [StarData] public Array<CodexEntry> Children;
    }
}
```

Create `Ship_Game/Codex/Codex.cs`:

```csharp
[StarDataType]
public sealed class Codex
{
    [StarData] public Array<CodexEntry> Entries;
}
```

### Files to modify

| File | Change |
|------|--------|
| `Ship_Game/HelpTopic.cs` | **Delete** (replaced by CodexEntry.cs) |
| `Ship_Game/HelpTopics.cs` | **Delete** (replaced by Codex.cs) |
| `Ship_Game/StoryAndEvents/InGameWiki.cs` | Rewrite: use `YamlParser.Deserialize<Codex>(file)`; categorization loop fixed (`Map<string, WikiHelpCategoryListItem>` keyed by category, always `AddSubItem`); resolve all displayed strings via `Localizer.Token(entry.TitleId)` etc. |
| `Ship_Game/StoryAndEvents/WikiHelpCategoryListItem.cs` | Update to hold `CodexEntry` instead of `HelpTopic` |
| `game/Content/GameText.yaml` | Append migrated strings (~150-ish tokens, IDs 100000+) |
| `Ship_Game/Data/GameText.cs` | Regenerated by localization tool after GameText.yaml is edited |
| `StarDrive.csproj` | Register `Codex.cs`, `CodexEntry.cs`; deregister `HelpTopic.cs`, `HelpTopics.cs` |
| `game/Content/HelpTopics/` | **Delete entire folder** after migration is verified |

### Migration script

Add a one-shot CLI command to the localization tool (similar to `--run-localizer`). Pseudocode:

```
1. Read game/Content/HelpTopics/English/HelpTopics_English.xml
2. Read game/Content/HelpTopics/Russian/HelpTopics_Russian.xml
3. For each <HelpTopic> in English XML:
   - Slug = derive UID (slugify Category + Title, e.g. "blackbox_updates")
   - Allocate three new IDs starting at 100000:
       TitleId,       ENG = topic.Title,           RUS = (matching Russian topic.Title or null)
       ShortDescId,   ENG = topic.ShortDescription, RUS = (matching or null)
       TextId,        ENG = topic.Text,            RUS = (matching or null)
   - Append to GameText.yaml as new LangToken entries with NameId = "Codex" + PascalCase(slug)
   - Emit CodexEntry to in-memory tree (group by Category)
4. Write Codex.yaml from the in-memory tree
5. Match Russian topics by Category+Title; warn on unmatched
```

The script lives in `Ship_Game/Tools/Localization/CodexMigrationTool.cs`, invoked via a new `--migrate-codex` CLI flag in `Ship_Game/GameScreens/Program.cs`. Single-use; can be deleted after the migration commit if you want, or kept as a reference.

### Acceptance criteria

- `dotnet build` clean (0 warnings, 0 errors).
- Launch game → press wiki hotkey → "BlackBox" category appears with **all 7 sub-entries** (not just "Updates").
- Click each → text body shows correct localized content in English.
- Switch language to Russian (via launcher / settings) → Russian topics show Russian text where present, English fallback otherwise.
- `game/Content/HelpTopics/` folder no longer exists.
- `Codex.yaml` parses with hot-reload (file edit while game running → wiki re-loads on next open).

---

## Phase 2 — UX upgrade (rename, resize, nest, OpenAt)

**Goal**: ship the renamed Codex with arbitrary-depth nesting in the category list and a public method for deep-linking. No tooltip hookup yet — that's Phase 3.

**Estimate**: 2 hours.

### Tasks

- Rename `InGameWiki` → `CodexScreen` (file + class).
- GameText token rename `StardriveHelp2` → `CodexTitle` (new token under 100000+).
- Window dimensions: change `PopupWindow(parent, 750, 600)` → `PopupWindow(parent, 1100, 750)`. Adjust internal rect math (CategoriesRect width, TextRect position) — currently hardcoded; rebase off the new size.
- **Nested category rendering**:
  - `WikiHelpCategoryListItem` becomes recursive. Each item knows its depth; render with indentation (e.g., 16px per depth level).
  - `LoadContent` builds the tree by recursing `CodexEntry.Children` rather than the current single-pass flat loop. Replace the buggy `HashSet` logic entirely.
  - Sub-items are added via `ScrollList.AddSubItem` — verify `ScrollList<T>` supports nested sub-items (read its implementation; if not, add support or render the tree as flat indented entries).
- **Public deep-link API**:
  ```csharp
  public void OpenAt(string uid)
  {
      // recurse Codex.Entries, find entry with matching UID,
      // expand all ancestor categories so it's visible,
      // select it (call OnHelpCategoryClicked equivalent).
  }
  ```
  Used by Phase 3.

### Candidate simplification — derive GameText NameIds from UID

**Status:** open question; decide before Phase 5 (content authoring) starts so authors don't write under the heavier schema and have to redo it.

`UID` + `TitleId` + `ShortDescId` + `TextId` carry overlapping information — they're all string identifiers for the same entry. If we adopt a convention (`TitleId = "Codex" + PascalCase(slug)`, `ShortDescId = TitleId + "Short"`, `TextId = TitleId + "Text"`), the three `*Id` fields can be dropped from `Codex.yaml`. Drops ~75% of authoring noise per entry. After:

```yaml
- UID: blackbox_updates
- UID: blackbox_releases
  Children:
    - UID: blackbox_test_dl
      Link: "https://github.com/TeamStarDrive/CombinedArms/releases"
```

Tradeoffs:
- **Pro:** much terser yaml; fewer typos; one renaming touchpoint per entry; convention makes intent obvious.
- **Con:** less explicit (NameIds become implicit); mods can no longer repoint a single text/short/title to a different token without changing the UID (and therefore every tooltip-hook that references it).
- **Migration:** if adopted, regenerate `Codex.yaml` from the existing tree by stripping `*Id` fields; runtime `CodexEntry` keeps `TitleId/ShortDescId/TextId` properties but populates them via a `[OnLoaded]` hook that derives from `UID`.

### Acceptance criteria

- Window visibly larger.
- Title bar reads "Codex" (localized).
- Categories with sub-categories show an expand indicator; clicking expands/collapses.
- Calling `screen.OpenAt("blackbox_test_dl")` from anywhere opens the Codex pre-navigated to that entry with parents expanded.

---

## Phase 4 — Styled markup (color, bold, url, img)

**Goal**: rich text in codex body. Four tag types. Parser + renderer.

**Estimate**: 5–6 hours.

### Tag spec

| Tag | Effect | Example |
|-----|--------|---------|
| `<color=Name>...</color>` | Color span. `Name` is a public field on `CodexStyles`. The `Caption` name is special — it also bumps the span to `CodexStyles.CaptionFont` (heading size) and emits a double line break after the close, so caption text reads as a heading above the body that follows. | `<color=Caption>Range</color>: weapon falloff distance.` |
| `<b>...</b>` | Bold span (renders with bold font variant). | `<b>Warning:</b> overcharge depletes power.` |
| `<url=https://...>display text</url>` | Clickable hyperlink. Renders in `CodexStyles.Url` color (forced — overrides any outer `<color>` for the span and restores on close), underlined. Click → `Log.OpenURL`. | `See <url=https://wiki.example.com>Game Wiki</url> for full table.` |
| `<li>item text</li>` | List item. Draws a dash at the margin and indents the item's text, including every line it wraps onto, past the dash. The item ends at the next line break, so `</li>` is optional. | `<li>Buildings with a population bonus add on top.</li>` |
| `<img>path/to/texture</img>` | Inline image; tag body is a `ResourceManager.Texture` path (no extension). Auto-wraps to a new line **after the last image of a contiguous img group** — see flow rules below. Missing texture renders the global error texture (`ResourceManager.ErrorTexture`, the red X) inline so authors notice the typo at a glance. | `Press <img>UI/icon_research</img> to open the research screen.` |

Tags **must not nest** in v1 (parser bails on the second open before any close). Pre-existing line breaks (`\n`) and Localizer placeholder syntax (`{0}`, etc.) pass through unchanged.

**`<img>` flow rules:**
- An image renders at its texture's natural size and is an **atomic run** — the renderer never splits an image across lines.
- Consecutive `<img>` tags (with only whitespace between them) flow horizontally on the same line, left-to-right, until they exhaust the line's remaining width. Overflow wraps the next image to a new line and the group continues there.
- The renderer inserts a **forced line break after the last image of the contiguous group** so subsequent text starts on a fresh line. No break is inserted before the group — if you want text above the image, end the preceding text run with `\n`.
- If an image's natural width exceeds `bounds.Width`, it still renders (clipped to the right edge) on its own line. Authors should size source textures to fit.
- Missing texture (not resolvable via `ResourceManager.Texture`) → renders `ResourceManager.ErrorTexture` (`NewUI/x_red`) inline at the broken position; logs the missing path once per run. Non-fatal.

### CodexStyles class

`Ship_Game/Codex/CodexStyles.cs`:

```csharp
using Color = Microsoft.Xna.Framework.Color;
using Ship_Game.Graphics;

namespace Ship_Game.Codex
{
    // Central registry of named colors / fonts used by the <color> and <b> tags
    // in Codex body text. Add new entries here, then reference them from yaml-
    // sourced strings via <color=Name>...</color>.
    public static class CodexStyles
    {
        // Body text colors -- add freely
        public static Color Default   = Color.White;
        public static Color Caption   = Color.LightGoldenrodYellow;
        public static Color Highlight = Color.Cyan;
        public static Color Warning   = Color.Orange;
        public static Color Lore      = new Color(180, 180, 200);
        public static Color Url       = new Color(120, 180, 255);

        // Font roles -- bold tag swaps to BoldFont; layout uses Default
        public static Font Default_Font => Fonts.Arial12;
        public static Font Bold_Font    => Fonts.Arial12Bold;
    }
}
```

### Parser

`Ship_Game/Codex/StyledText.cs`:

```csharp
public readonly struct StyledRun
{
    public readonly string Text;       // null for image runs
    public readonly Color Color;
    public readonly bool Bold;
    public readonly string Url;        // null if not a link
    public readonly string ImagePath;  // non-null marks this as an image run; Text/Color/Bold/Url ignored
}

public static class StyledTextParser
{
    public static StyledRun[] Parse(string source) { ... }
}
```

Recommended approach: simple state machine over the string. Track current color, current bold flag, current URL. On `<color=X>` push state, on `</color>` pop. `<img>...</img>` emits a single atomic run (no state push — img can't wrap other markup and other markup can't wrap img). Unknown tags pass through as literal text (defensive; doesn't crash on missing close tags). For unknown color name → fall back to `CodexStyles.Default`.

### Renderer

`Ship_Game/Codex/StyledTextRenderer.cs`:

```csharp
public class StyledTextRenderer
{
    public StyledTextRenderer(RectF bounds);

    // Replaces UITextBox.SetLines for codex body text.
    public void SetText(StyledRun[] runs);

    public void Draw(SpriteBatch batch);
    public bool HandleClick(Vector2 mousePos);  // for url tags
}
```

Layout: walk runs, measure each word with its run's font, wrap on word boundaries within `bounds.Width`. Track per-line url rects for click testing. Underline url runs at draw time (`batch.DrawLine` under the text bounds). Image runs are placed inline as atomic blocks (measure = texture size); the renderer inserts a forced newline after the last image in a contiguous img group (see flow rules above). Per-line height is `max(font line spacing, max image height on that line)`.

This is the most subtle code in the plan. Risks:
- Mixed-font line height (bold may have different metrics than regular)
- Word-break inside a run vs across runs
- Right-edge wrap when a run ends mid-word
- Image-vs-text vertical alignment on mixed lines (images can be taller than text)

Mitigation: hard-cap a single run at 200 chars during parse (split longer runs), use per-line max(font height, image height) for line spacing, wrap on whitespace + explicit `\n`. For mixed image/text lines, align the text baseline to the image's vertical center (or top) — pick one in v1 and document.

### Files

| File | Action |
|------|--------|
| `Ship_Game/Codex/CodexStyles.cs` | Create |
| `Ship_Game/Codex/StyledText.cs` | Create (parser + StyledRun struct) |
| `Ship_Game/Codex/StyledTextRenderer.cs` | Create |
| `Ship_Game/StoryAndEvents/CodexScreen.cs` | Replace `UITextBox HelpEntries` with `StyledTextRenderer`; call `SetText(StyledTextParser.Parse(Localizer.Token(entry.TextId)))` |
| `StarDrive.csproj` | Register the three new files |

### Acceptance criteria

- A codex entry with `<color=Caption>Title:</color> body text` renders Title in gold, body in white.
- `<b>Warning</b>` renders bold.
- `<url=https://example.com>Click me</url>` renders blue + underlined; clicking opens the URL in the default browser.
- A single `<img>UI/icon_research</img>` between text runs renders the icon inline and forces the following text onto a new line.
- Three consecutive `<img>` tags render side-by-side on one line (assuming combined width fits), then a single line break before the next text run.
- A missing image path (e.g., `<img>does_not_exist</img>`) renders the path string in warning color without crashing.
- Long body text wraps correctly at the panel right edge.
- Unknown tags don't crash — render as literal text.

---

## Phase 3 — Tooltip hooks + F1

**Goal**: any tooltip in the game can declare a Codex UID; when shown, an additional line "Press F1 for details" appears; pressing F1 opens the Codex pre-navigated to that entry.

**Estimate**: 2–3 hours.

### Sub-task 3a: Move FTLOverlay off F1

Currently [InputState.cs:106](../Ship_Game/Input/InputState.cs#L106):
```csharp
public bool FTLOverlay   => KeyPressed(Keys.F1);
public bool RangeOverlay => KeyPressed(Keys.F2);
```

Move to:
```csharp
public bool RangeOverlay => KeyPressed(Keys.F2);
public bool FTLOverlay   => KeyPressed(Keys.F3);   // moved from F1 (Codex now owns F1)
public bool CodexHelp    => KeyPressed(Keys.F1);   // new
```

Audit for any code/text/tutorial referencing "F1 for gravity wells" or similar:
```
grep -rn "F1" Ship_Game game/Content/Tutorials game/Content/HelpTopics
```

Update any hits. Likely candidates:
- Tutorial slides mentioning F1
- The Minimap FTL toggle button's tooltip
- Any GameText entry referencing F1

### Sub-task 3b: Extend ToolTip API

Find `Ship_Game/ToolTip.cs` (or wherever `CreateTooltip` is defined). Add optional `codexUid` parameter:

```csharp
public static void CreateTooltip(string text, string codexUid = null) { ... }
public static void CreateTooltip(int tokenId, string codexUid = null) { ... }
public static void CreateTooltip(GameText token, string codexUid = null) { ... }
```

Store the codexUid on the active tooltip state. When the tooltip is rendered:
- If `codexUid != null`, append a divider + the line `Localizer.Token(GameText.CodexPressF1ForDetails)` (new token, ID 100000+).

### Sub-task 3c: Wire F1

In `UniverseScreen.HandleInput.cs` (and wherever else tooltips can be active):

```csharp
if (input.CodexHelp)
{
    string hookedUid = ToolTip.GetActiveCodexUid();
    if (hookedUid != null)
    {
        var codex = new CodexScreen(this);
        ScreenManager.AddScreen(codex);
        codex.OpenAt(hookedUid);
    }
}
```

If no tooltip is active or it has no hook, F1 is a no-op (don't fall back to old FTL — that's on F3 now).

### Files

| File | Change |
|------|--------|
| `Ship_Game/Input/InputState.cs` | F1 → CodexHelp; F3 → FTLOverlay (was F1) |
| `Ship_Game/ToolTip.cs` | Optional codexUid arg + render hint line |
| `Ship_Game/Universe/UniverseScreen/UniverseScreen.HandleInput.cs` | F1 handler → open Codex |
| `Ship_Game/Universe/MiniMap.cs` | Audit FTL toggle button text/tooltip |
| `game/Content/GameText.yaml` | New token `CodexPressF1ForDetails` |
| `game/Content/Tutorials/*` | Audit + update any F1 references |

### Acceptance criteria

- Hover a UI element with a hooked tooltip → tooltip shows extra line "Press F1 for details".
- Press F1 → Codex opens at the hooked entry.
- Press F3 → FTL gravity-well overlay toggles (old F1 behavior).
- No regression: tooltips without hooks render unchanged.

---

## Phase 5 — Content authoring (ongoing)

**Goal**: actual codex content covering game systems. Not a single commit; each system gets an entry over time, plus tooltip hooks where relevant.

### Initial seed (suggested first batch)

Categories to seed, ordered by player-discovery sequence:

1. **First steps**: New Game, Colonization, Build queue, Trade, Research
2. **Combat**: Weapons (range / damage types), Shields, Armor, Combat AI states, Targeting
3. **Diplomacy**: Treaties, War types, Relations modifiers, Espionage
4. **Mid-game systems**: Mining stations, Research stations, Subspace projectors / FTL inhibition
5. **Late-game**: Exotic resources, Capital ships, Megastructures
6. **Modding**: How to add ships / weapons / mods, mod load order

### Process for adding a tooltip hook

When authoring a tooltip somewhere in the game:

1. Decide whether the topic warrants a codex entry. Hooks should only appear where the player will plausibly want depth (combat mechanics, economy formulas, etc.) — not on every button.
2. Add the codex entry to `Codex.yaml` (or reuse an existing one).
3. Update the tooltip call site: `ToolTip.CreateTooltip(..., codexUid: "combat_shields")`.
4. Verify in-game: hover → "Press F1 for details" appears → press F1 → correct entry opens.

---

## Risks & gotchas

| Risk | Mitigation |
|------|-----------|
| F1 rebinding breaks muscle memory for veterans | Mention in patch notes; tutorial slide if any reference F1 must be updated. |
| Russian translation drift during migration | Migration script logs unmatched topics; manually review the log before committing. |
| Mods (Combined Arms, Star Trek) may eventually override Codex.yaml | Existing mods don't override HelpTopics today (verified 2026-05-21). New format means future mod overrides drop in a `Codex.yaml` in their mod dir. Resolution: same `GatherFilesModOrVanilla` pattern as today. |
| StyledText line-wrap edge cases (mixed bold/regular line height, hyphenation) | Accept v1 imperfection; cap longest run at 200 chars; use per-line max font height. Polish in a follow-up. |
| Codex entry deleted but tooltip still references its UID | `OpenAt` on missing UID → log warning + open Codex at root. Non-fatal. |
| Token ID collisions if 100000+ overlaps later additions | Reserve 100000–199999 explicitly for Codex; add a comment at the top of GameText.yaml documenting the range allocations. |
| Hot-reload of yaml during play might leave UI in stale state | Test: edit Codex.yaml while game running → next time wiki is opened, fresh content. UI panels currently constructed in LoadContent; CodexScreen ctor reads file fresh on open, so this works. |
| GameText.cs regeneration after each yaml edit is friction | Document in plan: edit yaml → run `--run-localizer` → commit both. Standard StarDrive workflow. |

---

## Appendix A — Token range allocation

Reserved ranges in `GameText.yaml` to avoid future collisions:

| Range | Owner | Notes |
|-------|-------|-------|
| 1 – 99,999 | General game text | Existing + future general additions. Currently max ~18,284. |
| 100,000 – 199,999 | **Codex content** | Reserved by this plan. ~100K headroom for entries + tooltip hints. |
| 200,000 – 299,999 | Reserved (future) | Don't use without updating this table. |

Add a banner comment at the top of `game/Content/GameText.yaml`:

```yaml
# Token ID allocation:
#   1-99,999       general game text
#   100,000-199,999 codex content (see docs/codex-plan.md)
```

---

## Appendix B — Suggested initial Codex.yaml skeleton

```yaml
- UID: gameplay
  TitleId: CodexGameplay
  Children:
    - UID: gameplay_colonization
      TitleId: CodexColonization
      ShortDescId: CodexColonizationShort
      TextId: CodexColonizationText
    - UID: gameplay_research
      TitleId: CodexResearch
      ShortDescId: CodexResearchShort
      TextId: CodexResearchText
- UID: combat
  TitleId: CodexCombat
  Children:
    - UID: combat_weapons
      TitleId: CodexWeapons
      ShortDescId: CodexWeaponsShort
      TextId: CodexWeaponsText
    - UID: combat_shields
      TitleId: CodexShields
      ShortDescId: CodexShieldsShort
      TextId: CodexShieldsText
- UID: blackbox
  TitleId: CodexBlackBox
  Children:
    # ...migrated from HelpTopics
```

---

## Appendix C — Why this order (1, 2, 4, 3, 5)

Phase 4 (styling) before Phase 3 (tooltip hooks) so that:
- When tooltip hooks land (3), every codex entry already supports rich markup.
- Authors writing the first batch of content (5) don't have to rewrite plain entries into styled ones later.
- Phase 4 has no dependency on Phase 3 — they're orthogonal.

Phase 1 → 2 is mandatory order (2 builds on 1's data classes).
Phase 5 starts after 3 lands so authors can wire hooks while writing entries.
