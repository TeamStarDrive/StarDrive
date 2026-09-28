using System;
using SDUtils;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI;

// Order-time pathfinding that inserts intermediate waypoints to route around
// hostile/unknown space. Replaces the dead A* graph (PathFinder/Astar.cs).
//
// Two-tier obstacle model:
//   - FAR systems (both origin AND destination >= 1.1 system diameters from system
//     center) that aren't covered by friendly projector influence are treated as a
//     SINGLE blob — the whole system disc becomes the obstacle. Skips:
//       · friendly influence covers the system center (in our territory)
//       · system is KNOWN to be empty (no planets → no wells, safe transit)
//   - NEAR systems (origin OR destination within 1.1 diameters of the system center)
//     fall back to per-planet gravity-well detection — too close to swing around the
//     whole disc, and near the destination the disc contains the endpoint so it would
//     otherwise be skipped entirely (and its wells clipped on approach). Skips:
//       · system is NOT known to our empire (can't see wells; reactive warp-drop
//         logic handles whatever the ship hits — we can't strategically route
//         around obstacles we don't know exist).
//
// Each detoured obstacle adds exactly 2 waypoints that bracket the disc on the
// "natural" side. The middle leg (wp1→wp2) runs parallel to the original route
// outside the obstacle perimeter, so no warp-drop is needed for that obstacle.
//
// Cost is O(systems_near_route * planets_in_near_systems) per move order, not per tick.
public static class GravityWellRouter
{
    // Breathing room beyond the obstacle radius so tangent waypoints sit OUTSIDE.
    // Used by the system-disc and the segment-intersection pre-check, where a flat
    // value is appropriate (system radii are huge).
    const float Margin = 500f;

    // Keep ships visibly outside the authoritative influence radius. This also
    // absorbs small movement overshoots while a ship transitions between orders.
    const float BorderMargin = 2_000f;
    const float BorderStopBuffer = 100f;

    // For per-planet routing we use a percentage instead — small wells (~8K) get a
    // small margin, large wells get proportionally more. 1.05 = 5% safety.
    const float PlanetWellSafetyMul = 1.05f;

    // Bracket placement multiplier: wp1/wp2 sit at this fraction of obstacleRadius
    // outside the disc, so the parallel safe-leg between them has breathing room
    // and isn't mis-detected as tangent-touching the same obstacle on the next pass.
    const float BracketPlacementMul = 1.02f;

    // Per-system "near" threshold: if origin OR destination is within (system diameter
    // * 1.1) of the system center, fall back to per-planet checks (too close to swing
    // around). Bigger systems get a wider near-band naturally.
    const float NearSystemDiameterScale = 1.1f;

    // Recursion cap. Each detour inserts 2 waypoints (bracketing the disc), so cap
    // is low: past this we degrade gracefully and let the existing reactive
    // warp-drop / GetEscapeJumpPosition logic handle remaining obstacles.
    const int MaxDetourDepth = 3;

    // Hard ceiling on total waypoints — too many warp-drop cycles negates the win.
    // Once the chain hits this, recursion bails out; reactive logic handles the rest.
    const int MaxTotalWaypoints = 8;

    // Distance at which a ship is considered to have "passed" a detour waypoint
    // and should switch to aiming at the next one. Chosen larger than the warp-drop
    // threshold (~1500) so the ship keeps warp engaged through the swing-by instead
    // of dropping out at every intermediate waypoint.
    public const float DetourReachDistance = 7500f;

    static readonly Vector2[] Empty = Array.Empty<Vector2>();

    // Diagnostic; flip to true to trace router decisions in the log. Off by default.
    public static bool LogVerbose = false;

    // Walks the detour chain — returns the next point the ship should thrust toward,
    // advancing past any detours already reached or that lie farther from the ship
    // than the final target itself (the swing-by is no longer useful).
    public static Vector2 GetThrustTarget(Vector2[] detours, ref int detourIndex, Vector2 finalTarget, Vector2 shipPosition)
    {
        if (detours == null) return finalTarget;
        float distToFinal = shipPosition.Distance(finalTarget);
        while (detourIndex < detours.Length)
        {
            float distToDetour = shipPosition.Distance(detours[detourIndex]);
            if (distToDetour < DetourReachDistance || distToDetour >= distToFinal)
                ++detourIndex;
            else
                break;
        }
        return detourIndex < detours.Length ? detours[detourIndex] : finalTarget;
    }

