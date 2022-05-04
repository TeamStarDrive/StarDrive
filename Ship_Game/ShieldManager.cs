using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public sealed class ShieldManager
    {
        static readonly BatchRemovalCollection<Shield> ShieldList = new BatchRemovalCollection<Shield>();
        static readonly BatchRemovalCollection<Shield> PlanetaryShieldList = new BatchRemovalCollection<Shield>();

        static Model     ShieldModel;
        static Texture2D ShieldTexture;
        static Texture2D GradientTexture;
        static Effect    ShieldEffect;

        public static void LoadContent(GameContentManager content)
        {
            GameLoadingScreen.SetStatus("LoadShields");
            ShieldModel     = content.Load<Model>("Model/Projectiles/shield");
            ShieldTexture   = content.Load<Texture2D>("Model/Projectiles/shield_d.dds");
            GradientTexture = content.Load<Texture2D>("Model/Projectiles/shieldgradient");
            ShieldEffect    = content.Load<Effect>("Effects/scale");
        }

        public static void UnloadContent()
        {
            ShieldList.Clear();
            PlanetaryShieldList.Clear();
            ShieldModel = null;
            ShieldTexture = null;
            GradientTexture = null;
            ShieldEffect = null;
        }

        public static void Draw(UniverseScreen u, in Matrix view, in Matrix projection)
        {
            using (ShieldList.AcquireReadLock())
            {
                for (int i = 0; i < ShieldList.Count; i++)
                {
                    Shield shield = ShieldList[i];
                    if (shield.TexScale > 0f && shield.InFrustum(u))
                        DrawShield(shield, view, projection);
                }
            }
            using (PlanetaryShieldList.AcquireReadLock())
            {
                for (int i = 0; i < PlanetaryShieldList.Count; i++)
                {
                    Shield shield = PlanetaryShieldList[i];
                    if (shield.TexScale > 0f && shield.InFrustum(u))
                        DrawShield(shield, view, projection);
                }
            }
        }

        static void DrawShield(Shield shield, in Matrix view, in Matrix projection)
        {
            shield.UpdateWorldTransform();
            ShieldEffect.Parameters["World"]       .SetValue(shield.World);
            ShieldEffect.Parameters["View"]        .SetValue(view);
            ShieldEffect.Parameters["Projection"]  .SetValue(projection);
            ShieldEffect.Parameters["tex"]         .SetValue(ShieldTexture);
            ShieldEffect.Parameters["AlphaMap"]    .SetValue(GradientTexture);
            ShieldEffect.Parameters["scale"]       .SetValue(shield.TexScale);
            ShieldEffect.Parameters["displacement"].SetValue(shield.Displacement);
            ShieldEffect.CurrentTechnique = ShieldEffect.Techniques["Technique1"];

            foreach (ModelMesh mesh in ShieldModel.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                    part.Effect = ShieldEffect;
                mesh.Draw();
            }
        }

        public static void Clear()
        {
            ShieldList.Clear();
            PlanetaryShieldList.Clear();
        }

        public static Shield AddPlanetaryShield(Vector2 position)
        {
            var shield = new Shield(position);
            PlanetaryShieldList.Add(shield);
            return shield;
        }
    
        public static Shield AddShield(GameplayObject owner, float rotation, Vector2 center)
        {            
            var shield = new Shield(owner, rotation, center);
            ShieldList.Add(shield);
            return shield;
        }

        public static void RemoveShieldLights(UniverseScreen u, IEnumerable<ShipModule> shields)
        {
            foreach (ShipModule shield in shields)
                shield.Shield.RemoveLight(u);
        }

        public static void Update(UniverseScreen u)
        {
            using (PlanetaryShieldList.AcquireReadLock())
            {
                for (int i = 0; i < PlanetaryShieldList.Count; i++)
                {
                    Shield shield = PlanetaryShieldList[i];
                    if (shield.TexScale > 0f)
                    {
                        shield.UpdateLightIntensity(2.45f);
                        shield.Displacement += 0.085f;
                        shield.TexScale -= 0.185f;
                    }
                }
            }

            using (ShieldList.AcquireReadLock())
            {
                for (int i = 0; i < ShieldList.Count; i++)
                {
                    Shield shield = ShieldList[i];
                    if (shield.TexScale > 0f)
                    {
                        shield.UpdateLightIntensity(2.45f);
                        shield.Displacement += 0.025f;
                        shield.TexScale -= 0.004f;
                    }
                }
            }

            using (ShieldList.AcquireWriteLock())
            {
                for (int i = ShieldList.Count - 1; i >= 0; --i)
                {
                    Shield shield = ShieldList[i];
                    if (shield.Owner != null && !shield.Owner.Active)
                    {
                        ShieldList.RemoveAtSwapLast(i);
                        shield.RemoveLight(u);
                    }
                }
            }
        }
    }
}