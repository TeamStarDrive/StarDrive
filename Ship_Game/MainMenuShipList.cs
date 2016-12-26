using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ship_Game
{
    //Added by McShooterz: add a ship model to the main menu
    public sealed class MainMenuShipList
    {
        public Array<string> ModelPaths;

        public MainMenuShipList()
        {
            ModelPaths = new Array<string>();
        }
    }
}
