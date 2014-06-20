using Microsoft.Xna.Framework;
using System;

namespace Ship_Game
{
	public class Notification
	{
		public object ReferencedItem1;

		public object ReferencedItem2;

		public Empire RelevantEmpire;

		public string Message;

		public Rectangle ClickRect;

		public Rectangle DestinationRect;

		public float transitionElapsedTime;

		public float transDuration = 1f;

		public bool Tech;

		public string IconPath;

		public string Action;

		public bool ShowMessage;

		public Notification()
		{
		}
	}
}