"""Reads AiGameProbe output. Per galaxy, the AI empires are added up (shares and averages are averaged), then:
- with one arm: its mean, lowest and highest over the galaxies;
- with two arms: the paired difference second - first per galaxy, its mean and standard error, and in how many
  galaxies the second arm was lower or higher. Both arms' means are over the paired galaxies only.
A game stopped by the time limit is left out of the pairs, and a game that crashed or failed to load is left out.

Usage: py -I ai_probe_compare.py <probe out folder> <arm> [<second arm>]
"""
import csv
import math
import os
import sys

GAME = ["turns", "wall_s", "errors", "warnings", "first_war"]
SUMS = ["planets", "pop", "money", "score", "str", "lost", "lost_str", "captured", "won", "lost_to", "colonized",
        "lost_other", "wars", "turns_at_war"]
MEANS = ["share_task", "share_idle", "share_pool", "avg_waiting", "avg_fleets", "avg_kept", "avg_cap", "idle_kept_share"]
NONE_IF_NEGATIVE = {"first_war"}


def read(out_dir, arm):
    path = os.path.join(out_dir, f"{arm}_summary.tsv")
    if not os.path.exists(path):
        sys.exit(f"no {path}: check the folder and the arm name (PROBE_OUT and PROBE_ARM)")
    games = {}
    seen = set()
    with open(path, encoding="utf-8") as f:
        for row in csv.DictReader(f, delimiter="\t"):
            if row.get("stop") in ("load_failed", "crashed"):
                print(f"{arm}: {row['galaxy']} {row['stop'].replace('_', ' ')}, left out")
                continue
            key = (row["galaxy"], row["empire"])
            if key in seen:
                sys.exit(f"{path} has {row['galaxy']} / {row['empire']} twice: the probe adds to its files, so give every "
                         "run, the calibration game included, its own PROBE_OUT folder")
            seen.add(key)
            games.setdefault(row["galaxy"], []).append(row)
    result = {}
    for galaxy, rows in games.items():
        g = {k: float(rows[0][k]) for k in GAME}
        g.update({k: sum(float(r[k]) for r in rows) for k in SUMS})
        g.update({k: sum(float(r[k]) for r in rows) / len(rows) for k in MEANS})
        g["defeated"] = sum(1 for r in rows if int(r["defeated_turn"]) >= 0)
        g["stop"] = rows[0]["stop"]
        for k in NONE_IF_NEGATIVE:
            if g[k] < 0:
                g[k] = None
        result[galaxy] = g
    return result


def mean_se(values):
    if not values:
        return float("nan"), float("nan")
    m = sum(values) / len(values)
    if len(values) < 2:
        return m, float("nan")
    return m, math.sqrt(sum((v - m) ** 2 for v in values) / (len(values) - 1)) / math.sqrt(len(values))


def mean(values):
    return sum(values) / len(values) if values else float("nan")


def main():
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    out_dir, first = sys.argv[1], sys.argv[2]
    second = sys.argv[3] if len(sys.argv) > 3 else None
    keys = GAME + ["defeated"] + SUMS + MEANS
    a = read(out_dir, first)
    if second is None:
        stopped = sorted(g for g in a if a[g]["stop"] != "turns")
        print(f"{first}: {len(a)} galaxies" + (f", stopped by the time limit: {', '.join(stopped)}" if stopped else ""))
        print("metric\tmean\tlowest\thighest\tgalaxies")
        for k in keys:
            v = [g[k] for g in a.values() if g[k] is not None]
            if v:
                print(f"{k}\t{mean(v):.3f}\t{min(v):.3f}\t{max(v):.3f}\t{len(v)}")
        return

    b = read(out_dir, second)
    both = sorted(set(a) & set(b))
    stopped = [g for g in both if a[g]["stop"] != "turns" or b[g]["stop"] != "turns"]
    paired = [g for g in both if g not in stopped]
    print(f"{first}: {len(a)} galaxies, {second}: {len(b)} galaxies, paired: {len(paired)}")
    if stopped:
        print(f"left out of the pairs, stopped by the time limit: {', '.join(stopped)}")
    print(f"metric\t{first}_mean\t{second}_mean\tdiff_mean\tdiff_se\tlower\thigher\tpaired")
    for k in keys:
        pairs = [(a[g][k], b[g][k]) for g in paired if a[g][k] is not None and b[g][k] is not None]
        d = [y - x for x, y in pairs]
        dm, se = mean_se(d)
        lower = sum(1 for x in d if x < 0)
        higher = sum(1 for x in d if x > 0)
        print(f"{k}\t{mean([x for x, _ in pairs]):.3f}\t{mean([y for _, y in pairs]):.3f}\t{dm:.3f}\t{se:.3f}"
              f"\t{lower}\t{higher}\t{len(d)}")
    print()
    print(f"planets per galaxy, {second} - {first}: "
          + ", ".join(f"{g} {b[g]['planets'] - a[g]['planets']:+.0f}" for g in paired))


if __name__ == "__main__":
    main()
