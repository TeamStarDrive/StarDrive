using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.UI;
using Ship_Game.Universe;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.GameScreens.NewGame
{
    public class FoeSelectionContainer : UIElementContainer
    {
        readonly GameScreen baseGameScreen;
        readonly UniverseParams universeParams;
        UI.UIKeyValueLabel Label;
        public FoeSelectionContainer(FoeSelectionScreen screen, UniverseParams p, in Rectangle rect) : base(rect)
        {
            baseGameScreen = screen;
            universeParams = p;

            Label = Add(new UI.UIKeyValueLabel("header", "Header", valueColor: Color.Cyan));
            Label.SetLocalPos(20, 0);
        }

        public override void Update(float fixedDeltaTime)
        {
            var empireNames = universeParams.SelectedFoes.Select(i => i.ArchetypeName);
            var Pos = Label.Pos;
            for (int i = 0; i < empireNames.Length; i++)
            {
                var a = new TextListItem(empireNames[i], Fonts.Arial12Bold);
                //check how ship desingn issue screen shows issue texts
            }
            base.Update(fixedDeltaTime);
        }
    }
}
