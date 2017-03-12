using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Xml.Serialization;

namespace Ship_Game.Gameplay
{
	public sealed class ModuleSlot
	{
		public Vector2 Position;
		public float facing;
		public Guid HangarshipGuid;
		public Restrictions Restrictions;
		public bool Powered;
		public bool CheckedConduits;
		public string SlotOptions;
		public ShipDesignScreen.ActiveModuleState state;
		public float ModuleHealth;
		public float Shield_Power;
		public bool isDummy;
		public string InstalledModuleUID;
        public ShipModule module;

        [XmlIgnore][JsonIgnore]
        public Ship Parent;

        public ModuleSlot()
		{
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			module.Draw(spriteBatch);
		}

		public Ship GetParent()
		{
			return Parent;
		}

        public bool Initialize(bool fromSave = false)
        {
            if (module != null)
                Log.Warning("A module was reinitialized for no reason. This is a bug");

            if (InstalledModuleUID == "Dummy")
            {
                module = ResourceManager.CreateModuleFromUid(InstalledModuleUID);
                return true;
            }
            if (InstalledModuleUID != null)
            {
                module = ResourceManager.CreateModuleFromUid(InstalledModuleUID);
                module.installedSlot = this;
                module.SetParent(Parent);
                module.facing = facing;

                if (fromSave)
                    module.InitializeFromSave(Position);
                else
                    module.Initialize(Position);
                return true;
            }
            return false;
        }

		public void Update(float elapsedTime)
		{
			module.Update(elapsedTime);
		}
	}
}