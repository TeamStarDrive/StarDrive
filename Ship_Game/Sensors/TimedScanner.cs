using Microsoft.Xna.Framework;

namespace Ship_Game.Sensors
{
    public class TimedScanner : TargetData
    {
        float Timer                =0 ;
        public float TimerInterval = 0.2f;

        public TimedScanner(){}
        public TimedScanner(float interval ){TimerInterval = interval;}

        public override GameplayObject[] Scan(float elapsedTime, Vector2 position, Empire empire = null)
        {
            if (Timer -- < 0) {Timer = TimerInterval; return Nearby;}
            return base.Scan(elapsedTime, position, empire);
        }

    }
}