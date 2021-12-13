using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game.GameScreens.Scene
{
    [StarDataType]
    public class SceneInstance
    {
        #pragma warning disable 649
        [StarData] public SceneFleet[] Fleets = new SceneFleet[0];
        #pragma warning restore 649

        GameScreen Screen;

        public static SceneInstance FromFile(GameScreen screen, string relativePath)
        {
            var scene = YamlParser.Deserialize<SceneInstance>(relativePath);
            scene.Initialize(screen);
            return scene;
        }

        public void Initialize(GameScreen screen)
        {
            Screen = screen;
            foreach (SceneFleet fleet in Fleets)
            {
                fleet.CreateShips(screen);
            }
        }

        public bool HandleInput(InputState input)
        {
            foreach (var fleet in Fleets)
            {
                fleet.HandleInput(input, Screen);
            }
            return false;
        }

        public void Update(FixedSimTime timeStep)
        {
            foreach (SceneFleet fleet in Fleets)
            {
                fleet.Update(Screen, timeStep);
            }
        }

        public void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            foreach (var fleet in Fleets)
            {
                fleet.Draw(batch, Screen);
            }
        }
    }
}
