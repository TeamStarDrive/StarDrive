using System;
using System.Threading;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Mesh;
using Ship_Game.Data.Serialization;
using Ship_Game.Graphics.Particles;
using SynapseGaming.LightingSystem.Rendering;
using Color = Microsoft.Xna.Framework.Color;
using Vector3 = SDGraphics.Vector3;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        // a hull part can go once more than this share of its slot area is dead: an update with a death under it rolls the share dead past it, and with all of it dead it goes with no roll
        public const float PartLossDeadShare = 0.75f;

        // each hull part's slot area alive at its lowest since it was lost, -1 while it is on; null while no part is lost
        [StarData] int[] LostPartLow;

        // how much of each hull part shows, 1 whole, below 1 lost and coming back with its repairs since; null while no part is lost
        public float[] PartShown { get; private set; }

        // how hot the embers in its torn insides glow: from InsidesHeatAtLoss down by the share of the lost part least back, never under Derelict.MinInsidesHeat
        public float InsidesHeat { get; private set; } = 1f;
        // how hot its lost parts' insides glowed when lost: 1 in a fight, a recovered ship's derelict's at its recovery till it loses a part anew
        [StarData(DefaultValue = 1f)] float InsidesHeatAtLoss = 1f;

        // a module died or came back since the parts were counted, or they never were
        bool PartsOutdated = true;
        // the parts a module died under since the parts were counted, each bit a part, saved so a death just before a save still counts
        [StarData] uint PartsHit;

        // a game with the Hull Plates extension switched off plays every ship as if its model were in one piece
        bool HullPlatesOff => Universe?.P.HullPlates == false;

        // a module died: its part may be lost at the next count, and any part while the hull's model has not loaded; nothing on a hull in one piece
        void MarkPartHit(ShipModule m)
        {
            ShipHull hull = BaseHull;
            if (hull.ModelInOnePiece || HullPlatesOff)
                return;
            int part = Grid.PartOf(m.Pos);
            if (part < 0 && hull.NumParts > 0 && Grid.GetParts(out _))
                part = Grid.PartOf(m.Pos);
            Interlocked.Or(ref PartsHit, part < 0 ? uint.MaxValue : 1u << part);
            PartsOutdated = true;
        }

        // a part may be lost at the count of an update with a death under it, by a roll unless all of it is dead, a part no longer tied to the core by whole parts it touches goes with it, and a lost part comes back as its modules do, no faster than the parts it hangs on; a part with no modules is lost only that way; returns the parts it lost. Standing still, paused or saving, it loses none and kills no module, and with hull plates on changes nothing a save writes
        uint UpdateLostParts(bool timeRuns = true)
        {
            ShipHull hull = BaseHull;
            if (hull.NumParts == 0 || HullPlatesOff)
            {
                if (hull.ModelInOnePiece || HullPlatesOff)
                    PartsOutdated = false; // until a module comes back
                if (HullPlatesOff)
                {
                    LostPartLow = null; // it plays in one piece, whatever a save made with hull plates on had lost
                    PartShown = null;
                    PartsHit = 0u;
                    InsidesHeat = InsidesHeatAtLoss = 1f;
                }
                return 0u;
            }
            if (!timeRuns && (PartShown != null || LostPartLow == null))
                return 0u; // what shows is as counted, or no part is lost
            if (!Grid.GetParts(out int[] area))
                return 0u;
            if (timeRuns)
                PartsOutdated = false; // before the count, so a module dying meanwhile is counted on the next update
            uint hit = timeRuns ? Interlocked.Exchange(ref PartsHit, 0u) : 0u;

            int numParts = area.Length;
            int[] saved = LostPartLow?.Length == numParts ? LostPartLow : null; // one saved with another cut of the model starts over

            Span<int> alive = stackalloc int[numParts];
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule m = ModuleSlotList[i];
                if (m.Active)
                    alive[Grid.PartOf(m.Pos)] += m.Area;
            }

            Span<int> low = stackalloc int[numParts];
            if (saved != null)
                saved.CopyTo(low);
            else
                low.Fill(-1);
            Span<float> shown = stackalloc float[numParts];
            shown.Fill(1f);
            uint lostNow = 0u, lost = 0u, repaired = 0u;
            for (int part = 1; part < numParts; ++part)
            {
                int lowest = low[part];
                if (lowest < 0)
                {
                    if ((hit & 1u << part) == 0 || area[part] == 0)
                        continue;
                    if (alive[part] > 0)
                    {
                        float chance = (area[part] - alive[part]) / (float)area[part] - PartLossDeadShare;
                        if (chance <= 0f || !Loyalty.Random.RollDice(100f * chance))
                            continue;
                    }
                    lowest = alive[part];
                    lostNow |= 1u << part;
                }
                else if (alive[part] >= area[part])
                {
                    repaired |= 1u << part; // whole once a chain of whole parts it touches reaches the core
                    continue;
                }
                low[part] = lowest = Math.Min(lowest, alive[part]);
                shown[part] = (alive[part] - lowest) / (float)(area[part] - lowest);
                lost |= 1u << part;
            }
            if (lost != 0u || repaired != 0u)
                HoldByTouchingParts(hull.PartLinks, low, alive, shown, timeRuns, ref lost, ref lostNow, repaired);

            bool anyLost = lost != 0u;
            float leastBack = 1f;
            for (int part = 1; part < numParts; ++part)
                if ((lost & 1u << part) != 0)
                    leastBack = Math.Min(leastBack, shown[part]);
            if (timeRuns && (lostNow != 0u || !anyLost))
                InsidesHeatAtLoss = 1f;
            InsidesHeat = anyLost ? Math.Max(Derelict.MinInsidesHeat, InsidesHeatAtLoss * (1f - leastBack)) : 1f;

            // a new array only when it changes, as a save or the UI may be reading the one there
            if (timeRuns)
                LostPartLow = !anyLost ? null : saved != null && low.SequenceEqual(saved) ? saved : low.ToArray();
            PartShown = !anyLost ? null : PartShown is { } before && shown.SequenceEqual(before) ? before : shown.ToArray();
            if (lostNow != 0u && Active && !Dying)
                KillModulesUnder(lostNow);
            return lostNow;
        }

        // a whole part no chain of whole parts it touches ties to the core goes with the parts just lost, and a lost part shows no more than its best chain to the core does, staying lost, repaired, till that chain is whole; with no links found every part hangs on the core
        static void HoldByTouchingParts(uint[] links, Span<int> low, Span<int> alive, Span<float> shown, bool timeRuns, ref uint lost, ref uint lostNow, uint repaired)
        {
            int numParts = low.Length;
            if (links?.Length != numParts)
            {
                for (int part = 1; part < numParts; ++part)
                    if ((repaired & 1u << part) != 0)
                        low[part] = -1;
                return;
            }
            Span<float> reach = stackalloc float[numParts];
            ChainShown(links, shown, reach);
            uint stranded = 0u;
            for (int part = 1; part < numParts; ++part)
            {
                uint bit = 1u << part;
                if ((repaired & bit) != 0)
                {
                    if (reach[part] >= 1f)
                        low[part] = -1;
                    else
                        lost |= bit;
                }
                else if (low[part] < 0 && reach[part] < 1f && timeRuns)
                {
                    low[part] = alive[part];
                    shown[part] = 0f;
                    stranded |= bit;
                }
            }
            lost |= stranded;
            lostNow |= stranded;
            ChainShown(links, shown, reach);
            for (int part = 1; part < numParts; ++part)
                if ((lost & 1u << part) != 0)
                    shown[part] = Math.Min(shown[part], Math.Max(reach[part], 0f));
        }

        // each part's best chain of touching parts from the core, the least shown along it, -1 for none
        static void ChainShown(uint[] links, Span<float> shown, Span<float> reach)
        {
            int numParts = reach.Length;
            reach.Fill(-1f);
            reach[0] = 1f;
            uint done = 0u;
            for (;;)
            {
                int next = -1;
                for (int part = 0; part < numParts; ++part)
                    if ((done & 1u << part) == 0 && reach[part] >= 0f && (next < 0 || reach[part] > reach[next]))
                        next = part;
                if (next < 0)
                    return;
                done |= 1u << next;
                for (int part = 0; part < numParts; ++part)
                    if ((links[next] & 1u << part) != 0 && (done & 1u << part) == 0)
                        reach[part] = Math.Max(reach[part], Math.Min(reach[next], shown[part]));
            }
        }

        // the modules still alive under the parts just lost go with them, quietly, as long as the ship lives through it
        void KillModulesUnder(uint parts)
        {
            for (int i = 0; i < ModuleSlotList.Length && Active && !Dying; ++i)
            {
                ShipModule m = ModuleSlotList[i];
                if (m.Active && (parts & 1u << Grid.PartOf(m.Pos)) != 0)
                    m.DieWithItsPart();
            }
        }

        // a module under a part a derelict lost, once the hull's parts are known and the derelict's are of the same cut of the model
        bool UnderPartLostBy(float[] derelictShown, ShipModule m)
        {
            if (derelictShown == null || !Grid.GetParts(out int[] area) || derelictShown.Length != area.Length)
                return false;
            int part = Grid.PartOf(m.Pos);
            return derelictShown[part] < 1f;
        }

        // the parts just lost fly off the model as it is drawn now, for a ship the player sees
        internal void ThrowOffParts(uint lostNow, SceneObject model)
        {
            for (int part = 1; part < StaticMesh.MaxParts; ++part)
                if ((lostNow & 1u << part) != 0)
                    HullPartDebris.ThrowOff(this, model, part);
        }

        // a thruster on a lost part lights again once this much of the part is back
        public const float ThrusterBackShare = 0.75f;

        // a thruster on a lost part is out, as is its engine trail, till ThrusterBackShare of the part is back
        internal static bool ThrusterOut(int thruster, float[] shown, byte[] thrusterParts)
        {
            if (shown == null || thrusterParts == null || (uint)thruster >= (uint)thrusterParts.Length)
                return false;
            int part = thrusterParts[thruster];
            return part < shown.Length && shown[part] < ThrusterBackShare;
        }

        // how much of each hull part shows, counted again if a module died or came back since; on the sim thread, or for a ship that died
        internal float[] PartShownNow()
        {
            if (PartsOutdated)
                UpdateLostParts();
            return PartShown;
        }

        // each hull part shown, 1, or lost, 0, as counted now or, before the hull's model has loaded, as saved; null while no part is lost; lostNow, the parts this count lost
        internal float[] WholePartsNow(out uint lostNow)
        {
            lostNow = PartsOutdated ? UpdateLostParts() : 0u;
            float[] shown = PartShown;
            int[] low = LostPartLow;
            if (shown == null && low == null)
                return null;
            var whole = new float[shown?.Length ?? low.Length];
            for (int part = 0; part < whole.Length; ++part)
                whole[part] = (shown != null ? shown[part] < 1f : low[part] >= 0) ? 0f : 1f;
            return whole;
        }

        // a repair under a lost part, on a ship the player sees, sets off this many small flashes at random points on the part, one every RepairFlashGap seconds, sized as a blown-off part's; a repair there while they go off sets off no more
        public const int RepairFlashes = 10;
        public const float RepairFlashGap = 0.05f;

        // the parts a module was repaired under while lost since the ship's last update, each bit a part
        uint PartsRepaired;
        // each run of flashes going off, its part, the flashes it has left and the seconds to its next; null till its first run, then kept for the next; only the ship's own update reads and writes them
        Array<(int Part, int Left, float Next)> RepairFlashRuns;

        // a module was repaired under a lost part, maybe on another thread, as by a repair drone's beam
        internal void MarkPartRepaired(ShipModule m)
        {
            if (!HullPlatesOff && UnderLostPart(m, out int part))
                Interlocked.Or(ref PartsRepaired, 1u << part);
        }

        // a part repaired under in an update starts a run of flashes on it, the first now, unless one is going off on it, beside those on other parts, while the game runs; model, the ship's as placed by this update, null while the player does not see it, it launches or lands or its model is not made yet, which drops them
        void UpdateRepairFlashes(float dt, SceneObject model)
        {
            if (dt <= 0f || (PartsRepaired == 0u && (RepairFlashRuns == null || RepairFlashRuns.Count == 0)))
                return;
            uint repaired = Interlocked.Exchange(ref PartsRepaired, 0u);
            ParticleManager particles = Universe.Screen?.Particles;
            if (model == null || particles == null)
            {
                RepairFlashRuns?.Clear();
                return;
            }
            Array<(int Part, int Left, float Next)> runs = RepairFlashRuns;
            for (int i = 0; runs != null && i < runs.Count; ++i)
                repaired &= ~(1u << runs[i].Part);
            if (repaired != 0u)
            {
                runs ??= RepairFlashRuns = new();
                for (int part = 1; part < StaticMesh.MaxParts; ++part)
                    if ((repaired & 1u << part) != 0)
                        runs.Add((part, RepairFlashes, dt)); // its first flash in this update
            }
            if (runs == null)
                return;
            for (int i = runs.Count - 1; i >= 0; --i)
            {
                (int part, int left, float next) = runs[i];
                for (next -= dt; next <= 0f && left > 0; next += RepairFlashGap) // as many as are due, at a sim rate under 20 a second
                {
                    --left;
                    RepairFlash(part, model, particles);
                }
                if (left > 0)
                    runs[i] = (part, left, next);
                else
                    runs.RemoveAt(i);
            }
        }

        // a small flash at a random point on the part as the ship's model is placed, sent off at the ship's velocity
        void RepairFlash(int part, SceneObject model, ParticleManager particles)
        {
            if (!BaseHull.RandomPointOnPart(part, particles.Random, out XnaVector3 p, out float radius))
                return;
            XnaVector3 at = XnaVector3.Transform(p, model.World);
            particles.Flash.AddParticle(new Vector3(at.X, at.Y, at.Z), Velocity.ToVec3(), HullPartDebris.FlashScale(radius), Color.White);
        }

        // a module under a lost part shows no damage until the part is whole again
        public bool UnderLostPart(ShipModule m) => UnderLostPart(m, out _);

        bool UnderLostPart(ShipModule m, out int part)
        {
            part = -1;
            if (PartShown is not { } shown)
                return false;
            part = Grid.PartOf(m.Pos);
            return (uint)part < (uint)shown.Length && shown[part] < 1f;
        }

        // a recovered ship's lost parts are its derelict's, each coming back with its repairs from then on, their insides as hot as its; the recovery's damage takes none
        void KeepLostParts(float[] derelictShown, float derelictHeat)
        {
            if (derelictShown == null)
                return;
            var low = new int[derelictShown.Length];
            Array.Fill(low, -1);
            for (int part = 1; part < low.Length; ++part)
                if (derelictShown[part] < 1f)
                    low[part] = int.MaxValue; // its alive area at the next count
            LostPartLow = low;
            InsidesHeatAtLoss = derelictHeat;
            PartsOutdated = true;
        }
    }
}
