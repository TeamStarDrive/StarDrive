using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Ship_Game.Gameplay
{
    [DebuggerDisplay("UID = {InstalledModuleUID}  Module = {module}")]
    public sealed class ModuleSlot
    {
        public Vector2 Position;
        public string InstalledModuleUID;
        public Guid HangarshipGuid;
        public float Health;
        public float Shield_Power;
        public float facing;
        public ShipDesignScreen.ActiveModuleState state;
        public Restrictions Restrictions;
        public string SlotOptions;

        [XmlIgnore] [JsonIgnore] public bool Powered;
        [XmlIgnore] [JsonIgnore] public bool CheckedConduits;
        [XmlIgnore] [JsonIgnore] public bool isDummy;
        [XmlIgnore] [JsonIgnore] public ShipModule module;
        [XmlIgnore] [JsonIgnore] public Ship Parent;

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

        public bool Initialize()
        {
            if (module != null)
                Log.Error("A module was reinitialized for no reason. This is a bug");

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
                module.Initialize(Position);
                return true;
            }
            return false;
        }

        public void Update(float elapsedTime)
        {
            module.Update(elapsedTime);
        }

        public Color GetHealthStatusColor()
        {
            if (module == null)
                return Color.Purple;

            float healthPercent = module.Health / module.HealthMax;

            if (Empire.Universe.Debug && module.isExternal)
            {
                if (healthPercent >= 0.5f) return Color.Blue;
                if (healthPercent >  0.0f) return Color.DarkSlateBlue;
                return Color.DarkSlateGray;
            }

            if (healthPercent >= 0.90f) return Color.Green;
            if (healthPercent >= 0.65f) return Color.GreenYellow;
            if (healthPercent >= 0.45f) return Color.Yellow;
            if (healthPercent >= 0.15f) return Color.OrangeRed;
            if (healthPercent >  0.00f) return Color.Red;
            return Color.Black;
        }

        // @todo Find a way to get rid of this duplication ?
        public Color GetHealthStatusColorWhite()
        {
            float healthPercent = module.Health / module.HealthMax;

            if (healthPercent >= 0.90f) return Color.White;
            if (healthPercent >= 0.65f) return Color.GreenYellow;
            if (healthPercent >= 0.45f) return Color.Yellow;
            if (healthPercent >= 0.15f) return Color.OrangeRed;
            if (healthPercent >  0.00f) return Color.Red;
            return Color.Black;
        }
    }
}