    // Returns intermediate detour waypoints, in travel order (does NOT include the
    // final destination). Empty when straight-line is clear or routing is disabled.
    public static Vector2[] BuildDetours(Ship ship, Vector2 from, Vector2 to, MoveOrder order)
    {
        UniverseState u = ship?.Universe;
        if (u == null)
            return Empty;

        bool routeGravityWells = GlobalStats.RouteAroundGravityWells
                              && u.P.GravityWellRange != 0f
                              && !ship.Loyalty.WeAreRemnants
                              && !order.IsSet(MoveOrder.Aggressive)
                              && !order.IsSet(MoveOrder.Pursue);
        bool routeClosedBorders = HasClosedForeignBorders(ship);
        if (!routeGravityWells && !routeClosedBorders)
        {
            if (LogVerbose && ship != null)
                Log.Info($"[GWRouter] SKIP ({ship.Name}): no gravity-well or closed-border obstacles");
            return Empty;
        }

        var detours = new Array<Vector2>();
        Recurse(ship, origin: from, finalDest: to, a: from, b: to, detours, depth: 0,
                routeGravityWells: routeGravityWells, routeClosedBorders: routeClosedBorders);

        int beforeSmoothing = detours.Count;
        // Single-obstacle detours are always 2 brackets that can't collapse (chord
        // through them re-clips the disc by construction). Only run smoother when
        // multiple obstacles have produced potentially-redundant waypoints.
        if (detours.Count > 2)
            SmoothChain(ship, from, to, detours, routeGravityWells, routeClosedBorders);

        if (LogVerbose)
        {
            float dist = from.Distance(to);
            if (beforeSmoothing != detours.Count)
                Log.Info($"[GWRouter] {ship.Name}: from={from} to={to} dist={dist:0} → {detours.Count} detour(s)  (smoothed {beforeSmoothing}→{detours.Count})");
            else
                Log.Info($"[GWRouter] {ship.Name}: from={from} to={to} dist={dist:0} → {detours.Count} detour(s)");
        }
        return detours.Count == 0 ? Empty : detours.ToArray();
    }

    static bool HasClosedForeignBorders(Ship ship)
    {
        foreach (Empire empire in ship.Universe.Empires)
            if (!ship.HasBorderAccessTo(empire) && empire.BorderNodes.Length > 0)
                return true;
        return false;
    }

    static int GetBorderBridgeSteps(Empire owner, in Empire.InfluenceNode a, in Empire.InfluenceNode b,
                                    out float radius)
    {
        radius = owner.GetBorderConnectionRadius(new InfluenceConnection(a, b)) + BorderMargin;
        float distance = a.Position.Distance(b.Position);
        return Math.Max(2, (int)Math.Ceiling(distance / (radius * 1.5f)));
    }

    static float GetBorderNodeObstacleRadius(in Empire.InfluenceNode node)
        => node.Radius * (1f + Empire.BorderShapeMaxVariation) + BorderMargin;

    /// <summary>Cheap runtime check used to invalidate an in-flight route when
    /// projected borders or diplomatic access change.</summary>
    public static bool CrossesClosedBorder(Ship ship, Vector2 from, Vector2 to)
    {
        return TryFindClosedBorderEntry(ship, from, to, out _);
    }

    static Empire ClosedBorderOwnerAt(Ship ship, Vector2 point)
    {
        foreach (Empire empire in ship.Universe.Empires)
            if (!ship.HasBorderAccessTo(empire) && empire.IsInBorderTerritory(point))
                return empire;
        return null;
    }

    /// <summary>Used by strategic AI before selecting a destination. This keeps
    /// ships from accepting jobs whose endpoint is inside territory they cannot
    /// legally enter, instead of relying on the per-frame physics guard.</summary>
    public static bool IsDestinationAccessible(Ship ship, Vector2 destination)
        => ship?.Universe == null || ClosedBorderOwnerAt(ship, destination) == null;

