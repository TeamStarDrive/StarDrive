using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Debug.Page
{
    internal class SpatialDebug : DebugPage
    {
        readonly UniverseScreen Screen;
        readonly SpatialManager Spatial;
        FloatSlider LoyaltySlider;
        FloatSlider TypeSlider;
        Empire Loyalty;
        GameObjectType[] Types = (GameObjectType[])typeof(GameObjectType).GetEnumValues();
        GameObjectType FilterByType = GameObjectType.Ship;
        bool FilterByLoyalty = false;

        GameplayObject[] Found = Empty<GameplayObject>.Array;

        public SpatialDebug(UniverseScreen screen, DebugInfoScreen parent)
            : base(parent, DebugModes.SpatialManager)
        {
            Screen = screen;
            Spatial = UniverseScreen.Spatial;
            Loyalty = EmpireManager.GetEmpireById(1);

            var list = AddList(50, 210);
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
            
            list.AddCheckbox(() => this.FilterByLoyalty,
                    "FilterByLoyalty", "Filter debug search by Selected Loyalty in the slider below");

            LoyaltySlider = list.Add(new FloatSlider(SliderStyle.Decimal, 200, 30, $"Selected Loyalty: {Loyalty.Name}",
                                                     1, EmpireManager.NumEmpires, 0));
            LoyaltySlider.OnChange = (FloatSlider f) =>
            {
                Loyalty = EmpireManager.GetEmpireById((int)f.AbsoluteValue);
                f.Text = $"Selected Loyalty: {Loyalty.Name}";
            };
            
            TypeSlider = Add(new FloatSlider(SliderStyle.Decimal, 200, 30, $"Search Type: {FilterByType}",
                                             0, Types.Length-1, Array.IndexOf(Types, FilterByType) ));
            TypeSlider.OnChange = (FloatSlider f) =>
            {
                FilterByType = Types[(int)f.AbsoluteValue];
                f.Text = $"Search Type: {FilterByType}";
            };
            
            var changeLoyaltyBtn = list.Add(new UIButton(ButtonStyle.DanButtonBlue, $"Change Loyalty"));
            changeLoyaltyBtn.OnClick = (UIButton b) =>
            {
                if (Empire.Universe.SelectedShip != null)
                {
                    Empire.Universe.SelectedShip.ChangeLoyalty(Loyalty);
                }
                else if (Empire.Universe.SelectedShipList.NotEmpty)
                {
                    foreach (Ship ship in Empire.Universe.SelectedShipList)
                        ship.ChangeLoyalty(Loyalty);
                }
            };
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            SetTextCursor(50f, 150f, Color.White);
            DrawString($"Spatial.Type: {Spatial.Name}");
            DrawString($"Spatial.Collisions: {Spatial.Collisions}");
            DrawString($"Spatial.ActiveObjects: {Spatial.Count}");
            DrawString($"Spatial.SearchResults: {Found.Length}");
            Spatial.DebugVisualize(Screen);

            base.Draw(batch, elapsed);
        }

        public override bool HandleInput(InputState input)
        {
            if (base.HandleInput(input))
                return true;

            if (input.LeftMouseHeld(0.05f))
            {
                AABoundingBox2D screenArea = AABoundingBox2D.FromIrregularPoints(input.StartLeftHold, input.EndLeftHold);
                var opt = new Spatial.SearchOptions(Screen.UnprojectToWorldRect(screenArea), FilterByType)
                {
                    MaxResults = 1024,
                    DebugId = 1
                };
                if (FilterByLoyalty)
                    opt.OnlyLoyalty = Loyalty;

                Found = Spatial.FindNearby(opt);
                return true;
            }
            return false;
        }
    }
}
