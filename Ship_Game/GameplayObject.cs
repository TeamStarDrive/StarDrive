using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public abstract class GameplayObject
    {
        public static GraphicsDevice device;
        public static AudioListener audioListener { get; set; }

        [XmlIgnore][JsonIgnore] public bool Active = true;
        [XmlIgnore][JsonIgnore] protected Cue dieCue;
        [XmlIgnore][JsonIgnore] public SolarSystem System { get; private set; }

        [Serialize(0)] public Vector2 Position;
        [Serialize(1)] public Vector2 Center;
        [Serialize(2)] public Vector2 Velocity;
        [Serialize(3)] public float Rotation;

        [Serialize(4)] public Vector2 Dimensions;
        [Serialize(5)] public float Radius = 1f;
        [Serialize(6)] public float Mass = 1f;
        [Serialize(7)] public float Health;

        [XmlIgnore][JsonIgnore] public GameplayObject LastDamagedBy;
        [XmlIgnore][JsonIgnore] public bool CollidedThisFrame;

        [XmlIgnore][JsonIgnore] public int SpatialIndex = -1;
        [XmlIgnore][JsonIgnore] public bool InDeepSpace => System == null;


        private static int GameObjIds;
        [XmlIgnore] [JsonIgnore] public int Id = ++GameObjIds;

        protected GameplayObject()
        {
        }

        public virtual bool Damage(GameplayObject source, float damageAmount)
        {
            return false;
        }

        public virtual void Die(GameplayObject source, bool cleanupOnly)
        {
            Active = false;
        }

        public string SystemName => System?.Name ?? "Deep Space";

        public static SpatialManager SpatialManagerForSystem(SolarSystem system)
            => system?.spatialManager ?? UniverseScreen.DeepSpaceManager;

        public SpatialManager ActiveSpatialManager => SpatialManagerForSystem(System);

        public T[] GetNearby<T>() where T : GameplayObject => SpatialManagerForSystem(System).GetNearby<T>(Position, Radius);

        public void SetSystem(SolarSystem system)
        {
            if (System == system)
            {
                if (SpatialIndex == -1) // not assigned to a SpatialManager yet?
                    SpatialManagerForSystem(system).Add(this);
                return;
            }

            // remove from old manager if it's assigned to a SpatialManager
            if (SpatialIndex != -1)
                SpatialManagerForSystem(System).Remove(this);

            if (this is Ship ship)
            {
                System?.ShipList.RemoveSwapLast(ship);
                system?.ShipList.AddUnique(ship);
            }
            System = system;

            // insert to new system OR deep space spatial managers:
            SpatialManagerForSystem(system).Add(this);
        }

        public virtual void Initialize()
        {
        }

        public virtual bool Touch(GameplayObject target)
        {
            return false; // by default, objects can't be touched
        }

        public virtual void Update(float elapsedTime)
        {
            CollidedThisFrame = false;
        }

        public void UpdateSystem(float elapsedTime)
        {
        }

        public override string ToString() => $"GameObj Id={Id} Pos={Position}";
    }
}