    public static bool IsDestinationAccessible(Empire traveler, Vector2 destination)
    {
        if (traveler?.Universe == null)
            return true;
        foreach (Empire owner in traveler.Universe.Empires)
            if (!traveler.HasBorderAccessTo(owner) && owner.IsInBorderTerritory(destination))
                return false;
        return true;
    }

    /// <summary>Stricter strategic check for peaceful expansion. A planet covered
    /// by any known closed empire's raw claim is not a valid automatic colony
    /// target, even where winner-resolved or contested rendering assigns the exact
    /// point to another field.</summary>
    public static bool IsInsideClosedBorderClaim(Empire traveler, Vector2 point)
    {
        if (traveler?.Universe == null)
            return false;
        foreach (Empire owner in traveler.Universe.Empires)
        {
            if (traveler.HasBorderAccessTo(owner))
                continue;
            if (owner.GetBorderClaimStrength(point) >= 0f)
                return true;
        }
        return false;
    }

    public static bool IsValidAutoColonizationTarget(Empire traveler, Planet planet)
    {
        return planet != null
            && !IsInsideClosedBorderClaim(traveler, planet.Position)
            && !IsInsideClosedBorderClaim(traveler, planet.System.Position);
    }

    public static bool IsInsideClosedBorder(Ship ship)
        => ship?.Universe != null && ClosedBorderOwnerAt(ship, ship.Position) != null;

