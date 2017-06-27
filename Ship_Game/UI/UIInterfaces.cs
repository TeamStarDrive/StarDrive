using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public interface IInputHandler
    {
        // @return TRUE if input was handled by the UI Control
        bool HandleInput(InputState input);
    }

    public interface IDrawable
    {
        void Draw(ScreenManager screenManager);
    }

}
