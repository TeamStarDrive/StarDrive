using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ship_Game.Gameplay
{
	public class ModuleSlot
	{
		public Vector2 Position;

		public float facing;

		public Guid HangarshipGuid;

		public Ship_Game.Gameplay.Restrictions Restrictions;

		public bool Powered;

		public bool CheckedConduits;

		public string SlotOptions;

		public ShipDesignScreen.ActiveModuleState state;

		public float ModuleHealth;

		public float Shield_Power;

		public bool isDummy;

		public string InstalledModuleUID;

		private Ship Parent;

		public ShipModule module
		{
			get;
			set;
		}

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

		public void Initialize()
		{
			if (this.InstalledModuleUID != "Dummy" && this.InstalledModuleUID != null)
			{
				this.module = ResourceManager.GetModule(this.InstalledModuleUID);
				this.module.installedSlot = this;
				this.module.SetParent(this.Parent);
				this.module.facing = this.facing;
				this.module.Initialize(this.Position);
			}
		}

		public void InitializeForLoad()
		{
			this.module = ResourceManager.ShipModulesDict[this.InstalledModuleUID];
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