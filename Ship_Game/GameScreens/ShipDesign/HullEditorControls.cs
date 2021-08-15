using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.ShipDesign
{
    internal class HullEditorControls : UIElementContainer
    {
        readonly ShipDesignScreen S;
        readonly UIList StatLabels;
        readonly UIList EditElements;

        readonly FloatSlider MeshOffsetY;

        public HullEditorControls(ShipDesignScreen screen, Vector2 pos)
            : base(pos, new Vector2(200, 400))
        {
            S = screen;
            Button(ButtonStyle.Low100, pos, "Hull Editor", b => ToggleHullEditMode());

            StatLabels = Add(new UIList(ListLayoutStyle.ResizeList));
            StatLabels.SetRelPos(0, 20);
            StatLabels.Padding = new Vector2(2, 8);
            AddStatLabel(() => $"GridPos [{S.GridPosUnderCursor.X},{S.GridPosUnderCursor.Y}] slot: {S.SlotUnderCursor}");
            AddStatLabel(() => $"MeshOffset {S.CurrentHull?.MeshOffset}");
            AddStatLabel(() => $"GridCenter {S.CurrentHull?.GridCenter}");

            EditElements = Add(new UIList(ListLayoutStyle.ResizeList));
            EditElements.SetRelPos(0, 100);
            EditElements.Padding = new Vector2(2, 8);

            MeshOffsetY = EditElements.Add(new FloatSlider(SliderStyle.Decimal, new Vector2(200, 30),
                                           "MeshOffset.Y", -200, +200, 0));
            MeshOffsetY.Step = 2f;

            SetHullEditVisibility(S.HullEditMode);
        }

        public void Initialize(ShipHull hull)
        {
            MeshOffsetY.Y = hull.MeshOffset.Y;
            MeshOffsetY.OnChange = (s) =>
            {
                hull.MeshOffset.Y = (float)Math.Round(s.AbsoluteValue);
                S.UpdateHullWorldPos();
            };
        }

        void AddStatLabel(Func<string> dynamicText)
        {
            var label = StatLabels.Add(new UILabel(Fonts.Arial12Bold));
            label.Color = Color.Yellow;
            label.DynamicText = l => dynamicText();
        }

        void ToggleHullEditMode()
        {
            S.HullEditMode = !S.HullEditMode;
            SetHullEditVisibility(S.HullEditMode);
        }

        void SetHullEditVisibility(bool visible)
        {
            EditElements.Visible = visible;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
        }
    }
}
