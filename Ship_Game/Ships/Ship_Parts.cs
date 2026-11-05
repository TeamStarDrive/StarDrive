using System;
using System.Threading;
using Ship_Game.Data.Mesh;
using Ship_Game.Data.Serialization;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        // a hull part can go once more than this share of its slot area is dead: an update with a death under it rolls the share dead past it
        public const float PartLossDeadShare = 0.75f;

        // each hull part's slot area alive at its lowest since it was lost, -1 while it is on; null while no part is lost
        [StarData] int[] LostPartLow;

        // how much of each hull part shows, 1 whole, below 1 lost and coming back with its repairs since; null while no part is lost
        public float[] PartShown { get; private set; }

        // a module died or came back since the parts were counted, or they never were
        bool PartsOutdated = true;
        // the parts a module died under since the parts were counted, each bit a part, saved so a death just before a save still counts
        [StarData] uint PartsHit;

        // a module died: its part may be lost at the next count, and any part while the hull's model has not loaded; nothing on a hull in one piece
        void MarkPartHit(ShipModule m)
        {
            ShipHull hull = BaseHull;
            if (hull.ModelInOnePiece)
                return;
            int part = Grid.PartOf(m.Pos);
            if (part < 0 && hull.NumParts > 0 && Grid.GetParts(out _))
                part = Grid.PartOf(m.Pos);
            Interlocked.Or(ref PartsHit, part < 0 ? uint.MaxValue : 1u << part);
            PartsOutdated = true;
        }

        // a part may be lost by the roll of an update with a death under it, and comes back as its modules do; a part with no modules stays on; returns the parts it lost
        uint UpdateLostParts()
        {
            ShipHull hull = BaseHull;
            if (hull.NumParts == 0)
            {
                if (hull.ModelInOnePiece)
                    PartsOutdated = false; // until a module comes back
                return 0u;
            }
            if (!Grid.GetParts(out int[] area))
                return 0u;
            PartsOutdated = false; // before the count, so a module dying meanwhile is counted on the next update
            uint hit = Interlocked.Exchange(ref PartsHit, 0u);

            int numParts = area.Length;
            Span<int> alive = stackalloc int[numParts];
            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule m = ModuleSlotList[i];
                if (m.Active)
                    alive[Grid.PartOf(m.Pos)] += m.Area;
            }

            int[] saved = LostPartLow?.Length == numParts ? LostPartLow : null; // one saved with another cut of the model starts over
            Span<int> low = stackalloc int[numParts];
            if (saved != null)
                saved.CopyTo(low);
            else
                low.Fill(-1);
            Span<float> shown = stackalloc float[numParts];
            shown.Fill(1f);
            bool anyLost = false;
            uint lostNow = 0u;
            for (int part = 1; part < numParts; ++part)
            {
                int lowest = low[part];
                if (lowest < 0)
                {
                    if ((hit & 1u << part) == 0 || area[part] == 0)
                        continue;
                    float pastShare = (area[part] - alive[part]) / (float)area[part] - PartLossDeadShare;
                    if (pastShare <= 0f || !Loyalty.Random.RollDice(100f * pastShare))
                        continue;
                    lowest = alive[part];
                    lostNow |= 1u << part;
                }
                else if (alive[part] >= area[part])
                {
                    low[part] = -1;
                    continue;
                }
                low[part] = lowest = Math.Min(lowest, alive[part]);
                shown[part] = (alive[part] - lowest) / (float)(area[part] - lowest);
                anyLost = true;
            }

            // a new array only when it changes, as a save or the UI may be reading the one there
            LostPartLow = !anyLost ? null : saved != null && low.SequenceEqual(saved) ? saved : low.ToArray();
            PartShown = !anyLost ? null : PartShown is { } before && shown.SequenceEqual(before) ? before : shown.ToArray();
            return lostNow;
        }

        // the parts just lost fly off the model as it is drawn now, for a ship the player sees
        internal void ThrowOffParts(uint lostNow, SceneObject model)
        {
            for (int part = 1; part < StaticMesh.MaxParts; ++part)
                if ((lostNow & 1u << part) != 0)
                    HullPartDebris.ThrowOff(this, model, part);
        }

        // a thruster on a part not whole is out, as is its engine trail
        internal static bool ThrusterOut(int thruster, float[] shown, byte[] thrusterParts)
        {
            if (shown == null || thrusterParts == null || (uint)thruster >= (uint)thrusterParts.Length)
                return false;
            int part = thrusterParts[thruster];
            return part < shown.Length && shown[part] < 1f;
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

        // a module under a lost part shows no damage until the part is whole again
        public bool UnderLostPart(ShipModule m)
        {
            if (PartShown is not { } shown)
                return false;
            int part = Grid.PartOf(m.Pos);
            return (uint)part < (uint)shown.Length && shown[part] < 1f;
        }

        // a recovered ship's lost parts are its derelict's, each coming back with its repairs from then on; the recovery's damage takes none
        void KeepLostParts(float[] derelictShown)
        {
            if (derelictShown == null)
                return;
            var low = new int[derelictShown.Length];
            Array.Fill(low, -1);
            for (int part = 1; part < low.Length; ++part)
                if (derelictShown[part] < 1f)
                    low[part] = int.MaxValue; // its alive area at the next count
            LostPartLow = low;
            PartsOutdated = true;
        }
    }
}
