"""Inventory of tooltip tokens for Codex authoring.

Every GameText token used where a tooltip is created or assigned, with its
English text, the files using it, and whether CodexHooks.yaml already links
it. Re-run after adding tooltips or hooks:

    py docs/codex-tooltip-inventory.py

Output: docs/codex-tooltip-inventory.md (the Kind/Entry columns are filled by
hand, so re-running overwrites them - copy them out first).
"""
import os, re, sys
from collections import defaultdict

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src  = os.path.join(root, "Ship_Game")

# --- English text per token name ------------------------------------------
eng = {}          # yaml key -> ENG
yaml_by_id = {}   # token id -> yaml key
cur = None
with open(os.path.join(root, "game", "Content", "GameText.yaml"), encoding="utf-8-sig") as f:
    for line in f:
        if line and not line[0].isspace() and line.rstrip().endswith(":") and not line.startswith("#"):
            cur = line.strip()[:-1]
        elif cur and line.startswith(" Id:"):
            yaml_by_id[int(line[4:].strip())] = cur
        elif cur and line.startswith(" ENG:"):
            eng[cur] = line[5:].strip().strip('"')

# enum name -> id, from the generated GameText.cs
enum_id = {}
with open(os.path.join(root, "Ship_Game", "Data", "GameText.cs"), encoding="utf-8") as f:
    for m in re.finditer(r"^\s*(\w+)\s*=\s*(\d+)\s*,", f.read(), re.M):
        enum_id[m.group(1)] = int(m.group(2))

# --- existing hooks --------------------------------------------------------
hooks = {}
with open(os.path.join(root, "game", "Content", "CodexHooks.yaml"), encoding="utf-8") as f:
    for line in f:
        line = line.strip()
        if line and not line.startswith("#") and ":" in line:
            k, v = line.split(":", 1)
            hooks[k.strip()] = v.strip()

# --- tooltip sites ---------------------------------------------------------
# A line counts as a tooltip site when it creates or assigns a tooltip, or when
# the token itself is named *Tip (the naming convention for tooltip text).
# Stat rows (DrawStat*, Val, ValNZ) carry the tip as a positional argument.
site_re  = re.compile(r"CreateTooltip\(|Tooltip\s*=|[Tt]ool[Tt]ip:\s*|\.Tooltip\b|Tooltip\(|\.Tip\s*=|GameText\.\w+Tip\b|DrawStat\w*\(|\bVal(?:NZ)?\(")
token_re = re.compile(r"\b(?:GameText|GT)\.(\w+)")
stat_re  = re.compile(r"(DrawStat\w*|\bVal(?:NZ)?)\(")
tip_kw   = re.compile(r"[Tt]ool[Tt]ip\s*[:=(]")

def stat_tip_args(line):
    """The text after the argument that precedes the tip in a stat row call:
    DrawStat*(ref cursor, title, value, tip, ...) or Val(value, title, tip, ...)."""
    m = stat_re.search(line)
    if not m:
        return None
    skip = 2 if m.group(1).startswith("Val") else 3
    depth, args, start = 0, [], m.end()
    for i in range(m.end(), len(line)):
        c = line[i]
        if c in "([": depth += 1
        elif c in ")]":
            if depth == 0:
                break
            depth -= 1
        elif c == "," and depth == 0:
            args.append(line[start:i]); start = i + 1
    args.append(line[start:])
    return ",".join(args[skip:])

uses = defaultdict(set)   # token -> {file}
raw  = defaultdict(set)   # token -> {file} where the site passes Localizer.Token(...) (a raw string: never hooked)
for dirpath, _, files in os.walk(src):
    for fn in files:
        if not fn.endswith(".cs"):
            continue
        p = os.path.join(dirpath, fn)
        rel = os.path.relpath(p, root).replace("\\", "/")
        with open(p, encoding="utf-8", errors="replace") as f:
            for line in f:
                if not site_re.search(line):
                    continue
                # a line like `title: GameText.A, tooltip: GameText.B` names two
                # tokens; only the one after the tooltip keyword is the tip
                m = re.search(r"[Tt]ool[Tt]ip\s*[:=]|\.Tip\s*=", line)
                scan = line[m.end():] if m else line
                stat = stat_tip_args(line)
                if stat is not None:
                    scan = stat
                for tok in token_re.findall(scan):
                    if stat is not None or tok.endswith("Tip") or tip_kw.search(line) or ".Tip" in line:
                        uses[tok].add(rel)
                        if re.search(r"Localizer\.Token\(\s*GameText\." + tok + r"\b", line):
                            raw[tok].add(rel)

def short(rel):
    return rel.replace("Ship_Game/", "").replace("GameScreens/", "")

# a few yaml keys differ from the enum name (a BB_ prefix, a typo); the hook
# file accepts either, but the table shows the yaml key since that is where the
# text lives. Resolved through the token id.
def yaml_name(tok):
    return yaml_by_id.get(enum_id.get(tok, -1), tok)

rows = []
# grouped by the first screen that uses the token, so a tagging pass can walk
# one screen at a time
for tok in sorted(uses, key=lambda t: (sorted(uses[t])[0], t)):
    name = yaml_name(tok)
    text = eng.get(name, "(no ENG text)")
    rows.append((name, text, sorted(uses[tok]), hooks.get(name) or hooks.get(tok, ""), sorted(raw[tok])))

# --- write -----------------------------------------------------------------
out = os.path.join(root, "docs", "codex-tooltip-inventory.md")
lines = []
lines.append("# Codex tooltip inventory")
lines.append("")
lines.append("Generated by `docs/codex-tooltip-inventory.py`. Every GameText token used as a tooltip,")
lines.append("with its English text and where it appears. Fill the last two columns:")
lines.append("")
lines.append("- **Kind**: `action` (the tip says what a control does; no Codex needed) or `concept` (the tip explains a rule; wants an entry).")
lines.append("- **Entry**: the Codex.yaml UID the tooltip should link to. New UIDs are fine; they become the authoring list.")
lines.append("")
lines.append("Once a row has an Entry, it goes into `game/Content/CodexHooks.yaml` as `Token: entry_uid`.")
lines.append("A site marked *raw string* wraps the token in `Localizer.Token(...)`, so the tooltip carries no id and cannot be hooked until the call passes the enum.")
lines.append("")
lines.append(f"Tokens: {len(rows)}. Already hooked: {sum(1 for r in rows if r[3])}.")
lines.append("")
lines.append("| # | Token | English text | Used in | Kind | Entry |")
lines.append("|---|---|---|---|---|---|")
for i, (tok, text, files, hook, rawfiles) in enumerate(rows, 1):
    t = text.replace("|", "\\|").replace("\\n", " ")
    if len(t) > 160:
        t = t[:157] + "..."
    where = "<br>".join(short(f) + (" (raw string at site: pass the enum)" if f in rawfiles else "") for f in files)
    lines.append(f"| {i} | `{tok}` | {t} | {where} |  | {hook} |")
lines.append("")

with open(out, "w", encoding="utf-8", newline="\r\n") as f:
    f.write("\n".join(lines))
print(f"{len(rows)} tokens -> {out}")
print("no ENG text:", [r[0] for r in rows if r[1] == "(no ENG text)"][:20])
