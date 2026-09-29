---
name: self-review-before-commit
description: Before committing, launch a code-review agent over the staged + unstaged diff and report findings; only proceed to commit after the review (and any fixes it surfaces).
---

# Self-review before commit

When this skill is invoked, do NOT commit yet. Run a self-review first.

## Steps

1. Gather the diff to review. In parallel:
   - `git status` — full list of changed files (including untracked).
   - `git diff HEAD` — combined staged + unstaged changes since the last commit.
   - `git log -1 --format="%H %s"` — base commit for context.

   If the diff is empty, stop and tell the user there is nothing to review.

2. Launch a review agent. Use the `Agent` tool with `subagent_type: general-purpose`
   (or `code-reviewer` if available in this environment). Hand it a self-contained
   prompt — the agent has none of this conversation's context. Include:
   - The exact files changed and their paths.
   - What the user is trying to accomplish in this set of changes (1-2 sentences,
     synthesized from the conversation — do not delegate this understanding).
   - The diff itself, or instruction to `git diff HEAD -- <paths>` themselves.
   - Ask for: correctness bugs, security issues, concurrency/race risks, and any
     dead/unused code introduced. Explicitly ask the agent to flag findings
     by severity (blocker / nit) and to skip praise.
   - Tell the agent to read `.claude/skills/self-review-before-commit/stardrive-checklist.md`
     and check every item that applies to the diff: the threading rules between the UI
     and sim threads, saves, player-facing text, mods, tests and the codebase's conventions.
   - Also require these three passes (they catch the bugs diff-local reading misses):
     - **Caller-impact** — for any method whose signature OR behavior changed,
       enumerate ALL callers and check the new behavior against each caller's intent.
       A regression often only manifests at a call site outside the diff.
     - **Project conventions** — `.csproj` global usings / type aliases (e.g. `Range` →
       `SDGraphics.Range`, so `System.Range` slice syntax is off-convention), analyzer
       rules, and the codebase's preferred patterns.
     - **Exception paths** — for new IO / external-API / callback code, what can throw,
       and is failure swallowed where it must be (telemetry/attachments are best-effort)?
   - For sensitive subsystems (telemetry/logging, save/load, threading, serialization),
     run a deeper pass and lift the word cap. Otherwise cap: "Report under 300 words,
     bullet form."

   - **Codex impact** (required whenever the diff touches gameplay code, content
     yaml, or UI behaviour): the in-game Codex (`game/Content/Codex.yaml`, the
     `Codex*Text` tokens in `game/Content/GameText.yaml`, and the tooltip hooks in
     `game/Content/CodexHooks.yaml`) describes the rules the code uses, with numbers.
     Ask the agent to name every Codex entry and tooltip that describes the changed
     behaviour (grep GameText.yaml for the mechanic's words and numbers) and to say
     whether the text is now wrong. A stale entry is a blocker: the fix to the text
     ships in the same commit as the code, fact-checked against the new code.

3. Read the agent's report. Categorize findings:
   - **Blockers** — correctness/security/concurrency issues. Fix before committing.
   - **Nits** — style/clarity. Surface to the user; let them decide.
   - **False positives** — note briefly why dismissed.

4. Present the review to the user in a short summary. If there are blockers,
   propose fixes and ask the user before applying. If there are only nits,
   list them and ask whether to address or commit as-is. If the review is
   clean, say so and proceed to the normal commit flow. Before applying any fix,
   stage the reviewed changes (`git add` the reviewed files), so that step 5 can
   read the fixes on their own with `git diff`.

5. **Review the fixes you just made.** Code written to satisfy a review finding is
   the least-reviewed code in the diff: the review already ran, and a reviewer cannot
   catch the consequence of its own suggestion. Before committing, re-read the fix
   diff on its own (`git diff`, which shows only what changed since the reviewed
   files were staged) and ask of every change:
   - For a new early return, skipped initialization or swapped lookup: what does the
     object still owe downstream? Which fields stay at their defaults, and who reads
     them next? (This is the question that shipped a regression in PR #408: an early
     return left a Building with an unserialized `BID` of 0, and the next planet
     update threw on the sim thread.)
   - Did I verify the suggestion's *consequence*, or only its premise? A reviewer's
     proposed fix is a hypothesis, not a patch.
   - Does any new test pass with AND without the fix? If so it is not coverage —
     revert the fix and watch it fail.
   Findings here are fixed the same way, then re-read again.

6. After the user confirms (and any fixes are applied), perform the commit
   exactly as the standard "create a git commit" flow does — drafted commit
   message via HEREDOC, `Co-Authored-By` trailer, etc.

## Notes

- The review agent should run in the foreground — you need its findings before
  you can decide whether to commit.
- Do not skip step 2 when the diff is "obviously small." A trivial-looking
  diff is exactly where a fresh pair of eyes catches the missed null guard
  or the accidentally-removed branch.
- If the user explicitly says "skip review" or "just commit," honor that: skip
  the review agent and go to the commit flow, after the minimum Codex grep below.
- A nit that *downgrades* a defect rather than removing it ("stuck forever" becomes
  "lingers a second") is a finding that needs a decision, not one to accept silently.
- The Codex impact pass is never skipped for a gameplay change, even when the
  review itself is skipped: a one-line grep of GameText.yaml for the changed rule
  is the minimum.
