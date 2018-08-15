using System;

namespace Ship_Game
{
    public static class SparseEmpireMap
    {
        public static bool IsSet(this Empire[] empires, Empire empire)
        {
            int idx = empire.Id - 1;
            if (empires.Length <= idx)
                return false; // out of bounds, thus not set

            bool explored = empires[idx] != null;
            return explored;
        }
        public static void Set(this Empire[] emps, ref Empire[] empires, Empire empire)
        {
            int idx = empire.Id - 1;
            if (empires.Length < EmpireManager.NumEmpires)
            {
                Array.Resize(ref empires, EmpireManager.NumEmpires);
            }
            empires[idx] = empire;
        }
    }

    public class Explorable
    {
        // this is a sparse map where [Empire.Id-1] is the index
        private Empire[] ExploredBy = Empty<Empire>.Array;

        public bool IsExploredBy(Empire empire)  => ExploredBy.IsSet(empire);
        public void SetExploredBy(Empire empire) => ExploredBy.Set(ref ExploredBy, empire);
        public void SetExploredBy(string empireName) => SetExploredBy(EmpireManager.GetEmpireByName(empireName));
        public void SetExploredBy(Array<string> empireNames)
        {
            if (empireNames == null)
                return;
            foreach (string empireName in empireNames)
                SetExploredBy(empireName);
        }

        public Empire[] ExploredByEmpires
        {
            get
            {
                // probe first:
                int count = 0;
                for (int i = 0; i < ExploredBy.Length; ++i)
                    if (ExploredBy[i] != null)
                        ++count;

                // single alloc
                var empires = new Empire[count];
                for (int i = 0, n = 0; i < ExploredBy.Length; ++i)
                {
                    Empire empire = ExploredBy[i];
                    if (empire != null)
                        empires[n++] = empire;
                }
                return empires;
            }
        }
    }
}
