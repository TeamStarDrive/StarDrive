using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
		private Ship Parent;
        public ShipModule module;

		public ModuleSlot()
		{
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			this.module.Draw(spriteBatch);
		}

		public Ship GetParent()
		{
			return this.Parent;
		}

        private bool InitializeModule()
        {
            if (InstalledModuleUID != "Dummy" && InstalledModuleUID != null)
            {
                if (module != null)
                    Log.Warning("A module was reinitialized for no reason. This is a bug");
                module = ResourceManager.CreateModuleFromUid(InstalledModuleUID);
                module.installedSlot = this;
                module.SetParent(Parent);
                module.facing = facing;
                return true;
            }
            return false;
        }

		public void Initialize()
		{
            if (InitializeModule())
                module.Initialize(Position);
        }

        public void InitializeFromSave()
        {
            if (InitializeModule())
                module.InitializeFromSave(Position);
        }

		public void InitializeForLoad()
		{
			module = ResourceManager.CreateModuleFromUid(InstalledModuleUID);
		}

		public void SetParent(Ship ship)
		{
			this.Parent = ship;
		}

		public void Update(float elapsedTime)
		{
			this.module.Update(elapsedTime);
		}
	}
}