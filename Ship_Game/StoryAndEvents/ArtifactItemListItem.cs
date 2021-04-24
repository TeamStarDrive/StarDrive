using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class ArtifactItemListItem : ScrollListItem<ArtifactItemListItem>
    {
        public ArtifactEntry Artifact;
        public ArtifactItemListItem(ArtifactEntry artifact)
        {
            Artifact = artifact;
        }

        public override bool HandleInput(InputState input)
        {
            foreach (SkinnableButton button in Artifact.ArtifactButtons)
            {
                if (button.r.HitTest(input.CursorPosition))
                {
                    var art = (Artifact) button.ReferenceObject;
                    string text = $"{art.NameText.Text}\n\n{Localizer.Token(art.DescriptionIndex)}";
                    ToolTip.CreateTooltip(text);
                }
            }
            return false;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Vector2 cursor = Pos;
			foreach (SkinnableButton button in Artifact.ArtifactButtons)
			{
				button.r.X = (int)cursor.X;
				button.r.Y = (int)cursor.Y;
				cursor.X += 36f;
			}

            foreach (SkinnableButton button in Artifact.ArtifactButtons)
            {
                button.Draw(batch);
            }
        }
    }
}
