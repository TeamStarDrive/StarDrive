using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDGraphics;
using SynapseGaming.LightingSystem.Rendering;

using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Vector2d = SDGraphics.Vector2d;
using Vector3d = SDGraphics.Vector3d;
using Matrix = SDGraphics.Matrix;

using Viewport = Microsoft.Xna.Framework.Graphics.Viewport;

namespace Ship_Game
{
    public static class Matrices
    {
        // this is a copy of XNA Viewport.Project, with the third Matrix optimized out
        public static Vector2 ProjectTo2D(this Viewport viewport, Vector3 source, in Matrix projection, in Matrix view)
        {
            view.Multiply(projection, out Matrix viewProjection);
            Vector3 clipSpacePoint = source.Transform(viewProjection);
            float len = source.X * viewProjection.M14
                      + source.Y * viewProjection.M24
                      + source.Z * viewProjection.M34
                      + viewProjection.M44;
            if (!len.AlmostEqual(1f)) // normalize
                clipSpacePoint /= len;
            return new Vector2( (clipSpacePoint.X + 1.0f) * 0.5f * viewport.Width,
                               (-clipSpacePoint.Y + 1.0f) * 0.5f * viewport.Height);
        }

        public static Vector2 Measure2D(this Viewport viewport, Vector3 a, Vector3 b, in Matrix projection, in Matrix view)
        {
            Vector2 x = ProjectTo2D(viewport, a, projection, view);
            Vector2 y = ProjectTo2D(viewport, b, projection, view);
            return y - x;
        }

        public static Vector3 UnprojectToWorld(this Viewport viewport, int screenX, int screenY, float depth,
                                               ref Matrix projection, ref Matrix view)
        {
            Matrix.Multiply(ref view, ref projection, out Matrix viewProjection);
            Matrix.Invert(viewProjection, out Matrix invViewProj);

            var source = new Vector3(
                (screenX - viewport.X) / (viewport.Width * 2.0f) - 1.0f,
                (screenY - viewport.Y) / (viewport.Height * 2.0f) - 1.0f,
                (depth - viewport.MinDepth) / (viewport.MaxDepth - viewport.MinDepth));

            Vector3 worldPos = source.Transform(invViewProj);
            float len = source.X * invViewProj.M14 + source.Y * invViewProj.M24 + source.Z * invViewProj.M34 + invViewProj.M44;
            if (!len.AlmostEqual(1f))
                worldPos /= len;
            return worldPos;
        }

        // this is a copy of XNA Viewport.Project, with the third Matrix optimized out
        public static Vector2d ProjectTo2D(this Viewport viewport, in Vector3d source,
                                           in Matrix projection, in Matrix view)
        {
            view.Multiply(projection, out Matrix viewProjection);
            return ProjectTo2D(viewport, source, viewProjection);
        }

        // this is a copy of XNA Viewport.Project, with only a single ViewProjection matrix parameter
        public static Vector2d ProjectTo2D(this Viewport viewport, in Vector3d source,
                                           in Matrix viewProjection)
        {
            Vector3d clipSpacePoint = source.Transform(viewProjection);
            double len = source.X * viewProjection.M14
                       + source.Y * viewProjection.M24
                       + source.Z * viewProjection.M34
                       + viewProjection.M44;
            if (!len.AlmostEqual(1.0)) // normalize
                clipSpacePoint /= len;
            return new Vector2d( (clipSpacePoint.X + 1.0) * 0.5 * viewport.Width,
                                (-clipSpacePoint.Y + 1.0) * 0.5 * viewport.Height);
        }

        public static Vector3d Unproject(this Viewport viewport, in Vector3d source,
                                         in Matrix projection, in Matrix view)
        {
            view.Multiply(projection, out Matrix viewProjection);
            Matrix.Invert(viewProjection, out Matrix invViewProj);
            return Unproject(viewport, in source, in invViewProj);
        }

