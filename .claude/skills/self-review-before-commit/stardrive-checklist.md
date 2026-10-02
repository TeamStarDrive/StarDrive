# StarDrive review checklist

Rules every change to this codebase is held to, collected from past reviews. Both review skills
(`self-review-before-commit`, `full-pr-review`) hand this file to their review agent: check each
item that applies to the diff and report a broken one as a finding.

## Threads

The UI thread draws and handles input; the sim thread runs the universe. They run at the same
time.

- UI code (`Draw`, `HandleInput`, screen and UI element `Update`) that reads a list the sim thread
  changes in place - `fleet.Ships`, a planet's buildings - must survive the list changing
  mid-read. Read it once per frame. Never walk it twice and assume both passes saw the same
  ships, and never look up a key from one pass in a map built by another.
- UI code that changes game state goes through `UniverseScreen.RunOnSimThread`.
- While a screen that is not a popup (`IsPopup` false) covers the universe, the universe stops
  drawing and the sim thread waits, so work queued with `RunOnSimThread` does not run until that
  screen closes. A fix that relies on queued work running while a screen is open must say which
  kind of screen it is.
- Sim-thread code does not touch UI objects (`ScrollList` entries, UI elements). Set a flag and
  let the UI thread act on it in its own `Update`, or queue the work with the screen's
  `RunOnNextFrame` (it runs only while that screen is visible).
- An object the draw thread may be iterating over is not removed from the sim thread mid-frame:
  mark it dead and let the owning thread remove it in its own tick. No locks or concurrent
  collections without asking the maintainers first.

## Before the fix

- Confirm the bug's premise in the code before changing anything: trace the path that produces
  the symptom. A fix for a cause that isn't there adds risk and fixes nothing.
- Read every caller of every method whose behaviour changes.
- Look for the same bug next door. A component rebuilt on load usually sits next to another one
  rebuilt the same way.

## Code

- No comments that explain the change, the review or a fork's history. The reasoning goes in the
  commit message and the PR. Match the sparse comment style of the surrounding code.
- In per-frame and per-ship paths, prefer `for` loops over LINQ and avoid allocations every frame.
  Check `SDUtils/CollectionExt.cs` and `Ship_Game/ExtensionMethods/CollectionReduce.cs` for an
  existing helper first.
- `Range` is aliased to `SDGraphics.Range` in the project files, so `System.Range` slice syntax
  (`x[..n]`) is off-convention.
- Every project (`StarDrive.csproj`, `SDUtils`, `SDGraphics`, `UnitTests/SDUnitTests.csproj`)
  lists its files explicitly: a new `.cs` file needs a `<Compile Include="..." />` entry in its
  project.
- No line-ending churn: `git diff --stat` counts only the lines actually changed.

## Saves

- A new `[StarData]` field whose "unset" value is not the type's zero needs
  `[StarData(DefaultValue = X)]` as well as the initializer. The writer skips values equal to the
  declared default, never the initializer.
- Anything rebuilt on load (`OnDeserialized`, `Ship.InitializeStatus` with `fromSave`) must carry
  over the state the save just loaded into the object it replaces.
- Adding or removing a `[StarData]` field does not need a `SaveGameVersion` bump. Ask before
  bumping it.
- Fields are matched by name, so renaming one loads old saves with the initializer instead of
  the saved value. Deleting a `[StarDataType]` type that old saves still hold in a collection
  makes those saves fail to load. Ask before doing either.

## Player-facing text and the in-game Codex

- Player-facing text lives in `game/Content/GameText.yaml`, never as a hardcoded English string.
  `LocalizedText` converts from `string` implicitly, so a hardcoded string compiles and then
  never gets translated.
- New UI tokens take free ids in 4000-8000, never 18000-20000. Check `GameText.yaml`, the
  `GameText` enum and every `game/Mods/*/GameText.yaml` for ids in use. Put the yaml block and
  its enum member next to their id neighbours; `GameText.cs` says it is generated, but add the
  member by hand rather than regenerating the file.
- In-game Codex text is different: its tokens use ids 100000 and up and are named from the
  entry's UID, with no enum member (see the header of `game/Content/Codex.yaml`).
- The in-game Codex describes the rules the code uses, with numbers. Grep `GameText.yaml` for the
  changed mechanic's words and numbers: Codex entries are the `Codex*` tokens there, and
  tooltips are ordinary tokens there too (`game/Content/CodexHooks.yaml` only maps tooltips to
  entries). An entry or tooltip that now describes the old rule is a blocker and is fixed in the
  same change.

## Mods

- Content changes and "nothing uses this" claims cover the mods too: every folder under
  `game/Mods`. Only `ExampleMod` is in this repo; Combined Arms and Star Trek each have their own
  repository, so check them if you have them installed and say so in the PR if you don't.

## Tests

- The change builds and the unit tests pass: `dotnet build UnitTests/SDUnitTests.csproj`, then
  `dotnet test UnitTests/SDUnitTests.csproj --no-build`. Build the branch as it will merge, on
  top of the current fixes branch, not only on the tree you forked from.
- A new test must fail without the fix: revert the fix, watch the test fail, restore it. A test
  that passes both ways is not coverage.
- The float `AssertEqual` overload takes the tolerance first:
  `AssertEqual(tolerance, expected, actual, message)`.

## Commit message and PR

- The commit message and PR body describe what the code does now and how it was tested. Nothing
  left over from an earlier draft ("not built yet").
- Community PRs target the current `jupiter-1.60/fixes_NN` branch, not `main`, and the PR body
  links the issue ("Fixes #N").
