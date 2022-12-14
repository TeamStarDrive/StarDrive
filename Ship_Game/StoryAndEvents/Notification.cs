using SDGraphics;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
	public sealed class Notification
	{
		public object ReferencedItem1;
		public object ReferencedItem2;

		public Empire RelevantEmpire;
		public Rectangle ClickRect;
		public Rectangle DestinationRect;

		public float transitionElapsedTime;
		public float transDuration = 1f;
        
        public string Message;
		public string Action; // @TODO - this needs an enum!

        public SubTexture Icon;
        public string IconPath;
        
        public bool Tech;
		public bool ShowMessage;
        public bool Pause = true;
	}
}