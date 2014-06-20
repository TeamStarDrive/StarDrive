using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	internal class FTLManager
	{
		public const float scalePlus = 1.25f;

		public static UniverseScreen universeScreen;

		public static Texture2D FTLTexture;

		public static object FTLLock;

		public static BatchRemovalCollection<FTL> FTLList;

		static FTLManager()
		{
			FTLManager.FTLLock = new object();
			FTLManager.FTLList = new BatchRemovalCollection<FTL>();
		}

		public FTLManager()
		{
		}

		public static void Update(float elapsedTime)
		{
			for (int i = 0; i < FTLManager.FTLList.Count; i++)
			{
				if (FTLManager.FTLList[i] != null)
				{
					FTL item = FTLManager.FTLList[i];
					item.timer = item.timer - elapsedTime;
					if (FTLManager.FTLList[i].timer < 0.6f)
					{
						FTL fTL = FTLManager.FTLList[i];
						fTL.scale = fTL.scale / 2.5f;
					}
					else
					{
						FTL item1 = FTLManager.FTLList[i];
						item1.scale = item1.scale * 1.5625f;
						if (FTLManager.FTLList[i].scale > 60f)
						{
							FTLManager.FTLList[i].scale = 60f;
						}
					}
					if (elapsedTime > 0f)
					{
						FTL fTL1 = FTLManager.FTLList[i];
						fTL1.rotation = fTL1.rotation + 0.09817477f;
					}
					FTLManager.FTLList[i].WorldMatrix = Matrix.CreateRotationZ(FTLManager.FTLList[i].rotation) * Matrix.CreateTranslation(new Vector3(FTLManager.FTLList[i].Center, 0f));
					if (FTLManager.FTLList[i].timer <= 0f)
					{
						FTLManager.FTLList.QueuePendingRemoval(FTLManager.FTLList[i]);
					}
				}
			}
		}
	}
}