        public static Vector3d Unproject(this Viewport viewport, in Vector3d source,
                                         in Matrix inverseViewProjection)
        {
            Vector3d src;
            src.X =  ((source.X - viewport.X) / viewport.Width  * 2.0 - 1.0);
            src.Y = -((source.Y - viewport.Y) / viewport.Height * 2.0 - 1.0);
            src.Z =  ((source.Z - viewport.MinDepth) / (viewport.MaxDepth - viewport.MinDepth));

            Vector3d worldPos = src.Transform(in inverseViewProjection);
            double a = (src.X*inverseViewProjection.M14 +
                        src.Y*inverseViewProjection.M24 +
                        src.Z*inverseViewProjection.M34) + inverseViewProjection.M44;
            if (!a.AlmostEqual(1.0)) // normalize
                worldPos /= a;
            return worldPos;
        }
        
        /// <summary>
        /// Unprojects a screenSpace 2D point into a 3D world position
        /// </summary>
        public static Vector3d Unproject(this Viewport viewport, Vector2 screenSpace, double ZPlane,
                                         in Matrix projection, in Matrix view)
        {
            view.Multiply(projection, out Matrix viewProjection);
            Matrix.Invert(viewProjection, out Matrix invViewProj);
            return Unproject(viewport, screenSpace, ZPlane, invViewProj);
        }
        /// <summary>
        /// Unprojects a screenSpace 2D point into a 3D world position
        /// </summary>
        public static Vector3d Unproject(this Viewport viewport, Vector2 screenSpace, double ZPlane,
                                         in Matrix inverseViewProjection)
        {
            // nearPoint is the point inside the camera lens
            Vector3d nearPoint = viewport.Unproject(new(screenSpace, 0.0), in inverseViewProjection);
            // farPoint points away into the world
            Vector3d farPoint = viewport.Unproject(new(screenSpace, 1.0), in inverseViewProjection);

            // get the direction towards the world plane
            Vector3d dir = (farPoint - nearPoint).Normalized();

            // FIX: potential failure point here, dir.Z might be 0
            if (dir.Z.AlmostEqual(0.0, 0.000000001))
            {
                Log.Error($"UnprojectToWorldPosition3D dir.Z zero! dir={dir} screenPos={screenSpace}");
                return new(nearPoint.X, nearPoint.Y, 0.0);
            }

            // calculate distance of intersection point from nearPoint
            double distance = ((-nearPoint.Z + ZPlane) / dir.Z);
            return nearPoint + (dir * distance);
        }

        public static Matrix CreateLookAtDown(double camX, double camY, double camZ)
        {
            return Matrix.CreateLookAt(new Vector3((float)camX, (float)camY, (float)camZ),
                                       new Vector3((float)camX, (float)camY, 0.0f),
                                       new Vector3(0.0f, -1f, 0.0f));
        }

        // Creates an Affine World transformation Matrix
        public static Matrix AffineTransform(in Vector3 position, in Vector3 rotationRadians, float scale)
        {
            return Matrix.CreateScale(scale)
                 * rotationRadians.RadiansToRotMatrix()
                 * Matrix.CreateTranslation(position);
        }

        // Sets the Affine World transformation Matrix for this SceneObject
        public static void AffineTransform(this SceneObject so, in Vector3 position, in Vector3 rotationRadians, float scale)
        {
            so.World = Matrix.CreateScale(scale)
                     * rotationRadians.RadiansToRotMatrix()
                     * Matrix.CreateTranslation(position);
        }

        // Sets the Affine World transformation Matrix for this SceneObject
        public static void AffineTransform(this SceneObject so, in Vector3 position, in Vector3 rotationRadians, in Vector3 scale)
        {
            so.World = Matrix.CreateScale(scale)
                     * rotationRadians.RadiansToRotMatrix()
                     * Matrix.CreateTranslation(position);
        }

        public static Matrix RadiansToRotMatrix(this Vector3 rotation)
        {
            return Matrix.CreateRotationX(rotation.X)
                 * Matrix.CreateRotationY(rotation.Y)
                 * Matrix.CreateRotationZ(rotation.Z);
        }
    }
}
