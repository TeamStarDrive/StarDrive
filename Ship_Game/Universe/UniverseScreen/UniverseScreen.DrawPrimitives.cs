using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed partial class UniverseScreen
    {
        // this does some magic to convert a game position/coordinate to a drawable screen position
        public Vector2 ProjectToScreenPosition(Vector2 posInWorld, float zAxis = 0f)
        {
            //return Viewport.Project(position.ToVec3(zAxis), projection, view, Matrix.Identity).ToVec2();
            return Viewport.ProjectTo2D(posInWorld.ToVec3(zAxis), ref projection, ref view);
        }

        public void ProjectToScreenCoords(Vector2 posInWorld, float zAxis, float sizeInWorld, out Vector2 posOnScreen, out float sizeOnScreen)
        {
            posOnScreen  = ProjectToScreenPosition(posInWorld, zAxis);
            sizeOnScreen = ProjectToScreenPosition(new Vector2(posInWorld.X + sizeInWorld, posInWorld.Y),zAxis).Distance(ref posOnScreen);
        }

        public void ProjectToScreenCoords(Vector2 posInWorld, float sizeInWorld, out Vector2 posOnScreen, out float sizeOnScreen, float zAxis = 0)
        {
            ProjectToScreenCoords(posInWorld, zAxis, sizeInWorld, out posOnScreen, out sizeOnScreen);
        }

        public void ProjectToScreenCoords(Vector2 posInWorld, float widthInWorld, float heightInWorld, 
                                       out Vector2 posOnScreen, out float widthOnScreen, out float heightOnScreen)
        {
            posOnScreen    = ProjectToScreenPosition(posInWorld);
            Vector2 size   = ProjectToScreenPosition(new Vector2(posInWorld.X + widthInWorld, posInWorld.Y + heightInWorld)) - posOnScreen;
            widthOnScreen  = Math.Abs(size.X);
            heightOnScreen = Math.Abs(size.Y);
        }

        public void ProjectToScreenCoords(Vector2 posInWorld, Vector2 sizeInWorld, out Vector2 posOnScreen, out Vector2 sizeOnScreen)
        {
            posOnScreen  = ProjectToScreenPosition(posInWorld);
            Vector2 size = ProjectToScreenPosition(new Vector2(posInWorld.X + sizeInWorld.X, posInWorld.Y + sizeInWorld.Y)) - posOnScreen;
            sizeOnScreen = new Vector2(Math.Abs(size.X), Math.Abs(size.Y));
        }

        public Vector2 ProjectToScreenSize(float widthInWorld, float heightInWorld)
        {
            float width = ProjectToScreenSize(widthInWorld);
            float height = ProjectToScreenSize(heightInWorld);

            return new Vector2(width, height);
        }

        public float ProjectToScreenSize(float sizeInWorld)
        {
            Vector2 zero = ProjectToScreenPosition(Vector2.Zero);
            return zero.Distance(ProjectToScreenPosition(new Vector2(sizeInWorld, 0f)));
        }

        public Vector3 UnprojectToWorldPosition3D(Vector2 screenSpace)
        {
            Vector3 pos = Viewport.Unproject(new Vector3(screenSpace, 0f), projection, view, Matrix.Identity);
            Vector3 dir = Viewport.Unproject(new Vector3(screenSpace, 1f), projection, view, Matrix.Identity) - pos;
            dir.Normalize();
            float num = -pos.Z / dir.Z;
            return (pos + num * dir);
        }

        public Vector2 UnprojectToWorldPosition(Vector2 screenSpace)
        {
            return UnprojectToWorldPosition3D(screenSpace).ToVec2();
        }


        // projects the line from World positions into Screen positions, then draws the line
        public Vector2 DrawLineProjected(Vector2 startInWorld, Vector2 endInWorld, Color color, 
                                         float zAxis = 0f, float zAxisStart = -1f)
        {
            zAxisStart = zAxisStart < 0f ? zAxis : zAxisStart;
            Vector2 projPos = ProjectToScreenPosition(startInWorld, zAxisStart);
            DrawLine(projPos, ProjectToScreenPosition(endInWorld, zAxis), color);
            return projPos;
        }

        public void DrawLineWideProjected(Vector2 startInWorld, Vector2 endInWorld, Color color, float thickness)
        {
            Vector2 projPos = ProjectToScreenPosition(startInWorld);
            DrawLine(projPos, ProjectToScreenPosition(endInWorld), color, thickness);
        }

        public Vector2 DrawLineToPlanet(Vector2 startInWorld, Vector2 endInWorld, Color color)
            => DrawLineProjected(startInWorld, endInWorld, color, 2500);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircleProjected(Vector2 posInWorld, float radiusInWorld, Color color, float thickness = 1f)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2 screenPos, out float screenRadius);
            DrawCircle(screenPos, screenRadius, color, thickness);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircleProjected(Vector2 posInWorld, float radiusInWorld, int sides, Color color, float thickness = 1f)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2 screenPos, out float screenRadius);
            DrawCircle(screenPos, screenRadius, sides, color, thickness);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCapsuleProjected(Capsule capsuleInWorld, Color color, float thickness = 1f)
        {
            var capsuleOnScreen = new Capsule(
                ProjectToScreenPosition(capsuleInWorld.Start),
                ProjectToScreenPosition(capsuleInWorld.End),
                ProjectToScreenSize(capsuleInWorld.Radius)
            );
            ScreenManager.SpriteBatch.DrawCapsule(capsuleOnScreen, color, thickness);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircleOnPlanet(Vector2 posInWorld, float radiusInWorld, Color color, float thickness = 1f)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2 screenPos, out float screenRadius, 2500f);
            DrawCircle(screenPos, screenRadius, color, thickness);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircleProjectedZ(Vector2 posInWorld, float radiusInWorld, Color color, float zAxis = 0f)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2 screenPos, out float screenRadius, zAxis);
            DrawCircle(screenPos, screenRadius, color);
        }

        // draws a projected circle, with an additional overlay texture
        public void DrawCircleProjected(Vector2 posInWorld, float radiusInWorld, Color color, float thickness, Texture2D overlay, Color overlayColor, float z = 0)
        {
            ProjectToScreenCoords(posInWorld, radiusInWorld, out Vector2 screenPos, out float screenRadius);
            float scale = screenRadius / (overlay.Width * .5f);
            DrawTexture(overlay, screenPos, scale, 0f, overlayColor);
            DrawCircle(screenPos, screenRadius, color, thickness);
        } 

        public void DarwCircleOnPlanetTextured(Vector2 posInWorld, float radiusInWorld, Color color, float thickness, Texture2D overlay, Color overlayColor)
            => DrawCircleProjected(posInWorld, radiusInWorld, color, thickness, overlay, overlayColor, 2500f);


        public void DrawRectangleProjected(Rectangle rectangle, Color edge)
        {
            Vector2 rectTopLeft  = ProjectToScreenPosition(new Vector2(rectangle.X, rectangle.Y));

            Vector2 rectBotRight = ProjectToScreenPosition(new Vector2(rectangle.X - rectangle.Width, rectangle.Y - rectangle.Height));
            var rect = new Rectangle((int)rectTopLeft.X, (int)rectTopLeft.Y, (int)(rectTopLeft.X - rectBotRight.X), (int)(rectTopLeft.Y - rectBotRight.Y));
            DrawRectangle(rect, edge);
        }

        public void DrawRectangleProjected(Rectangle rectangle, Color edge, Color fill)
        {
            Vector2 rectTopLeft  = ProjectToScreenPosition(new Vector2(rectangle.X, rectangle.Y));
            Vector2 rectBotRight = ProjectToScreenPosition(new Vector2(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height));
            var rect  = new Rectangle((int)rectTopLeft.X, (int)rectTopLeft.Y, 
                                    (int)Math.Abs(rectTopLeft.X - rectBotRight.X), (int)Math.Abs(rectTopLeft.Y - rectBotRight.Y));
            DrawRectangle(rect, edge, fill);            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRectangleProjected(Vector2 centerInWorld, Vector2 sizeInWorld, float rotation, Color color, float thickness = 1f)
        {
            ProjectToScreenCoords(centerInWorld, sizeInWorld, out Vector2 posOnScreen, out Vector2 sizeOnScreen);
            DrawRectangle(posOnScreen, sizeOnScreen, rotation, color, thickness);
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextureProjected(Texture2D texture, Vector2 posInWorld, float textureScale, Color color)
            => DrawTexture(texture, ProjectToScreenPosition(posInWorld), textureScale, 0.0f, color);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawTextureProjected(Texture2D texture, Vector2 posInWorld, float textureScale, float rotation, Color color)
            => DrawTexture(texture, ProjectToScreenPosition(posInWorld), textureScale, rotation, color);



        public void DrawTextureWithToolTip(Texture2D texture, Color color, int tooltipID, Vector2 mousePos, int rectangleX, int rectangleY, int width, int height)
        {
            var rectangle = new Rectangle(rectangleX, rectangleY, width, height);
            ScreenManager.SpriteBatch.Draw(texture, rectangle, color);
            
            if (rectangle.HitTest(mousePos))
                ToolTip.CreateTooltip(tooltipID);                
        }

        public void DrawTextureWithToolTip(Texture2D texture, Color color, string text, Vector2 mousePos, int rectangleX, int rectangleY, int width, int height)
        {
            var rectangle = new Rectangle(rectangleX, rectangleY, width, height);
            ScreenManager.SpriteBatch.Draw(texture, rectangle, color);

            if (rectangle.HitTest(mousePos))
                ToolTip.CreateTooltip(text);
        }
        public void DrawStringProjected(Vector2 posInWorld, float rotation, float textScale, Color textColor, string text)
        {
            Vector2 screenPos = ProjectToScreenPosition(posInWorld);
            Vector2 size = Fonts.Arial11Bold.MeasureString(text);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold, text, screenPos, textColor, rotation, size * 0.5f, textScale, SpriteEffects.None, 1f);
        }
    }
}
