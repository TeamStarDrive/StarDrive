using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Matrix = SDGraphics.Matrix;

namespace Ship_Game
{
    public sealed class ShieldManager
    {
        public static BatchRemovalCollection<Shield> VisibleShields = new ();
        public static BatchRemovalCollection<Shield> VisiblePlanetShields = new();

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
            ShieldModel = null;
            ShieldTexture = null;
            GradientTexture = null;
            ShieldEffect = null;
        }

        public static void Draw(UniverseScreen u, in Matrix view, in Matrix projection)
        {
            using (VisibleShields.AcquireReadLock())
            {
                for (int i = 0; i < VisibleShields.Count; i++)
                {
                    Shield shield = VisibleShields[i];
                    if (shield.LightEnabled && shield.InFrustum(u))
                        DrawShield(shield, view, projection);
                }
            }
            using (VisiblePlanetShields.AcquireReadLock())
            {
                for (int i = 0; i < VisiblePlanetShields.Count; i++)
                {
                    Shield shield = VisiblePlanetShields[i];
                    if (shield.LightEnabled && shield.InFrustum(u))
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

        public static void RemoveShieldLights(UniverseScreen u, IEnumerable<ShipModule> shields)
        {
            foreach (ShipModule shield in shields)
                shield.Shield.RemoveLight(u);
        }

        public static void Update(UniverseScreen u)
        {
            using (VisiblePlanetShields.AcquireReadLock())
            {
                for (int i = 0; i < VisiblePlanetShields.Count; i++)
                {
                    Shield shield = VisiblePlanetShields[i];
                    if (shield.LightEnabled)
                    {
                        shield.UpdateLightIntensity(-2.45f);
                        shield.UpdateDisplacement(0.085f);
                        shield.UpdateTexScale(-0.185f);
                    }
                }
            }

            using (VisibleShields.AcquireReadLock())
            {
                for (int i = 0; i < VisibleShields.Count; i++)
                {
                    Shield shield = VisibleShields[i];
                    if (shield.LightEnabled)
                    {
                        shield.UpdateLightIntensity(-0.002f);
                        shield.UpdateDisplacement(0.04f);
                        shield.UpdateTexScale(-0.01f);
                    }
                }
            }
        }

        public static void SetVisibleShields(BatchRemovalCollection<Shield> visibleShields)
        {
            VisibleShields = visibleShields;
        }

        public static void SetVisiblePlanetShields(BatchRemovalCollection<Shield> visibleShields)
        {
            VisiblePlanetShields = visibleShields;
        }
    }
}