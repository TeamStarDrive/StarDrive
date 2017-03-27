using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Ship_Game.Gameplay
{
    [DebuggerDisplay("UID = {InstalledModuleUID}  Module = {Module}")]
    public sealed class ModuleSlot
    {
        public Vector2 Position;
        public string InstalledModuleUID;
        public Guid HangarshipGuid;
        public float Health;
        public float ShieldPower;
        public float Facing;
        public ShipDesignScreen.ActiveModuleState State; // @todo This duplicates Facing... ?? Remove this
        public Restrictions Restrictions;
        public string SlotOptions;

        [XmlIgnore] [JsonIgnore] public bool Powered;
        [XmlIgnore] [JsonIgnore] public bool CheckedConduits;
        [XmlIgnore] [JsonIgnore] public ShipModule Module;
        [XmlIgnore] [JsonIgnore] public Ship Parent;

        public void Draw(SpriteBatch spriteBatch)
        {
            Module.Draw(spriteBatch);
        }

        public Ship GetParent()
        {
            return Parent;
        }

        public bool Initialize()
        {
            if (Module != null)
                Log.Error("A module was reinitialized for no reason. This is a bug");

            if (InstalledModuleUID == "Dummy")
            {
                Module = ResourceManager.CreateModuleFromUid(InstalledModuleUID);
                return true;
            }
            if (InstalledModuleUID != null)
            {
                Module = ResourceManager.CreateModuleFromUid(InstalledModuleUID);
                Module.installedSlot = this;
                Module.SetParent(Parent);
                Module.facing = Facing;
                Module.Initialize(Position);
                return true;
            }
            return false;
        }

        public void Update(float elapsedTime)
        {
            Module.Update(elapsedTime);
        }

        public Color GetHealthStatusColor()
        {
            if (Module == null)
                return Color.Purple;

            float healthPercent = Module.Health / Module.HealthMax;

            if (Empire.Universe.Debug && Module.isExternal)
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
            float healthPercent = Module.Health / Module.HealthMax;

            if (healthPercent >= 0.90f) return Color.White;
            if (healthPercent >= 0.65f) return Color.GreenYellow;
            if (healthPercent >= 0.45f) return Color.Yellow;
            if (healthPercent >= 0.15f) return Color.OrangeRed;
            if (healthPercent >  0.00f) return Color.Red;
            return Color.Black;
        }
    }
}