namespace Ship_Game.Ships
{
    public struct ModuleCache
    {
        public readonly ShipModule[] Modules;
        public readonly ShipModule[] Shields;
        public readonly ShipModule[] Amplifiers;

        public ModuleCache(ShipModule[] moduleList)
        {
            Modules = moduleList;
            var shields    = new Array<ShipModule>();
            var amplifiers = new Array<ShipModule>();

            for (int i = 0; i < moduleList.Length; ++i)
            {
                ShipModule module = moduleList[i];
                if (module.shield_power_max > 0f)
                    shields.Add(module);

                if (module.AmplifyShields > 0f)
                    amplifiers.Add(module);
            }

            Shields    = shields.ToArray();
            Amplifiers = amplifiers.ToArray();
        }
    }
}