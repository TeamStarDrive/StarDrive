using System;
using SDUtils;

namespace Ship_Game
{
    public static class EmpireFlatMap
    {
        static object LockResize = new object();
        public static bool FlatMapIsSet(this Empire[] empires, Empire empire)
        {
            int idx = empire.Id - 1;
            if (empires.Length <= idx)
                return false; // out of bounds, thus not set

            bool exists = empires[idx] != null; // is it set?
            return exists;
        }
        public static void FlatMapSet(this Empire[] emps, ref Empire[] empires, Empire empire)
        {
            if (empires.Length < EmpireManager.NumEmpires)
            {
                lock (LockResize)
                    Array.Resize(ref empires, EmpireManager.NumEmpires);
            }

            int idx = empire.Id - 1;
            empires[idx] = empire; // set it so
        }
        public static void FlatMapUnset(this Empire[] emps, ref Empire[] empires, Empire empire)
        {
            if (empires.Length < EmpireManager.NumEmpires)
                Array.Resize(ref empires, EmpireManager.NumEmpires);
            
            int idx = empire.Id - 1;
            empires[idx] = null; // clear it
        }
    }
}
