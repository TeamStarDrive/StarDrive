using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

namespace Ship_Game.Debug.Page
{
    internal class SpatialDebug : DebugPage
    {
        readonly UniverseScreen Screen;
        readonly SpatialManager Spatial;
        public SpatialDebug(UniverseScreen screen, DebugInfoScreen parent)
            : base(parent, DebugModes.SpatialManager)
        {
            Screen = screen;
            Spatial = UniverseScreen.Spatial;
            
            var list = AddList(50, 200);
            list.AddCheckbox(() => Spatial.VisOpt.Enabled,
                    "Enable Overlay", "Enable Spatial Debug Overlay");

            list.AddCheckbox(() => Spatial.VisOpt.ObjectBounds,
                    "Object Rect", "Draw AABB rectangle over objects");
            list.AddCheckbox(() => Spatial.VisOpt.NodeBounds,
                    "Node Rect", "Draw AABB rectangle over nodes");
            list.AddCheckbox(() => Spatial.VisOpt.ObjectToLeaf,
                    "Object Owner Lines", "Draw lines from Object to owning Leaf Cell");

            list.AddCheckbox(() => Spatial.VisOpt.SearchDebug,
                    "Search Debug", "Show the debug information of latest searches");
            list.AddCheckbox(() => Spatial.VisOpt.SearchResults,
                    "Search Results", "Highlight search results with Yellow");
            list.AddCheckbox(() => Spatial.VisOpt.Collisions,
                    "Collisions", "Shows broad phase collisions as Cyan flashes");
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;
            
            SetTextCursor(50f, 150f, Color.White);
            DrawString($"Spatial.Type: {Spatial.Name}");
            DrawString($"Spatial.Collisions: {Spatial.Collisions}");
            DrawString($"Spatial.ActiveObjects: {Spatial.Count}");
            Spatial.DebugVisualize(Screen);

            base.Draw(batch, elapsed);
        }

    }
}
