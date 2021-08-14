using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.GameScreens.ShipDesign
{
    class HullEditorControls : UIElementContainer
    {
        readonly ShipDesignScreen S;
        readonly UIList Labels;

        public HullEditorControls(ShipDesignScreen screen, Vector2 pos)
            : base(pos, new Vector2(200, 400))
        {
            S = screen;
            Button(ButtonStyle.Low100, pos, "Hull Editor", b => ToggleVisibility());

            Labels = Add(new UIList(ListLayoutStyle.ResizeList));
            Labels.SetRelPos(0, 20);
            Labels.Padding = new Vector2(2, 8);
            AddLabel(() => $"GridPos [{S.GridPosUnderCursor.X},{S.GridPosUnderCursor.Y}] slot: {S.SlotUnderCursor}");
            AddLabel(() => $"MeshOffset {S.CurrentHull?.MeshOffset}");
            AddLabel(() => $"GridCenter {S.CurrentHull?.GridCenter}");
        }

        UILabel AddLabel(Func<string> dynamicText)
        {
            var label = Labels.Add(new UILabel(Fonts.Arial12Bold));
            label.Color = Color.Yellow;
            label.DynamicText = l => dynamicText();
            return label;
        }

        void ToggleVisibility()
        {
            bool visible = (S.HullEditMode = !S.HullEditMode);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
        }
    }
}
