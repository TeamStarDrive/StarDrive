using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;

namespace Ship_Game.GameScreens.MainMenu
{
    [StarDataType]
    class MainMenuDesc
    {
        #pragma warning disable 649
        [StarData] public string Name;
        [StarData] public string UILayoutFile;
        [StarData] public string SceneFile;
        #pragma warning restore 649
    }
    
    [StarDataType]
    class MainMenusDescr
    {
        #pragma warning disable 649
        [StarData] public string Default;
        [StarData] public MainMenuDesc[] MainMenus = new MainMenuDesc[0];
        #pragma warning restore 649

        public MainMenuDesc GetDefault()
        {
            return MainMenus.FirstOrDefault(m => m.Name == Default) ?? MainMenus.First();
        }
    }
}