    /// <summary>Finds the closest approximately straight, monotonically outward
    /// route from a ship trapped by border growth, a treaty change, or an event
    /// spawn. Sampling is intentionally done only when evacuation begins.</summary>
    public static bool TryGetNearestClosedBorderExit(Ship ship, out Vector2 exit)
    {
        exit = ship?.Position ?? Vector2.Zero;
        if (ship?.Universe == null)
            return false;
        Empire owner = ClosedBorderOwnerAt(ship, ship.Position);
        if (owner == null)
            return false;

        float step = Math.Max(owner.GetProjectorRadius() * 0.12f, BorderStopBuffer * 4f);
        float maxDistance = step * 4f;
        foreach (Empire.InfluenceNode node in owner.BorderNodes)
            maxDistance = Math.Max(maxDistance, ship.Position.Distance(node.Position) + node.Radius * 1.5f);

        float startStrength = owner.GetBorderClaimStrength(ship.Position);
        float bestDistance = float.MaxValue;
        const int directions = 48;
        float phase = ship.Id * 0.618033989f;
        for (int d = 0; d < directions; ++d)
        {
            float angle = phase + d * ((float)Math.PI * 2f) / directions;
            Vector2 direction = new((float)Math.Cos(angle), (float)Math.Sin(angle));
            float previousStrength = startStrength;
            for (float distance = step; distance <= maxDistance; distance += step)
            {
                Vector2 candidate = ship.Position + direction * distance;
                float strength = owner.GetBorderClaimStrength(candidate);
                if (strength > previousStrength + 0.00001f)
                    break; // this ray first travels deeper into the territory
                previousStrength = strength;
                if (ClosedBorderOwnerAt(ship, candidate) == null)
                {
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        exit = candidate + direction * BorderMargin;
                    }
                    break;
                }
            }
        }
        return bestDistance < float.MaxValue;
    }

    static bool TryFindClosedBorderEntry(Ship ship, Vector2 from, Vector2 to, out float entry)
    {
        entry = 1f;
        Vector2 move = to - from;
        float distance = move.Length();
        if (ship?.Universe == null || distance < 0.01f)
            return false;

        float sampleLength = ship.Loyalty.GetProjectorRadius() * 0.5f;
        int steps = distance <= sampleLength ? 1 : (int)Math.Ceiling(distance / sampleLength);
        steps = Math.Max(1, Math.Min(steps, 512));
        Empire startingOwner = ClosedBorderOwnerAt(ship, from);
        float startingStrength = startingOwner?.GetBorderClaimStrength(from) ?? 0f;
        float previousT = 0f;

        for (int step = 1; step <= steps; ++step)
        {
            float t = step / (float)steps;
            Vector2 point = from + move * t;
            Empire owner = ClosedBorderOwnerAt(ship, point);

            if (startingOwner != null && owner == startingOwner)
            {
                // Treaty changes must not trap ships, but only genuine progress
                // toward the frontier is allowed—not movement deeper inside.
                // Do not use a per-frame epsilon here. A slow ship can move less
                // than that epsilon each frame and cumulatively creep inward.
                if (owner.GetBorderClaimStrength(point) > startingStrength)
                {
                    entry = 0f;
                    return true;
                }
                previousT = t;
                continue;
            }

            if (owner != null)
            {
                float low = previousT;
                float high = t;
                for (int iteration = 0; iteration < 12; ++iteration)
                {
                    float mid = (low + high) * 0.5f;
                    if (ClosedBorderOwnerAt(ship, from + move * mid) != null) high = mid;
                    else low = mid;
                }
                entry = high;
                return true;
            }

            startingOwner = null; // the ship successfully exited old closed territory
            previousT = t;
        }
        return false;
    }

    /// <summary>
    /// Last-line physics guard for movement plans which thrust directly instead of
    /// using waypoints (combat, orbit, escort, trade and formation behaviours).
    /// Returns the last legal point before a closed foreign frontier.
    /// </summary>
    public static bool ClampBorderCrossing(Ship ship, Vector2 from, Vector2 to, out Vector2 legalPosition)
    {
        legalPosition = to;
        Vector2 move = to - from;
        float moveLen2 = move.SqLen();
        if (!TryFindClosedBorderEntry(ship, from, to, out float earliestEntry))
            return false;

        // Stay just outside rather than landing exactly on a numerically unstable edge.
        float safeEntry = Math.Max(0f, earliestEntry - BorderStopBuffer / (float)Math.Sqrt(moveLen2));
        legalPosition = from + move * safeEntry;
        return true;
    }

    /// <summary>
    /// Stops a move whose destination is inside a closed foreign border at the
    /// first border edge. A ship caught inside by a treaty change may move outward,
    /// but cannot exploit repeated orders at the boundary to push farther inward.
    /// </summary>
    public static Vector2 ClampToAccessibleBorders(Ship ship, Vector2 from, Vector2 destination)
    {
        // A legal destination on the far side must remain intact so BuildDetours
        // can route around the border. Clamp only an endpoint that is itself illegal.
        if (IsDestinationAccessible(ship, destination))
            return destination;
        Vector2 move = destination - from;
        float moveLen2 = move.SqLen();
        if (!TryFindClosedBorderEntry(ship, from, destination, out float earliestEntry))
            return destination;
        float safeEntry = Math.Max(0f, earliestEntry - BorderStopBuffer / (float)Math.Sqrt(moveLen2));
        return from + move * safeEntry;
    }

    // String-pulling pass: walk the chain and drop any waypoint whose neighbours
    // can see each other directly (no blocker on the shortcut segment). Iterates
    // until a full pass makes no changes. Cheap because chains are tiny (≤8 nodes).
    //
    // Operates on the FULL polyline [from, d0, d1, ..., dn, to] so endpoints
    // anchor the smoothing — but only mutates the interior `detours` list.
    // Uses the original `from` as the routing origin so near/far system classification
    // stays consistent with how the chain was built.
    static void SmoothChain(Ship ship, Vector2 from, Vector2 to, Array<Vector2> detours,
                            bool routeGravityWells, bool routeClosedBorders)
    {
        // Build the working polyline including endpoints
        var nodes = new Array<Vector2>(detours.Count + 2);
        nodes.Add(from);
        for (int i = 0; i < detours.Count; i++) nodes.Add(detours[i]);
        nodes.Add(to);

        bool changed;
        do
        {
            changed = false;
            int i = 0;
            while (i + 2 < nodes.Count)
            {
                if (IsSegmentClear(ship, from, to, nodes[i], nodes[i + 2],
                                   routeGravityWells, routeClosedBorders))
                {
                    if (LogVerbose)
                        Log.Info($"[GWRouter]   smoother dropping detour at idx {i + 1} ({nodes[i + 1]})");
                    nodes.RemoveAt(i + 1);
                    changed = true;
                    // stay at i — the new triple [i, i+1, i+2] might also collapse
                }
                else
                {
                    i++;
                }
            }
        } while (changed);

        // Write back the interior (drop the from/to anchors)
        detours.Clear();
        for (int i = 1; i < nodes.Count - 1; i++)
            detours.Add(nodes[i]);
    }

    static bool IsSegmentClear(Ship ship, Vector2 origin, Vector2 finalDest, Vector2 a, Vector2 b,
                               bool routeGravityWells, bool routeClosedBorders)
    {
        return !FindFirstBlocker(ship, origin, finalDest, a, b,
                                 out Vector2 _, out float _, out string _,
                                 routeGravityWells, routeClosedBorders, logNoBlock: false);
    }

    static void Recurse(Ship ship, Vector2 origin, Vector2 finalDest,
        Vector2 a, Vector2 b, Array<Vector2> detours, int depth,
        bool routeGravityWells, bool routeClosedBorders)
    {
        if (depth >= MaxDetourDepth)
        {
            if (LogVerbose) Log.Info($"[GWRouter]   depth cap reached at {depth}");
            return;
        }
        if (detours.Count >= MaxTotalWaypoints)
        {
            if (LogVerbose) Log.Info($"[GWRouter]   waypoint ceiling ({MaxTotalWaypoints}) reached");
            return;
        }

        if (!FindFirstBlocker(ship, origin, finalDest, a, b,
                              out Vector2 obstacleCenter, out float obstacleRadius,
                              out string obstacleName, routeGravityWells, routeClosedBorders))
            return;

        // Two-waypoint bracket around the disc, with a verify-and-merge pass:
        //   1. compute brackets for the current cluster
        //   2. check whether the safe-leg wp1→wp2 actually clears every other blocker
        //   3. if a NEW blocker shows up on the safe-leg, merge it into the cluster
        //      and recompute brackets — handles cases like two closely-spaced wells
        //      where the seed segment only intersected one, but the chosen detour
        //      side puts the safe-leg through the other.
        Vector2 ab = b - a;
        Vector2 abDir = ab.Normalized();
        Vector2 wp1, wp2;
        ComputeBrackets(obstacleCenter, obstacleRadius, a, ab, abDir, out wp1, out wp2);

        const int MaxSafetyMerges = 3;
        for (int it = 0; it < MaxSafetyMerges; it++)
        {
            if (!FindFirstBlocker(ship, origin, finalDest, wp1, wp2,
                                  out Vector2 otherC, out float otherR, out string otherN,
                                  routeGravityWells, routeClosedBorders, logNoBlock: false))
                break; // safe-leg actually clear

            float d = (otherC - obstacleCenter).Length();
            if (d < 0.001f) break; // safety: degenerate
            MergeDiscs(ref obstacleCenter, ref obstacleRadius, otherC, otherR, d);
            obstacleName += " ⊕ " + otherN;
            ComputeBrackets(obstacleCenter, obstacleRadius, a, ab, abDir, out wp1, out wp2);
            if (LogVerbose)
                Log.Info($"[GWRouter]   depth={depth} safe-leg hit '{otherN}' → merged, recomputed brackets");
        }

        if (LogVerbose)
            Log.Info($"[GWRouter]   depth={depth} BLOCKED by {obstacleName} r={obstacleRadius:0} → wp1={wp1} wp2={wp2}");

        // Recurse on the three sub-segments (the middle leg is safe from THIS obstacle
        // but could still hit a different one along its parallel path).
        Recurse(ship, origin, finalDest, a, wp1, detours, depth + 1,
                routeGravityWells, routeClosedBorders);
        detours.Add(wp1);
        Recurse(ship, origin, finalDest, wp1, wp2, detours, depth + 1,
                routeGravityWells, routeClosedBorders);
        detours.Add(wp2);
        Recurse(ship, origin, finalDest, wp2, b, detours, depth + 1,
                routeGravityWells, routeClosedBorders);
    }

    static void ComputeBrackets(Vector2 obstacleCenter, float obstacleRadius,
        Vector2 a, Vector2 ab, Vector2 abDir, out Vector2 wp1, out Vector2 wp2)
    {
        Vector2 fromCenterToLine = ClosestPointOnSegment(a, ab, obstacleCenter) - obstacleCenter;
        float perpLen = fromCenterToLine.Length();
        Vector2 perpDir = perpLen > 1f
            ? fromCenterToLine / perpLen
            : abDir.LeftVector(); // degenerate: center sits on the line, pick a side

        float placement = obstacleRadius * BracketPlacementMul;
        Vector2 safeBandCenter = obstacleCenter + perpDir * placement;
        wp1 = safeBandCenter - abDir * placement;
        wp2 = safeBandCenter + abDir * placement;
    }

    static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 ab, Vector2 p)
    {
        float abLen2 = ab.SqLen();
        if (abLen2 < 1f) return a;
        float t = (p - a).Dot(ab) / abLen2;
        if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
        return a + ab * t;
    }

    // Returns the earliest blocker (smallest t along the segment a→b).
    // Each candidate system chooses between system-level and planet-level routing
    // based on its distance from `origin`:
    //   - far system, no friendly influence → system disc is the obstacle
    //   - near system → per-planet check inside it (existing gravity-well logic)
    // Per-segment candidate before merging.
    readonly struct Candidate
    {
        public readonly Vector2 Center;
        public readonly float Radius;
        public readonly string Name;
        public readonly float T;
        public Candidate(Vector2 c, float r, string n, float t) { Center = c; Radius = r; Name = n; T = t; }
    }

    static bool FindFirstBlocker(Ship ship, Vector2 origin, Vector2 finalDest,
        Vector2 a, Vector2 b,
        out Vector2 obstacleCenter, out float obstacleRadius, out string obstacleName,
        bool routeGravityWells, bool routeClosedBorders, bool logNoBlock = true)
    {
        obstacleCenter = default;
        obstacleRadius = 0f;
        obstacleName = null;
        float scale = ShipInhibitionScale(ship);

        Vector2 ab = b - a;
        float abLen2 = ab.SqLen();
        if (abLen2 < 1f)
            return false;

        var candidates = new Array<Candidate>();
        var systems = ship.Universe.Systems;
        int sysCandidates = 0, sysObstacles = 0, planetsBlocking = 0;
        int skipFriendly = 0, skipKnownEmpty = 0, skipNearUnknown = 0;
        for (int s = 0; routeGravityWells && s < systems.Count; s++)
        {
            SolarSystem sys = systems[s];
            float bandR = sys.Radius + Margin;
            if (!SegmentIntersectsDisc(a, ab, abLen2, sys.Position, bandR))
                continue;
            sysCandidates++;

            float nearThreshold = sys.Radius * 2f * NearSystemDiameterScale;
            // Per-planet routing kicks in when the ship can't swing around the whole system
            // disc — true near BOTH the origin and the destination. Near the destination this
            // lets us avoid individual wells on approach: the arrival system's disc contains
            // finalDest, so the coarse system-disc path skips the whole system and clips its
            // wells. Per-planet still skips the destination planet's own well (we're going there).
            float nt2 = nearThreshold * nearThreshold;
            bool isNear = sys.Position.SqDist(origin) < nt2 || sys.Position.SqDist(finalDest) < nt2;
            bool isKnown = sys.IsExploredBy(ship.Loyalty);

            if (!isNear)
            {
                if (ship.Universe.Influence.IsInInfluenceOf(ship.Loyalty, sys.Position))
                {
                    skipFriendly++;
                    continue;
                }

                // Known portal systems carry a warp inhibitor — treat as obstacle even when
                // planet-less (radiating-star / lone-system portals would otherwise be skipped
                // by the known-empty rule and trap a passing ship).
                bool isKnownPortalSystem = isKnown && ship.Universe.HasRemnantPortal(sys);

                if (isKnown && sys.PlanetList.Count == 0 && !isKnownPortalSystem)
                {
                    skipKnownEmpty++;
                    continue;
                }

                float sysR = sys.Radius + Margin;
                if (TryCollect(origin, finalDest, a, ab, abLen2, sys.Position, sysR, ref candidates, $"system {sys.Name}"))
                    sysObstacles++;
            }
            else
            {
                if (!isKnown)
                {
                    skipNearUnknown++;
                    continue;
                }

                var planets = sys.PlanetList;
                for (int i = 0; i < planets.Count; i++)
                {
                    Planet p = planets[i];
                    if (!IsBlockingFor(p, ship))
                        continue;
                    planetsBlocking++;

                    float r = p.GravityWellRadius * scale * PlanetWellSafetyMul;
                    TryCollect(origin, finalDest, a, ab, abLen2, p.Position, r, ref candidates, $"planet {p.Name}");
                }
            }
        }

        int closedBorderNodes = 0;
        if (routeClosedBorders)
        {
            foreach (Empire empire in ship.Universe.Empires)
            {
                if (ship.HasBorderAccessTo(empire))
                    continue;

                Empire.InfluenceNode[] nodes = empire.BorderNodes;
                for (int i = 0; i < nodes.Length; ++i)
                {
                    ref Empire.InfluenceNode node = ref nodes[i];
                    float radius = GetBorderNodeObstacleRadius(node);
                    if (TryCollect(origin, finalDest, a, ab, abLen2, node.Position, radius,
                                   ref candidates, $"closed border {empire.Name}"))
                        ++closedBorderNodes;
                }


                // The map presents nearby colonies and projector stations as one
                // continuous territory. Model the connecting corridor as a short
                // chain of overlapping discs so navigation obeys that same border.
                foreach (InfluenceConnection connection in empire.BorderConnections)
                {
                    Empire.InfluenceNode n1 = connection.Node1;
                    Empire.InfluenceNode n2 = connection.Node2;
                    int steps = GetBorderBridgeSteps(empire, n1, n2, out _);
                    Vector2 bridge = n2.Position - n1.Position;
                    for (int step = 1; step < steps; ++step)
                    {
                        float amount = step / (float)steps;
                        float radius = empire.GetBorderConnectionRadiusAt(connection, amount) + BorderMargin;
                        Vector2 center = n1.Position + bridge * amount;
                        if (TryCollect(origin, finalDest, a, ab, abLen2, center, radius,
                                       ref candidates, $"closed border corridor {empire.Name}"))
                            ++closedBorderNodes;
                    }
                }
            }
        }

        if (candidates.Count == 0)
        {
            if (LogVerbose && logNoBlock)
                Log.Info($"[GWRouter]   no block: {sysCandidates}/{systems.Count} systems ({sysObstacles} far-unfriendly, {planetsBlocking} near-blocking planets, {closedBorderNodes} closed-border nodes; skipped {skipFriendly} friendly, {skipKnownEmpty} known-empty, {skipNearUnknown} near-unknown)");
            return false;
        }

        // Seed cluster from earliest candidate, then iteratively merge any others
        // whose disc overlaps the cluster. Handles partially-merged wells (which
        // would otherwise produce brackets that land inside the neighbouring well).
        int seedIdx = 0;
        for (int i = 1; i < candidates.Count; i++)
            if (candidates[i].T < candidates[seedIdx].T) seedIdx = i;

        obstacleCenter = candidates[seedIdx].Center;
        obstacleRadius = candidates[seedIdx].Radius;
        obstacleName = candidates[seedIdx].Name;
        candidates.RemoveAt(seedIdx);

        bool merged;
        do
        {
            merged = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate c = candidates[i];
                float d = (c.Center - obstacleCenter).Length();
                if (d < c.Radius + obstacleRadius)
                {
                    MergeDiscs(ref obstacleCenter, ref obstacleRadius, c.Center, c.Radius, d);
                    obstacleName += " + " + c.Name;
                    candidates.RemoveAt(i);
                    merged = true;
                    break;
                }
            }
        } while (merged);
        return true;
    }

    static bool TryCollect(Vector2 origin, Vector2 finalDest,
        Vector2 a, Vector2 ab, float abLen2, Vector2 center, float r,
        ref Array<Candidate> candidates, string name)
    {
        float r2 = r * r;

        // If the USER's original endpoints (origin or finalDest) sit inside this
        // disc, skip it — routing around an obstacle that contains the start or
        // end of the journey is futile (the final leg or initial leg re-enters
        // the well regardless of how cleverly we detour). Let reactive warp-drop
        // logic handle it. NOTE: we deliberately check ONLY the user's endpoints,
        // not the sub-segment's a/b — those are internally-computed waypoints,
        // and if they land inside a well that's a routing bug we WANT to flag
        // so the safety-merge can pull that well into the cluster.
        if (origin.SqDist(center) <= r2)
        {
            if (LogVerbose) Log.Info($"[GWRouter]   skip {name}: origin inside disc (r={r:0})");
            return false;
        }
        if (finalDest.SqDist(center) <= r2)
        {
            if (LogVerbose) Log.Info($"[GWRouter]   skip {name}: finalDest inside disc (r={r:0}) — destination system, not routed around");
            return false;
        }

        // Closest-point t can be outside (0, 1) — clamp to capture wells that contain
        // an endpoint of the segment too. Otherwise wells right at a/b silently slip
        // through and brackets end up inside them.
        float t = (center - a).Dot(ab) / abLen2;
        float clampedT = t < 0f ? 0f : (t > 1f ? 1f : t);
        Vector2 closest = a + ab * clampedT;
        if (closest.SqDist(center) > r2) return false;
        candidates.Add(new Candidate(center, r, name, clampedT));
        return true;
    }

    // Smallest enclosing circle of two discs (closed form). `d` is precomputed distance.
    static void MergeDiscs(ref Vector2 c1, ref float r1, Vector2 c2, float r2, float d)
    {
        // Case 1: one disc fully contains the other → use the bigger
        if (d + Math.Min(r1, r2) <= Math.Max(r1, r2))
        {
            if (r2 > r1) { c1 = c2; r1 = r2; }
            return;
        }
        // Case 2: partial overlap → new circle along the C1–C2 line
        float newR = (d + r1 + r2) * 0.5f;
        if (d > 0.001f)
        {
            Vector2 dir = (c2 - c1) / d;
            c1 = c1 + dir * (newR - r1);
        }
        r1 = newR;
    }

    // Mirrors SolarSystem.IdentifyGravityWell's inhibition rule, but evaluated
    // at the WELL'S position (not the ship's) so long routes that exit projector
    // coverage are handled correctly:
    //   - own planets:   never block (no self-inhibit)
    //   - enemy planets: ALWAYS block (inhibits even inside friendly projector range)
    //   - star (system center) NOT in our influence: the whole system is hostile transit
    //     space. A ship crossing it won't be in friendly projector range, so EVERY well
    //     inhibits — a single planet that merely clips the fringe of our influence must not
    //     exempt itself (the Romandal case: one planet sat partly in our influence while the
    //     system did not, so the router waved the scout straight through the well).
    //   - star in our influence: friendly system — block only wells outside our coverage.
    static bool IsBlockingFor(Planet p, Ship ship)
    {
        Empire owner = p.Owner;
        if (owner == ship.Loyalty) return false;
        if (owner != null && owner.WillInhibit(ship.Loyalty)) return true;
        if (!ship.Universe.Influence.IsInInfluenceOf(ship.Loyalty, p.ParentSystem.Position))
            return true;
        return !ship.Universe.Influence.IsInInfluenceOf(ship.Loyalty, p.Position);
    }

    static float ShipInhibitionScale(Ship s)
    {
        // Mirror Planet.IdentifyGravityWell: trait can shrink the effective well
        return 1f - s.Loyalty.data.Traits.EnemyPlanetInhibitionPercentCounter;
    }

    // Quick segment-disc test: does the segment from a to a+ab come within radius r of center?
    static bool SegmentIntersectsDisc(Vector2 a, Vector2 ab, float abLen2, Vector2 center, float r)
    {
        Vector2 ac = center - a;
        float t = ac.Dot(ab) / abLen2;
        if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
        Vector2 closest = a + ab * t;
        return closest.SqDist(center) <= r * r;
    }
}
