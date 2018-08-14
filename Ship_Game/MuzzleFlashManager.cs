using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	internal sealed class MuzzleFlashManager
	{
		public static UniverseScreen universeScreen;

		public static Texture2D FlashTexture;

		public static Model flashModel;

		public static BatchRemovalCollection<MuzzleFlash> FlashList;

		static MuzzleFlashManager()
		{
			FlashList = new BatchRemovalCollection<MuzzleFlash>();
		}

		public MuzzleFlashManager()
		{
		}

		public static void Update(float elapsedTime)
		{
			lock (GlobalStats.ExplosionLocker)
			{
				for (int i = 0; i < FlashList.Count; i++)
				{
					if (FlashList[i] != null)
					{
						MuzzleFlash item = FlashList[i];
						item.timer = item.timer - elapsedTime;
						MuzzleFlash muzzleFlash = FlashList[i];
						muzzleFlash.scale = muzzleFlash.scale * 2f;
						if (FlashList[i].scale > 6f)
						{
							FlashList[i].scale = 6f;
						}
						if (FlashList[i].timer <= 0f)
						{
							FlashList.QueuePendingRemoval(FlashList[i]);
						}
					}
				}
			}
		}
	}
}